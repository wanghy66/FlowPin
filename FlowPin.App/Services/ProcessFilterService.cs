using System.Diagnostics;
using FlowPin.App.Interop;
using FlowPin.App.Models;

namespace FlowPin.App.Services;

public sealed class ProcessFilterService
{
    private readonly AppSettings _settings;

    public ProcessFilterService(AppSettings settings)
    {
        _settings = settings;
    }

    public bool ShouldBlockForegroundWindow()
    {
        if (_settings.FilterMode == FilterMode.Disabled)
        {
            return false;
        }

        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            var process = Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            var className = NativeMethods.GetWindowClassName(hwnd);

            var processMatched = _settings.ProcessList.Any(item =>
                string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            var classMatched = !string.IsNullOrWhiteSpace(className) &&
                               _settings.WindowClassList.Any(item =>
                                   string.Equals(item, className, StringComparison.OrdinalIgnoreCase));
            var exists = processMatched || classMatched;

            return _settings.FilterMode switch
            {
                FilterMode.Blacklist => exists,
                FilterMode.Whitelist => !exists,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}

