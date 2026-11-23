using System.Runtime.InteropServices;

namespace ScreenDraw.Core.Interop;

[Flags]
internal enum WindowExStyle : uint
{
    TopMost = 0x00000008,
    Transparent = 0x00000020,
    ToolWindow = 0x00000080,
    NoRedirectionBitmap = 0x00200000,
    Layered = 0x00080000,
    NoActivate = 0x08000000,
}

internal enum WindowStyle : uint
{
    Popup = 0x80000000,
}

[Flags]
internal enum SetWindowPosFlags : uint
{
    NoSize = 0x0001,
    NoMove = 0x0002,
    NoZOrder = 0x0004,
    NoActivate = 0x0010,
    FrameChanged = 0x0020,
    ShowWindow = 0x0040,
}

internal static class WindowsMessages
{
    public const uint Destroy = 0x0002;
    public const uint NcCreate = 0x0081;
    public const uint NcDestroy = 0x0082;
    public const uint MouseMove = 0x0200;
    public const uint LeftButtonDown = 0x0201;
    public const uint LeftButtonUp = 0x0202;
    public const uint RightButtonDown = 0x0204;
    public const uint MiddleButtonDown = 0x0207;
    public const uint MouseWheel = 0x020A;
    public const uint CaptureChanged = 0x0215;
    public const uint Timer = 0x0113;
    public const uint Hotkey = 0x0312;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;

    public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);

    public override readonly string ToString() => $"({Left}, {Top})-({Right}, {Bottom})";
}

[StructLayout(LayoutKind.Sequential)]
internal struct Point32
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Size32
{
    public int Width;
    public int Height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowClass
{
    public uint Size;
    public uint Style;
    public nint WindowProcedure;
    public int ClassExtra;
    public int WindowExtra;
    public nint Instance;
    public nint Icon;
    public nint Cursor;
    public nint Background;
    public nint MenuName;
    public nint ClassName;
    public nint SmallIcon;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CreateStruct
{
    public nint CreateParams;
    public nint Instance;
    public nint Menu;
    public nint Parent;
    public int Height;
    public int Width;
    public int Y;
    public int X;
    public uint Style;
    public nint Name;
    public nint ClassName;
    public uint ExStyle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct BlendFunction
{
    public byte BlendOp;
    public byte BlendFlags;
    public byte SourceConstantAlpha;
    public byte AlphaFormat;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfoHeader
{
    public uint Size;
    public int Width;
    public int Height;
    public ushort Planes;
    public ushort BitCount;
    public uint Compression;
    public uint SizeImage;
    public int XPelsPerMeter;
    public int YPelsPerMeter;
    public uint ColorsUsed;
    public uint ColorsImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfo
{
    public BitmapInfoHeader Header;
    public uint Colors;
}

internal delegate nint WindowProcedure(nint hwnd, uint message, nint wParam, nint lParam);
