using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FlowPin.App.Core;

namespace FlowPin.App.UI;

public partial class AnchorOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private readonly DispatcherTimer _rotationTimer;
    private double _currentAngle;
    private double _targetAngle;
    private bool _isPrepared;

    public AnchorOverlayWindow()
    {
        InitializeComponent();
        _rotationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _rotationTimer.Tick += OnRotationTick;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GwlExStyle);
        SetWindowLong(hwnd, GwlExStyle, exStyle | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    public void ShowAt(MousePoint point)
    {
        Prepare();
        MoveTo(point);
        ResetIndicator();
        if (!IsVisible)
        {
            Show();
        }
    }

    public void MoveTo(MousePoint point)
    {
        var dipPoint = ToDipPoint(point);
        var rightOffset = (Width / 2.0) + 2.0;
        Left = dipPoint.X - (Width / 2.0) + rightOffset;
        Top = dipPoint.Y - (Height / 2.0);
    }

    public void Prepare()
    {
        if (_isPrepared)
        {
            return;
        }

        var oldLeft = Left;
        var oldTop = Top;
        var oldOpacity = Opacity;

        Left = -10000;
        Top = -10000;
        Opacity = 0;
        Show();
        Hide();

        Left = oldLeft;
        Top = oldTop;
        Opacity = oldOpacity;
        _isPrepared = true;
    }

    public void UpdateIndicator(MousePoint currentPoint, MousePoint anchorPoint, int _deadZone)
    {
        var dx = currentPoint.X - anchorPoint.X;
        var dy = currentPoint.Y - anchorPoint.Y;
        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
        {
            ResetIndicator();
            return;
        }

        _targetAngle = (Math.Atan2(dy, dx) * 180.0 / Math.PI) + 90.0;
        EnsureRotationTimer();
        UpTriangle.Opacity = 0.95;
        DownTriangle.Opacity = 0;
    }

    public void ApplyStyle(string? ringColorHex, double? overlaySize)
    {
        if (overlaySize.HasValue)
        {
            var size = Math.Clamp(overlaySize.Value, 12.0, 48.0);
            Width = size;
            Height = size;
        }

        if (string.IsNullOrWhiteSpace(ringColorHex))
        {
            return;
        }

        try
        {
            var parsed = System.Windows.Media.ColorConverter.ConvertFromString(ringColorHex);
            if (parsed is not System.Windows.Media.Color ringColor)
            {
                return;
            }

            RingEllipse.Stroke = new SolidColorBrush(ringColor);
            var centerColor = System.Windows.Media.Color.FromArgb(0xCC, ringColor.R, ringColor.G, ringColor.B);
            CenterDot.Fill = new SolidColorBrush(centerColor);
            var fillColor = System.Windows.Media.Color.FromArgb(0x24, ringColor.R, ringColor.G, ringColor.B);
            RingEllipse.Fill = new SolidColorBrush(fillColor);
        }
        catch
        {
        }
    }

    public void HideOverlay()
    {
        if (IsVisible)
        {
            Hide();
        }
    }

    private void ResetIndicator()
    {
        _rotationTimer.Stop();
        _currentAngle = 0;
        _targetAngle = 0;
        UpTriangleRotate.Angle = 0;
        UpTriangle.Opacity = 0.95;
        DownTriangle.Opacity = 0.95;
    }

    private void EnsureRotationTimer()
    {
        if (!_rotationTimer.IsEnabled)
        {
            _rotationTimer.Start();
        }
    }

    private void OnRotationTick(object? sender, EventArgs e)
    {
        var delta = NormalizeDelta(_targetAngle - _currentAngle);
        if (Math.Abs(delta) < 0.5)
        {
            _currentAngle = _targetAngle;
            UpTriangleRotate.Angle = _currentAngle;
            _rotationTimer.Stop();
            return;
        }

        _currentAngle += delta * 0.24;
        UpTriangleRotate.Angle = _currentAngle;
    }

    private static double NormalizeDelta(double angle)
    {
        while (angle > 180)
        {
            angle -= 360;
        }

        while (angle < -180)
        {
            angle += 360;
        }

        return angle;
    }

    private System.Windows.Point ToDipPoint(MousePoint point)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return new System.Windows.Point(point.X, point.Y);
        }

        var transformFromDevice = source.CompositionTarget.TransformFromDevice;
        return transformFromDevice.Transform(new System.Windows.Point(point.X, point.Y));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}

