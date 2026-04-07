using System.Drawing;
using System.Runtime.InteropServices;

namespace OlAform
{
    internal static class NativeMethods
    {
        public const uint GA_ROOT = 2;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private static readonly LowLevelMouseProc MouseProc = LowLevelMouseHookCallback;
        private static readonly object MouseHookSync = new();
        private static IntPtr _mouseHookHandle;
        private static bool _isLeftMouseButtonDown;
        private static int _mouseHookReferenceCount;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(Point point)
            {
                X = point.X;
                Y = point.Y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nuint dwExtraInfo);

        public static bool IsLeftMouseButtonDown()
        {
            return _isLeftMouseButtonDown;
        }

        public static void StartMouseHook()
        {
            lock (MouseHookSync)
            {
                if (_mouseHookReferenceCount == 0)
                {
                    _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, MouseProc, IntPtr.Zero, 0);
                    if (_mouseHookHandle == IntPtr.Zero)
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "安装鼠标钩子失败。");
                    }
                }

                _mouseHookReferenceCount++;
            }
        }

        public static void StopMouseHook()
        {
            lock (MouseHookSync)
            {
                if (_mouseHookReferenceCount <= 0)
                {
                    return;
                }

                _mouseHookReferenceCount--;
                if (_mouseHookReferenceCount == 0 && _mouseHookHandle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHookHandle);
                    _mouseHookHandle = IntPtr.Zero;
                    _isLeftMouseButtonDown = false;
                }
            }
        }

        public static void MoveMouseRelative(int deltaX, int deltaY)
        {
            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            mouse_event(
                MOUSEEVENTF_MOVE,
                unchecked((uint)deltaX),
                unchecked((uint)deltaY),
                0,
                0);
        }

        private static IntPtr LowLevelMouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var message = unchecked((int)wParam);
                if (message == WM_LBUTTONDOWN)
                {
                    _isLeftMouseButtonDown = true;
                }
                else if (message == WM_LBUTTONUP)
                {
                    _isLeftMouseButtonDown = false;
                }
            }

            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}
