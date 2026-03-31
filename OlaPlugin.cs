using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace OlAform
{
    // Lightweight wrapper for OLA plugin. Attempts to load native DLLs if available.
    public class OlaPlugin : IDisposable
    {
        private IntPtr _lib = IntPtr.Zero;
        public bool IsLoaded => _lib != IntPtr.Zero;

        public bool Load()
        {
            // try x64 then x86
            string[] candidates = { "OLA\\OLAPlug-beta65_x64.dll", "OLA\\OLAPlug-beta65_x86.dll", "OLAPlug-beta65_x64.dll", "OLAPlug-beta65_x86.dll" };
            foreach (var c in candidates)
            {
                _lib = LoadLibrary(c);
                if (_lib != IntPtr.Zero)
                    break;
            }
            return IsLoaded;
        }

        public void MouseMove(int x, int y)
        {
            // If plugin provides function we could call it. Fallback to Win32 SetCursorPos.
            SetCursorPos(x, y);
        }

        public void MouseClick(int button = 0)
        {
            // left click
            mouse_event(MouseEventFlags.LEFTDOWN | MouseEventFlags.LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public void KeyPress(string key)
        {
            // Very small fallback: send single char via keybd_event simulation for ASCII letters
            if (string.IsNullOrEmpty(key))
                return;
            foreach (var ch in key)
            {
                short vk = VkKeyScan(ch);
                byte vkCode = (byte)(vk & 0xff);
                keybd_event(vkCode, 0, 0, UIntPtr.Zero);
                keybd_event(vkCode, 0, KeyEventFlags.KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        public string OcrRegion(int x, int y, int w, int h)
        {
            // Placeholder: if plugin available, invoke OCR. Otherwise return empty string.
            return string.Empty;
        }

        public Rectangle FindImage(Bitmap template, double threshold = 0.9)
        {
            // Placeholder: return empty rectangle
            return Rectangle.Empty;
        }

        public void Dispose()
        {
            if (_lib != IntPtr.Zero)
            {
                FreeLibrary(_lib);
                _lib = IntPtr.Zero;
            }
        }

        #region Win32 imports
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [Flags]
        private enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
        }

        private static class KeyEventFlags
        {
            public const uint KEYEVENTF_KEYUP = 0x0002;
        }

        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion
    }
}
