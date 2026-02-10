namespace FlowPin.App.Core;

public sealed class AppState
{
    public bool IsEnabled { get; set; } = true;
    public bool IsFlowPinning { get; set; }
    public MousePoint AnchorPoint { get; set; }
    public MousePoint CurrentPoint { get; set; }
    public IntPtr TargetWindowHandle { get; set; } = IntPtr.Zero;
}

public readonly record struct MousePoint(int X, int Y);


