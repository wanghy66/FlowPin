using System.Diagnostics;
using System.Windows.Automation;
using FlowPin.App.Interop;
using FlowPin.App.Models;

namespace FlowPin.App.Services;

public sealed class AutomationScrollService
{
    private readonly LoggerService _logger;
    private readonly AppSettings _settings;
    private IntPtr _cachedHwnd;
    private ScrollPattern? _cachedPattern;
    private DateTime _lastResolveUtc = DateTime.MinValue;

    public AutomationScrollService(LoggerService logger, AppSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public bool TryScrollContinuous(double speed, double deltaTime)
    {
        if (Math.Abs(speed) < 1 || deltaTime <= 0)
        {
            return false;
        }

        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        if (!TryGetProcessName(hwnd, out var processName))
        {
            return false;
        }

        if (IsEnhancedOnlyProcess(processName))
        {
            return false;
        }

        if (!IsContinuousPreferredProcess(processName))
        {
            return false;
        }

        var pattern = ResolveScrollPattern(hwnd);
        if (pattern is null)
        {
            return false;
        }

        try
        {
            var current = pattern.Current;
            if (!current.VerticallyScrollable)
            {
                return false;
            }

            var currentPercent = current.VerticalScrollPercent;
            if (double.IsNaN(currentPercent) || currentPercent < 0)
            {
                return false;
            }

            var deltaPercent = speed * deltaTime * 0.018;
            if (Math.Abs(deltaPercent) < 0.005)
            {
                return true;
            }

            var targetPercent = Math.Clamp(currentPercent + deltaPercent, 0, 100);
            var horizontal = current.HorizontallyScrollable ? current.HorizontalScrollPercent : ScrollPattern.NoScroll;
            pattern.SetScrollPercent(horizontal, targetPercent);
            return true;
        }
        catch (Exception ex)
        {
            InvalidateCache();
            _logger.Debug($"Automation scroll failed: {ex.Message}");
            return false;
        }
    }

    private ScrollPattern? ResolveScrollPattern(IntPtr hwnd)
    {
        var now = DateTime.UtcNow;
        if (hwnd == _cachedHwnd && _cachedPattern is not null && (now - _lastResolveUtc).TotalSeconds < 1.5)
        {
            return _cachedPattern;
        }

        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root is null)
            {
                InvalidateCache();
                return null;
            }

            var focused = AutomationElement.FocusedElement;
            var pattern = FindScrollPattern(focused, root) ?? FindScrollPattern(root, root);
            if (pattern is null)
            {
                InvalidateCache();
                return null;
            }

            _cachedHwnd = hwnd;
            _cachedPattern = pattern;
            _lastResolveUtc = now;
            return pattern;
        }
        catch (Exception ex)
        {
            InvalidateCache();
            _logger.Debug($"Resolve scroll pattern failed: {ex.Message}");
            return null;
        }
    }

    private static ScrollPattern? FindScrollPattern(AutomationElement? start, AutomationElement root)
    {
        if (start is null)
        {
            return null;
        }

        if (!IsDescendantOrSelf(start, root))
        {
            return null;
        }

        var walker = TreeWalker.RawViewWalker;
        var current = start;
        while (current is not null)
        {
            if (current.TryGetCurrentPattern(ScrollPattern.Pattern, out var patternObj) && patternObj is ScrollPattern pattern)
            {
                return pattern;
            }

            if (current.Equals(root))
            {
                break;
            }

            current = walker.GetParent(current);
        }

        try
        {
            var condition = new PropertyCondition(AutomationElement.IsScrollPatternAvailableProperty, true);
            var element = root.FindFirst(TreeScope.Subtree, condition);
            if (element is not null && element.TryGetCurrentPattern(ScrollPattern.Pattern, out var subtreePatternObj) && subtreePatternObj is ScrollPattern subtreePattern)
            {
                return subtreePattern;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool IsDescendantOrSelf(AutomationElement candidate, AutomationElement root)
    {
        if (candidate.Equals(root))
        {
            return true;
        }

        var walker = TreeWalker.RawViewWalker;
        var current = candidate;
        while (current is not null)
        {
            current = walker.GetParent(current);
            if (current is null)
            {
                return false;
            }

            if (current.Equals(root))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsEnhancedOnlyForegroundProcess()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        return TryGetProcessName(hwnd, out var processName) && IsEnhancedOnlyProcess(processName);
    }

    private bool IsContinuousPreferredProcess(string processName)
    {
        if (_settings.ContinuousPreferredProcesses.Count == 0)
        {
            return false;
        }

        return _settings.ContinuousPreferredProcesses.Any(item =>
            string.Equals(item, processName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsEnhancedOnlyProcess(string processName)
    {
        if (_settings.EnhancedModeProcesses.Count == 0)
        {
            return false;
        }

        return _settings.EnhancedModeProcesses.Any(item =>
            string.Equals(item, processName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetProcessName(IntPtr hwnd, out string processName)
    {
        processName = string.Empty;
        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            processName = Process.GetProcessById((int)processId).ProcessName;
            return !string.IsNullOrWhiteSpace(processName);
        }
        catch
        {
            return false;
        }
    }

    private void InvalidateCache()
    {
        _cachedHwnd = IntPtr.Zero;
        _cachedPattern = null;
        _lastResolveUtc = DateTime.MinValue;
    }
}

