using System.Diagnostics;
using FlowPin.App.Interop;
using FlowPin.App.Models;
using FlowPin.App.Services;

namespace FlowPin.App.Core;

public sealed class ScrollEngine : IDisposable
{
    private const double GlobalSpeedMultiplier = 2.0;
    private const double BaseSpeedFactor = 1200.0;
    private readonly AppState _state;
    private readonly AppSettings _settings;
    private readonly AutomationScrollService _automationScrollService;
    private readonly System.Threading.Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private double _wheelAccumulator;
    private double _lastSeconds;

    public ScrollEngine(AppState state, AppSettings settings, AutomationScrollService automationScrollService)
    {
        _state = state;
        _settings = settings;
        _automationScrollService = automationScrollService;
        _timer = new System.Threading.Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _stopwatch.Start();
        _lastSeconds = _stopwatch.Elapsed.TotalSeconds;
        _timer.Change(0, 4);
    }

    public void Dispose()
    {
        _timer.Dispose();
        _stopwatch.Stop();
    }

    private void OnTick(object? state)
    {
        if (!_state.IsEnabled || !_state.IsFlowPinning)
        {
            _wheelAccumulator = 0;
            _lastSeconds = _stopwatch.Elapsed.TotalSeconds;
            return;
        }

        var nowSeconds = _stopwatch.Elapsed.TotalSeconds;
        var deltaTime = nowSeconds - _lastSeconds;
        _lastSeconds = nowSeconds;
        if (deltaTime <= 0 || deltaTime > 0.1)
        {
            deltaTime = 0.008;
        }

        var dy = _state.CurrentPoint.Y - _state.AnchorPoint.Y;
        var absDy = Math.Abs(dy);
        if (absDy < _settings.DeadZone)
        {
            return;
        }

        var normalized = (absDy - _settings.DeadZone) / (double)Math.Max(1, _settings.Range);
        if (normalized <= 0)
        {
            return;
        }

        var curved = Math.Pow(normalized, _settings.Gamma);
        var speed = Math.Sign(dy) * BaseSpeedFactor * curved * _settings.Sensitivity * GlobalSpeedMultiplier;
        var deltaPerTick = speed * deltaTime;

        if (_automationScrollService.TryScrollContinuous(speed, deltaTime))
        {
            _wheelAccumulator = 0;
            return;
        }

        var enhanced = _automationScrollService.IsEnhancedOnlyForegroundProcess();
        var wheelStep = enhanced ? 5.0 : 30.0;
        _wheelAccumulator += deltaPerTick;
        var steps = (int)(Math.Abs(_wheelAccumulator) / wheelStep);
        if (steps > 0)
        {
            var direction = _wheelAccumulator > 0 ? 1 : -1;
            if (enhanced)
            {
                steps = Math.Min(steps, 6);
            }

            var wheelDelta = -(int)Math.Round(direction * steps * wheelStep);
            NativeMethods.SendMouseWheelToWindow(
                _state.TargetWindowHandle,
                wheelDelta,
                _state.AnchorPoint.X,
                _state.AnchorPoint.Y);
            _wheelAccumulator -= direction * steps * wheelStep;
        }
    }
}


