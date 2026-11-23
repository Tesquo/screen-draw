namespace ScreenDraw.Core.Rendering;

public readonly record struct OverlayHud(
    bool Border = false,
    string? Hint = null,
    PointF? Cursor = null,
    float BrushWidth = 0f)
{
    public static OverlayHud None => default;
}
