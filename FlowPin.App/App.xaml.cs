using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using FlowPin.App.Core;
using FlowPin.App.Models;
using FlowPin.App.Services;

namespace FlowPin.App;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private LoggerService? _logger;
    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private AppState? _state;
    private ProcessFilterService? _filterService;
    private AutomationScrollService? _automationScrollService;
    private ScrollEngine? _scrollEngine;
    private GlobalMouseHookService? _mouseHook;
    private GlobalKeyboardService? _keyboardHook;
    private TrayService? _tray;
    private UI.AnchorOverlayWindow? _anchorOverlay;
    private UI.SettingsWindow? _settingsWindow;
    private long _lastMiddleClickTick;
    private bool _isMiddleButtonDown;
    private MousePoint _middleDownPoint;
    private long _middleDownTick;
    private bool _startedByMiddleDown;
    private bool _movedWhileMiddleDown;
    private bool _holdToScrollMode;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        var createdNew = false;
        _singleInstanceMutex = new Mutex(true, @"Global\FlowPin.App.Singleton", out createdNew);
        if (!createdNew)
        {
            Current.Shutdown();
            return;
        }

        _logger = new LoggerService();
        _settingsService = new SettingsService(_logger);
        _settings = _settingsService.Load();
        if (!_settings.EnableDebugLog)
        {
            _settings.EnableDebugLog = true;
            _settingsService.Save(_settings);
        }
        _state = new AppState();
        _filterService = new ProcessFilterService(_settings);

        _tray = new TrayService(
            onToggleEnabled: () => Current.Dispatcher.BeginInvoke(new Action(ToggleEnabled)),
            onShowSettings: () => Current.Dispatcher.BeginInvoke(new Action(ShowSettings)),
            onExit: () => Current.Dispatcher.BeginInvoke(new Action(ExitApplication)),
            isEnabledAccessor: () => _state.IsEnabled,
            languageAccessor: () => _settings?.UiLanguage ?? "zh-CN",
            filterModeAccessor: () => _settings?.FilterMode ?? FilterMode.Blacklist);
        _tray.Start();
        _tray.ShowStartupReady();
        ShowStartupToast();

        if (!_settings.HasShownStartupGuide)
        {
            var guideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1600)
            };
            guideTimer.Tick += (_, _) =>
            {
                guideTimer.Stop();
                _tray?.ShowStartupGuide();
            };
            guideTimer.Start();

            _settings.HasShownStartupGuide = true;
            _settingsService.Save(_settings);
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(InitializeRuntimeServices));

        _logger.Info("FlowPin started.");
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _tray?.Dispose();
        _keyboardHook?.Dispose();
        _mouseHook?.Dispose();
        _scrollEngine?.Dispose();
        _anchorOverlay?.Close();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
    }

    private void ToggleEnabled()
    {
        if (_state is null)
        {
            return;
        }

        _state.IsEnabled = !_state.IsEnabled;
        if (!_state.IsEnabled)
        {
            SetFlowPinning(false);
        }

        _tray?.RefreshState();
        _logger?.Info($"Enabled changed: {_state.IsEnabled}");
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsService is null)
        {
            return;
        }

        try
        {
            if (_settingsWindow is not null)
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                {
                    _settingsWindow.WindowState = WindowState.Normal;
                }

                _settingsWindow.Activate();
                _settingsWindow.Topmost = true;
                _settingsWindow.Topmost = false;
                _settingsWindow.Focus();
                return;
            }

            var window = new UI.SettingsWindow(_settings);
            _settingsWindow = window;
            window.Closed += (_, _) => _settingsWindow = null;

            var result = window.ShowDialog();
            if (result == true)
            {
                _settingsService.Save(_settings);
                _anchorOverlay?.ApplyStyle(_settings.OverlayRingColorHex, _settings.OverlaySize);
                _tray?.RefreshState();
                _logger?.Info("Settings saved.");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"ShowSettings failed: {ex.Message}");
            _tray?.ShowRuntimeWarning(
                _settings.UiLanguage == "en-US" ? "Settings Error" : "设置窗口异常",
                _settings.UiLanguage == "en-US" ? "Failed to open settings window." : "打开设置窗口失败。");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error($"Unhandled UI exception: {e.Exception.Message}");
        e.Handled = true;
        if (_settings is not null)
        {
            _tray?.ShowRuntimeWarning(
                _settings.UiLanguage == "en-US" ? "Runtime Error" : "运行时异常",
                _settings.UiLanguage == "en-US" ? "An exception was captured. App continues running." : "已捕获异常，程序继续运行。");
        }
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.Error($"Unhandled exception: {ex.Message}");
        }
    }

    private void ExitApplication()
    {
        _logger?.Info("Exit requested by tray menu.");
        Current.Shutdown();
    }

    private void OnMiddleButtonDown(object? sender, MousePoint point)
    {
        if (_state is null || _settings is null || _filterService is null)
        {
            return;
        }

        if (!_state.IsEnabled)
        {
            return;
        }

        var nowTick = Environment.TickCount64;
        var debounceMs = Math.Clamp(_settings.MiddleClickDebounceMs, 0, 1000);
        if (nowTick - _lastMiddleClickTick < debounceMs)
        {
            return;
        }
        _lastMiddleClickTick = nowTick;

        if (_filterService.ShouldBlockForegroundWindow())
        {
            return;
        }

        _isMiddleButtonDown = true;
        _middleDownPoint = point;
        _middleDownTick = Environment.TickCount64;
        _startedByMiddleDown = false;
        _movedWhileMiddleDown = false;
        _holdToScrollMode = false;

        if (_state.IsFlowPinning)
        {
            SetFlowPinning(false);
            return;
        }

        _state.AnchorPoint = point;
        _state.CurrentPoint = point;
        _state.TargetWindowHandle = Interop.NativeMethods.GetWindowFromScreenPoint(point.X, point.Y);
        if (_state.TargetWindowHandle == IntPtr.Zero)
        {
            _state.TargetWindowHandle = Interop.NativeMethods.GetForegroundWindow();
        }
        SetFlowPinning(true, point);
        _startedByMiddleDown = true;
    }

    private void OnMouseMoved(object? sender, MousePoint point)
    {
        if (_state is null || _settings is null)
        {
            return;
        }

        _state.CurrentPoint = point;
        if (_isMiddleButtonDown)
        {
            var distance = Math.Abs(point.X - _middleDownPoint.X) + Math.Abs(point.Y - _middleDownPoint.Y);
            if (distance >= 8)
            {
                _movedWhileMiddleDown = true;
                _holdToScrollMode = true;
            }
        }

        if (_state.IsFlowPinning)
        {
            _anchorOverlay?.UpdateIndicator(_state.CurrentPoint, _state.AnchorPoint, _settings.DeadZone);
        }
    }

    private void OnMiddleButtonUp(object? sender, MousePoint point)
    {
        if (_state is null)
        {
            return;
        }

        _isMiddleButtonDown = false;
        if (!_startedByMiddleDown)
        {
            return;
        }

        var duration = Environment.TickCount64 - _middleDownTick;
        var shouldExitOnUp = _holdToScrollMode || _movedWhileMiddleDown || duration > 260;
        if (shouldExitOnUp && _state.IsFlowPinning)
        {
            SetFlowPinning(false);
        }

        _startedByMiddleDown = false;
        _movedWhileMiddleDown = false;
        _holdToScrollMode = false;
    }

    private void OnEscapePressed(object? sender, EventArgs e)
    {
        if (_state is not null)
        {
            SetFlowPinning(false);
        }
    }

    private void SetFlowPinning(bool active, MousePoint? anchor = null)
    {
        if (_state is null)
        {
            return;
        }

        _state.IsFlowPinning = active;
        if (active)
        {
            var anchorPoint = anchor ?? _state.AnchorPoint;
            _state.AnchorPoint = anchorPoint;
            EnsureOverlay();
            _anchorOverlay?.ApplyStyle(_settings?.OverlayRingColorHex, _settings?.OverlaySize);
            _anchorOverlay?.ShowAt(anchorPoint);
            Mouse.OverrideCursor = System.Windows.Input.Cursors.ScrollNS;
        }
        else
        {
            _anchorOverlay?.HideOverlay();
            Mouse.OverrideCursor = null;
            _state.TargetWindowHandle = IntPtr.Zero;
        }
    }

    private void InitializeRuntimeServices()
    {
        if (_state is null || _settings is null || _logger is null)
        {
            return;
        }

        _automationScrollService = new AutomationScrollService(_logger, _settings);
        _scrollEngine = new ScrollEngine(_state, _settings, _automationScrollService);
        _mouseHook = new GlobalMouseHookService(_logger);
        _keyboardHook = new GlobalKeyboardService(_logger);

        _mouseHook.MiddleButtonDown += OnMiddleButtonDown;
        _mouseHook.MiddleButtonUp += OnMiddleButtonUp;
        _mouseHook.MouseMoved += OnMouseMoved;
        _keyboardHook.EscapePressed += OnEscapePressed;

        var mouseOk = _mouseHook.Start();
        var keyboardOk = _keyboardHook.Start();
        if (!mouseOk || !keyboardOk)
        {
            _tray?.ShowRuntimeWarning(
                _settings.UiLanguage == "en-US" ? "Hook Error" : "钩子初始化失败",
                _settings.UiLanguage == "en-US"
                    ? "Global input hook failed. Try restart as administrator."
                    : "全局输入钩子初始化失败，可尝试以管理员身份重启。");
        }
        _scrollEngine.Start();
    }

    private void EnsureOverlay()
    {
        if (_anchorOverlay is null)
        {
            _anchorOverlay = new UI.AnchorOverlayWindow();
            _anchorOverlay.Prepare();
        }
    }

    private void ShowStartupToast()
    {
        try
        {
            var toast = new UI.StartupToastWindow(_settings?.UiLanguage ?? "zh-CN");
            toast.Show();
        }
        catch
        {
        }
    }
}


