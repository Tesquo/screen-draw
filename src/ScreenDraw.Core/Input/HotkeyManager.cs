using System.ComponentModel;
using System.Runtime.InteropServices;
using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Input;

public sealed class HotkeyManager : IDisposable
{
    private const string ClassName = "ScreenDrawHotkeyWindow";

    private readonly HotkeysWindow _window;
    private readonly Dictionary<int, Action> _callbacks = [];
    private int _nextId = 1;

    public HotkeyManager()
    {
        _window = new HotkeysWindow(OnHotkey);
    }

    public int Register(HotkeyModifiers modifiers, VirtualKey key, Action callback)
    {
        var id = _nextId++;
        if (NativeMethods.RegisterHotKey(_window.Handle, id, (uint)modifiers | NativeMethods.ModNoRepeat, (uint)key) == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "That hotkey is already in use by another program.");
        }

        _callbacks[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        NativeMethods.UnregisterHotKey(_window.Handle, id);
        _callbacks.Remove(id);
    }

    public void Dispose()
    {
        foreach (var id in _callbacks.Keys)
        {
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        }

        _callbacks.Clear();
        _window.Dispose();
    }

    private void OnHotkey(int id)
    {
        if (_callbacks.TryGetValue(id, out var callback))
        {
            callback();
        }
    }

    private sealed class HotkeysWindow : NativeWindowBase
    {
        private readonly Action<int> _callback;

        public HotkeysWindow(Action<int> callback)
            : base(ClassName, 0, 0, 0, Rectangle.Empty, NativeMethods.HwndMessage) => _callback = callback;

        protected override nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
        {
            if (message == WindowsMessages.Hotkey)
            {
                _callback((int)wParam);
                return 0;
            }

            return base.WindowProc(hwnd, message, wParam, lParam);
        }
    }
}
