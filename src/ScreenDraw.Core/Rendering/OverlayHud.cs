namespace ScreenDraw.Core.Rendering;

public readonly record struct OverlayHud(bool Border, string? Hint)
{
    public static OverlayHud None => default;
}
