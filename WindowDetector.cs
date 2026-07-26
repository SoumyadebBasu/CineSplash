using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CineSplash
{
    public static class WindowDetector
    {
        // P/Invoke declarations
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // Delegates
        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Constants
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

        // Static fields
        private static WinEventProc _winEventProc; // Keep reference to prevent GC
        private static IntPtr _hookHandle = IntPtr.Zero;
        private static Action<string> _onWindowDetected;

        public static void StartForegroundHook(Action<string> onDetected)
        {
            StopForegroundHook(); // Ensure we don't hook twice

            _onWindowDetected = onDetected;
            _winEventProc = new WinEventProc(WindowEventCallback);

            _hookHandle = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventProc,
                0, 0,
                WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        public static void StopForegroundHook()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _winEventProc = null;
            _onWindowDetected = null;
        }

        private static void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
            {
                string title = GetWindowTitle(hwnd);
                if (!string.IsNullOrWhiteSpace(title) && title != "CineSplashScreen" && title != "Playnite")
                {
                    _onWindowDetected?.Invoke(title);
                }
            }
        }

        public static bool FindWindowByTitle(string titleSubstring, out IntPtr foundHwnd)
        {
            IntPtr result = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true; // continue enumeration

                string title = GetWindowTitle(hWnd);
                if (string.IsNullOrWhiteSpace(title) || title == "CineSplashScreen" || title == "Playnite")
                    return true;

                if (title.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result = hWnd;
                    return false; // stop enumeration
                }

                return true;
            }, IntPtr.Zero);

            foundHwnd = result;
            return result != IntPtr.Zero;
        }

        public static string GetForegroundWindowTitle()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
            {
                return GetWindowTitle(hwnd);
            }
            return string.Empty;
        }

        private static string GetWindowTitle(IntPtr hwnd)
        {
            int length = GetWindowTextLength(hwnd);
            if (length > 0)
            {
                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            return string.Empty;
        }
    }
}
