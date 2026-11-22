using System.Windows;
using System.Windows.Interop;

namespace QrSnippet.Services
{
    /// <summary>
    /// System-wide hotkey bound to a window's message loop. It fires even when the window
    /// is minimized or hidden, because hiding a WPF window keeps its HWND alive.
    /// </summary>
    public sealed class GlobalHotkey : IDisposable
    {
        private const int HotkeyId = 0x5152; // Arbitrary, unique within this window.

        private readonly HwndSource _source;
        private readonly Action _pressed;
        private bool _registered;

        private GlobalHotkey(HwndSource source, Action pressed)
        {
            _source = source;
            _pressed = pressed;
        }

        /// <summary>
        /// Registers the hotkey, or returns null if another app already owns the combination.
        /// </summary>
        public static GlobalHotkey? TryRegister(Window window, uint modifiers, uint virtualKey, Action pressed)
        {
            IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
            var hotkey = new GlobalHotkey(HwndSource.FromHwnd(hwnd), pressed);

            if (!NativeMethods.RegisterHotKey(hwnd, HotkeyId, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
            {
                return null;
            }

            hotkey._registered = true;
            hotkey._source.AddHook(hotkey.WndProc);
            return hotkey;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
            {
                handled = true;
                _pressed();
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (!_registered)
            {
                return;
            }

            _registered = false;
            _source.RemoveHook(WndProc);
            NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        }
    }
}
