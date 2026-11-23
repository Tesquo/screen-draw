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
    B = 0x42,
    D = 0x44,
    E = 0x45,
    Z = 0x5A,
    F9 = 0x78,
}
