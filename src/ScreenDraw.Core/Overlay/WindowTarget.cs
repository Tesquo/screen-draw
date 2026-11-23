using System.Runtime.InteropServices;
using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Overlay;

internal readonly record struct WindowTarget(nint Handle, Rectangle Bounds, string Title)
{
    public static WindowTarget? Foreground()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == 0)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return null;
        }

        if (NativeMethods.IsIconic(hwnd) != 0)
        {
            return null;
        }

        var bounds = GetBounds(hwnd);
        if (bounds.Width < 64 || bounds.Height < 64)
        {
            return null;
        }

        return new WindowTarget(hwnd, bounds, GetTitle(hwnd));
    }

    private static Rectangle GetBounds(nint hwnd)
    {
        if (NativeMethods.DwmGetWindowAttribute(
                hwnd,
                NativeMethods.DwmwaExtendedFrameBounds,
                out var frame,
                Marshal.SizeOf<Rect>()) == 0)
        {
            var rect = frame.ToRectangle();
            if (rect.Width > 0 && rect.Height > 0)
            {
                return rect;
            }
        }

        return NativeMethods.GetWindowRect(hwnd, out var classic) != 0 ? classic.ToRectangle() : Rectangle.Empty;
    }

    private static string GetTitle(nint hwnd)
    {
        var buffer = new char[256];
        var length = NativeMethods.GetWindowText(hwnd, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : "window";
    }
}
