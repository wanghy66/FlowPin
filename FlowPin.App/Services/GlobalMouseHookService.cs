using System.Runtime.InteropServices;
using FlowPin.App.Core;
using FlowPin.App.Interop;

namespace FlowPin.App.Services;

public sealed class GlobalMouseHookService : IDisposable
{
    private readonly LoggerService _logger;
    private readonly NativeMethods.LowLevelMouseProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event EventHandler<MousePoint>? MiddleButtonDown;
    public event EventHandler<MousePoint>? MiddleButtonUp;
    public event EventHandler<MousePoint>? MouseMoved;

    public GlobalMouseHookService(LoggerService logger)
    {
        _logger = logger;
        _proc = HookCallback;
    }

    public bool Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return true;
        }

        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            var code = Marshal.GetLastWin32Error();
            _logger.Error($"Failed to install mouse hook: {code}");
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var msg = wParam.ToInt32();
        var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
        var point = new MousePoint(data.pt.X, data.pt.Y);

        if (msg == NativeMethods.WM_MBUTTONDOWN)
        {
            MiddleButtonDown?.Invoke(this, point);
        }
        else if (msg == NativeMethods.WM_MBUTTONUP)
        {
            MiddleButtonUp?.Invoke(this, point);
        }
        else if (msg == NativeMethods.WM_MOUSEMOVE)
        {
            MouseMoved?.Invoke(this, point);
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

