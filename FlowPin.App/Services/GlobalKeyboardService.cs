using System.Runtime.InteropServices;
using FlowPin.App.Interop;

namespace FlowPin.App.Services;

public sealed class GlobalKeyboardService : IDisposable
{
    private readonly LoggerService _logger;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event EventHandler? EscapePressed;

    public GlobalKeyboardService(LoggerService logger)
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

        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            var code = Marshal.GetLastWin32Error();
            _logger.Error($"Failed to install keyboard hook: {code}");
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

        var message = wParam.ToInt32();
        if (message == NativeMethods.WM_KEYDOWN)
        {
            var keyboardData = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if (keyboardData.vkCode == NativeMethods.VK_ESCAPE)
            {
                EscapePressed?.Invoke(this, EventArgs.Empty);
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}

