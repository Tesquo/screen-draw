namespace ScreenDraw.Core.Rendering;

public readonly record struct OverlayHud(
    bool Border = false,
    string? Label = null,
    PointF? Cursor = null,
    float BrushWidth = 0f,
    Color? BrushColor = null,
    bool Eraser = false)
{
    public static OverlayHud None => default;
}
