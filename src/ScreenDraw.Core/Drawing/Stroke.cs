namespace ScreenDraw.Core.Drawing;

public sealed class Stroke
{
    public Stroke(Color color, float width, bool eraser)
    {
        Color = color;
        Width = width;
        IsEraser = eraser;
    }

    public Color Color { get; }

    public float Width { get; }

    public bool IsEraser { get; }

    public List<PointF> Points { get; } = [];
}
