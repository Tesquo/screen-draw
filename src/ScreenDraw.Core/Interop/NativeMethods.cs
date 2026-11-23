using System.Runtime.InteropServices;

namespace ScreenDraw.Core.Interop;

internal static partial class NativeMethods
{
    internal const int GwlExStyle = -20;
    internal const int GwlUserData = -21;

    internal const int IdcArrow = 32512;
    internal const int IdcCross = 32515;

    internal const int SwShowNoActivate = 4;
    internal const int HwndMessage = -3;

    internal const uint UlwAlpha = 2;
    internal const uint AcSrcOver = 0;
    internal const uint AcSrcAlpha = 1;
    internal const uint DibRgbColors = 0;
    internal const uint BiRgb = 0;

    internal const uint DwmwaExtendedFrameBounds = 9;
    internal const uint ModNoRepeat = 0x4000;
    internal const uint AttachParentProcess = 0xFFFFFFFF;

    internal const uint InputKeyboard = 1;
    internal const uint KeyEventKeyUp = 2;
    internal const uint PmRemove = 1;

    internal const int ErrorClassAlreadyExists = 1410;

    internal static readonly nint HwndTopMost = -1;

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    internal static partial ushort RegisterClassEx(ref WindowClass windowClass);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int UnregisterClass(string className, nint instance);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowEx(
        uint exStyle,
        string className,
        string? windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    internal static partial nint DefWindowProc(nint hwnd, uint message, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    internal static partial int ShowWindow(nint hwnd, int command);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static partial nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [LibraryImport("user32.dll")]
    internal static partial int IsIconic(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int GetWindowRect(nint hwnd, out Rect rect);

    [LibraryImport("user32.dll")]
    internal static partial nint SetCapture(nint hwnd);

    [LibraryImport("user32.dll")]
    internal static partial int ReleaseCapture();

    [LibraryImport("user32.dll", EntryPoint = "SetTimer")]
    internal static partial nint SetTimer(nint hwnd, nint id, uint interval, nint callback);

    [LibraryImport("user32.dll", EntryPoint = "KillTimer")]
    internal static partial int KillTimer(nint hwnd, nint id);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int RegisterHotKey(nint hwnd, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int UnregisterHotKey(nint hwnd, int id);

    [LibraryImport("user32.dll")]
    internal static partial nint GetDC(nint hwnd);

    [LibraryImport("user32.dll")]
    internal static partial int ReleaseDC(nint hwnd, nint dc);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    internal static partial nint LoadCursor(nint instance, nint cursor);

    [LibraryImport("user32.dll", EntryPoint = "SendInput")]
    internal static partial uint SendInput(uint count, [In] InputEvent[] inputs, int size);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    internal static partial int PeekMessage(out Msg message, nint hwnd, uint minimum, uint maximum, uint remove);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    internal static partial int TranslateMessage(ref Msg message);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    internal static partial nint DispatchMessage(ref Msg message);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial int UpdateLayeredWindow(
        nint hwnd,
        nint destinationDc,
        nint destinationPoint,
        ref Size32 size,
        nint sourceDc,
        ref Point32 sourcePoint,
        uint colorKey,
        ref BlendFunction blend,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(nint hwnd, [Out] char[] text, int maxCount);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? moduleName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial int AttachConsole(uint processId);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(nint hwnd, uint attribute, out Rect value, int size);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial nint CreateCompatibleDC(nint dc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial int DeleteDC(nint dc);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial nint SelectObject(nint dc, nint gdiObject);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    internal static partial int DeleteObject(nint gdiObject);
}
