namespace ScreenDraw.Core.Drawing;

public sealed class InkCanvas
{
    private const float MinimumPointDistance = 1.2f;
    private const float MinimumWidth = 1f;
    private const float MaximumWidth = 32f;

    private readonly List<Stroke> _strokes = [];
    private float _width = 4f;

    public IReadOnlyList<Stroke> Strokes => _strokes;

    public Stroke? Active { get; private set; }

    public int Version { get; private set; }

    public Color Color { get; set; } = Palette.Default;

    public bool Eraser { get; set; }

    public float Width
    {
        get => _width;
        set => _width = Math.Clamp(value, MinimumWidth, MaximumWidth);
    }

    public void BeginStroke(PointF point)
    {
        Active = new Stroke(Color, Width, Eraser);
        Active.Points.Add(point);
    }

    public void ExtendStroke(PointF point)
    {
        if (Active is null)
        {
            return;
        }

        var last = Active.Points[^1];
        if (Math.Abs(point.X - last.X) < MinimumPointDistance && Math.Abs(point.Y - last.Y) < MinimumPointDistance)
        {
            return;
        }

        Active.Points.Add(point);
    }

    public void EndStroke()
    {
        if (Active is null)
        {
            return;
        }

        if (Active.Points.Count > 0)
        {
            _strokes.Add(Active);
        }

        Active = null;
        Version++;
    }

    public void Undo()
    {
        if (_strokes.Count == 0)
        {
            return;
        }

        _strokes.RemoveAt(_strokes.Count - 1);
        Version++;
    }

    public void Clear()
    {
        Active = null;
        if (_strokes.Count == 0)
        {
            return;
        }

        _strokes.Clear();
        Version++;
    }

    public void CycleColor()
    {
        var index = Array.IndexOf(Palette.Colors, Color);
        Color = Palette.Colors[(index + 1) % Palette.Colors.Length];
        Eraser = false;
    }

    public void SetColor(int index)
    {
        Color = Palette.Colors[Math.Clamp(index, 0, Palette.Colors.Length - 1)];
        Eraser = false;
    }

    public void ToggleEraser() => Eraser = !Eraser;

    public void UsePen() => Eraser = false;

    public void AdjustWidth(float delta) => Width += delta;

    public string Describe()
    {
        if (Eraser)
        {
            return $"eraser · {Width:0} px";
        }

        var index = Array.IndexOf(Palette.Colors, Color);
        var name = index >= 0 ? Palette.Names[index] : Color.Name.ToLowerInvariant();
        return $"{name} · {Width:0} px";
    }
}
