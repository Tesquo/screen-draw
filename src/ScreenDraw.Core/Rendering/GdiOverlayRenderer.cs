using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Rendering;

public sealed class GdiOverlayRenderer : IOverlayRenderer
{
    private const float HaloPadding = 4f;

    // Transparent pixels of a layered window are click-through, so the frame keeps an alpha floor of 1.
    private const byte HitTestAlpha = 1;

    private const float MinimumRingDiameter = 8f;

    private static readonly Font HudFont = new("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

    private readonly nint _hwnd;
    private nint _memoryDc;
    private nint _dib;
    private nint _previousBitmap;
    private Bitmap? _frame;
    private Graphics? _frameGraphics;
    private Bitmap? _committed;
    private Graphics? _committedGraphics;
    private uint _committedVersion = uint.MaxValue;

    public GdiOverlayRenderer(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public Size Size { get; private set; }

    internal Bitmap? Frame => _frame;

    public void Resize(Size size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Height, 1);

        if (size == Size && _frame is not null)
        {
            return;
        }

        Release();

        var screenDc = NativeMethods.GetDC(0);
        try
        {
            _memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = size.Width,
                    Height = -size.Height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = NativeMethods.BiRgb,
                },
            };

            _dib = NativeMethods.CreateDIBSection(screenDc, ref info, NativeMethods.DibRgbColors, out var bits, 0, 0);
            if (_dib == 0 || bits == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not allocate the overlay frame.");
            }

            _previousBitmap = NativeMethods.SelectObject(_memoryDc, _dib);
            _frame = new Bitmap(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppPArgb, bits);
            _frameGraphics = Graphics.FromImage(_frame);
            _committed = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            _committedGraphics = Graphics.FromImage(_committed);
            FillHitTestBackground(_committedGraphics, size);
            _committedVersion = uint.MaxValue;
            Size = size;
        }
        finally
        {
            NativeMethods.ReleaseDC(0, screenDc);
        }
    }

    public void Render(InkCanvas canvas, OverlayHud hud)
    {
        var committed = _committed ?? throw new InvalidOperationException("Resize must be called before rendering.");

        if (_committedVersion != (uint)canvas.Version)
        {
            var committedGraphics = _committedGraphics!;
            FillHitTestBackground(committedGraphics, Size);
            foreach (var stroke in canvas.Strokes)
            {
                DrawStroke(committedGraphics, stroke);
            }

            _committedVersion = (uint)canvas.Version;
        }

        var graphics = _frameGraphics!;
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImageUnscaled(committed, 0, 0);
        graphics.CompositingMode = CompositingMode.SourceOver;

        if (canvas.Active is { } active)
        {
            DrawStroke(graphics, active);
        }

        DrawHud(graphics, hud);
    }

    private static void FillHitTestBackground(Graphics graphics, Size size)
    {
        graphics.CompositingMode = CompositingMode.SourceCopy;
        using var brush = new SolidBrush(Color.FromArgb(HitTestAlpha, 0, 0, 0));
        graphics.FillRectangle(brush, 0, 0, size.Width, size.Height);
        graphics.CompositingMode = CompositingMode.SourceOver;
    }

    public void Present()
    {
        if (_frame is null)
        {
            throw new InvalidOperationException("Resize must be called before presenting.");
        }

        var size = new Size32 { Width = Size.Width, Height = Size.Height };
        var source = new Point32();
        var blend = new BlendFunction
        {
            BlendOp = (byte)NativeMethods.AcSrcOver,
            SourceConstantAlpha = 255,
            AlphaFormat = (byte)NativeMethods.AcSrcAlpha,
        };

        var screenDc = NativeMethods.GetDC(0);
        try
        {
            if (NativeMethods.UpdateLayeredWindow(_hwnd, screenDc, 0, ref size, _memoryDc, ref source, 0, ref blend, NativeMethods.UlwAlpha) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not present the overlay frame.");
            }
        }
        finally
        {
            NativeMethods.ReleaseDC(0, screenDc);
        }
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    private static void DrawHud(Graphics graphics, OverlayHud hud)
    {
        if (hud.Border && graphics.VisibleClipBounds is { Width: > 0, Height: > 0 } bounds)
        {
            using var pen = new Pen(Color.FromArgb(120, 255, 214, 64), 1f);
            graphics.DrawRectangle(pen, 0.5f, 0.5f, bounds.Width - 1.5f, bounds.Height - 1.5f);
        }

        if (string.IsNullOrEmpty(hud.Hint) == false)
        {
            DrawHint(graphics, hud.Hint);
        }

        if (hud.Cursor is { } position)
        {
            DrawBrushRing(graphics, position, hud.BrushWidth);
        }
    }

    private static void DrawHint(Graphics graphics, string hint)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var textSize = graphics.MeasureString(hint, HudFont);
        var pill = new RectangleF(16f, 16f, textSize.Width + 24f, textSize.Height + 12f);
        using (var back = new SolidBrush(Color.FromArgb(214, 18, 20, 26)))
        using (var rounded = RoundedRectangle(pill, 7f))
        {
            graphics.FillPath(back, rounded);
        }

        using var text = new SolidBrush(Color.FromArgb(255, 236, 240, 250));
        graphics.DrawString(hint, HudFont, text, pill.X + 12f, pill.Y + 6f);
    }

    private static void DrawBrushRing(Graphics graphics, PointF center, float brushWidth)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var diameter = Math.Max(brushWidth, MinimumRingDiameter);
        var radius = diameter / 2f;
        var left = center.X - radius;
        var top = center.Y - radius;

        using (var halo = new Pen(Color.FromArgb(150, 0, 0, 0), 2.5f))
        {
            graphics.DrawEllipse(halo, left, top, diameter, diameter);
        }

        using (var ring = new Pen(Color.FromArgb(235, 255, 255, 255), 1f))
        {
            graphics.DrawEllipse(ring, left, top, diameter, diameter);
        }

        using var dot = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
        graphics.FillEllipse(dot, center.X - 1f, center.Y - 1f, 2f, 2f);
    }

    private static GraphicsPath RoundedRectangle(RectangleF rectangle, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180f, 90f);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270f, 90f);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    private static void DrawStroke(Graphics graphics, Stroke stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var points = stroke.Points.ToArray();

        if (stroke.IsEraser)
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            using var eraser = new Pen(Color.FromArgb(HitTestAlpha, 0, 0, 0), stroke.Width + HaloPadding);
            ConfigureInk(eraser);
            DrawSegments(graphics, eraser, points);
            graphics.CompositingMode = CompositingMode.SourceOver;
            return;
        }

        graphics.CompositingMode = CompositingMode.SourceOver;
        using (var halo = new Pen(Color.FromArgb(90, 0, 0, 0), stroke.Width + HaloPadding))
        {
            ConfigureInk(halo);
            DrawSegments(graphics, halo, points);
        }

        using var ink = new Pen(stroke.Color, stroke.Width);
        ConfigureInk(ink);
        DrawSegments(graphics, ink, points);
    }

    private static void DrawSegments(Graphics graphics, Pen pen, PointF[] points)
    {
        if (points.Length == 1)
        {
            var radius = pen.Width / 2f;
            using var brush = new SolidBrush(pen.Color);
            graphics.FillEllipse(brush, points[0].X - radius, points[0].Y - radius, pen.Width, pen.Width);
            return;
        }

        graphics.DrawLines(pen, points);
    }

    private static void ConfigureInk(Pen pen)
    {
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        pen.LineJoin = LineJoin.Round;
    }

    private void Release()
    {
        _committedGraphics?.Dispose();
        _committedGraphics = null;
        _committed?.Dispose();
        _committed = null;
        _frameGraphics?.Dispose();
        _frameGraphics = null;
        _frame?.Dispose();
        _frame = null;

        if (_memoryDc != 0)
        {
            if (_previousBitmap != 0)
            {
                NativeMethods.SelectObject(_memoryDc, _previousBitmap);
                _previousBitmap = 0;
            }

            NativeMethods.DeleteDC(_memoryDc);
            _memoryDc = 0;
        }

        if (_dib != 0)
        {
            NativeMethods.DeleteObject(_dib);
            _dib = 0;
        }

        Size = Size.Empty;
    }
}
