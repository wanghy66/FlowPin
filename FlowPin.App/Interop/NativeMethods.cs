using System.Runtime.InteropServices;
using System.Text;

namespace FlowPin.App.Interop;

internal static class NativeMethods
{
    internal const int WH_MOUSE_LL = 14;
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WM_MOUSEMOVE = 0x0200;
    internal const int WM_MBUTTONDOWN = 0x0207;
    internal const int WM_MBUTTONUP = 0x0208;
    internal const int WM_MOUSEWHEEL = 0x020A;
    internal const int WM_KEYDOWN = 0x0100;
    internal const int VK_ESCAPE = 0x1B;
    internal const int WHEEL_DELTA = 120;
    internal const uint INPUT_MOUSE = 0;
    internal const uint MOUSEEVENTF_WHEEL = 0x0800;

    internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    internal static void SendMouseWheelInput(int wheelDelta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    mouseData = unchecked((uint)wheelDelta),
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            }
        };
        SendInput(1, ref input, Marshal.SizeOf<INPUT>());
    }

    internal static void SendMouseWheelToWindow(IntPtr hwnd, int wheelDelta, int screenX, int screenY)
    {
        if (hwnd != IntPtr.Zero)
        {
            var wParam = MakeWParam(0, (short)wheelDelta);
            var lParam = MakeLParam(screenX, screenY);
            if (PostMessage(hwnd, WM_MOUSEWHEEL, wParam, lParam))
            {
                return;
            }
        }

        SendMouseWheelInput(wheelDelta);
    }

    internal static string GetWindowClassName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        var length = GetClassName(hwnd, builder, builder.Capacity);
        return length > 0 ? builder.ToString() : string.Empty;
    }

    internal static IntPtr GetWindowFromScreenPoint(int x, int y)
    {
        var pt = new POINT { X = x, Y = y };
        return WindowFromPoint(pt);
    }

    private static IntPtr MakeWParam(int low, int high)
    {
        var value = (low & 0xFFFF) | ((high & 0xFFFF) << 16);
        return new IntPtr(value);
    }

    private static IntPtr MakeLParam(int low, int high)
    {
        var value = (low & 0xFFFF) | ((high & 0xFFFF) << 16);
        return new IntPtr(value);
    }
}

