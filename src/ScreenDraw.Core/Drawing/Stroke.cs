namespace ScreenDraw.Core.Drawing;

public sealed class Stroke
{
    public Stroke(Color color, float width, InkTool tool)
    {
        Color = color;
        Width = width;
        Tool = tool;
    }

    public Color Color { get; }

    public float Width { get; }

    public InkTool Tool { get; }

    public List<PointF> Points { get; } = [];
}
