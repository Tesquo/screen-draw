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

    private readonly nint _hwnd;
    private nint _memoryDc;
    private nint _dib;
    private nint _previousBitmap;
    private Bitmap? _frame;
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
            _committed = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            _committedGraphics = Graphics.FromImage(_committed);
            _committedVersion = uint.MaxValue;
            Size = size;
        }
        finally
        {
            NativeMethods.ReleaseDC(0, screenDc);
        }
    }

    public void Render(InkCanvas canvas)
    {
        var frame = _frame ?? throw new InvalidOperationException("Resize must be called before rendering.");
        var committed = _committed!;

        if (_committedVersion != (uint)canvas.Version)
        {
            var committedGraphics = _committedGraphics!;
            committedGraphics.Clear(Color.Transparent);
            foreach (var stroke in canvas.Strokes)
            {
                DrawStroke(committedGraphics, stroke);
            }

            _committedVersion = (uint)canvas.Version;
        }

        using var graphics = Graphics.FromImage(frame);
        graphics.Clear(Color.Transparent);
        graphics.DrawImageUnscaled(committed, 0, 0);
        if (canvas.Active is { } active)
        {
            DrawStroke(graphics, active);
        }
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
            using var eraser = new Pen(Color.Transparent, stroke.Width + HaloPadding);
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
