namespace ScreenDraw.Core.Input;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}

public enum VirtualKey : uint
{
    Escape = 0x1B,
    Space = 0x20,
    B = 0x42,
    D = 0x44,
    E = 0x45,
    H = 0x48,
    Q = 0x51,
    Z = 0x5A,
    F9 = 0x78,
    D1 = 0x31,
    D2 = 0x32,
    D3 = 0x33,
    D4 = 0x34,
    D5 = 0x35,
    D6 = 0x36,
    OemOpenBrackets = 0xDB,
    OemCloseBrackets = 0xDD,
}
