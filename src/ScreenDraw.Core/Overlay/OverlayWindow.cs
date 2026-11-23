using System.Runtime.InteropServices;
using ScreenDraw.Core.Diagnostics;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Interop;
using ScreenDraw.Core.Rendering;

namespace ScreenDraw.Core.Overlay;

public sealed class OverlayWindow : NativeWindowBase
{
    private const string ClassName = "ScreenDrawOverlayWindow";
    private const string Hint = "Armed  ·  left drag to ink  ·  Esc to finish";
    private const uint TopMostInterval = 500;
    private const uint HintDuration = 2500;

    private static readonly nint TopMostTimerId = 1;
    private static readonly nint HintTimerId = 2;

    private static readonly WindowExStyle Styles =
        WindowExStyle.Layered | WindowExStyle.TopMost | WindowExStyle.ToolWindow | WindowExStyle.NoActivate;

    private readonly InkCanvas _canvas;
    private readonly GdiOverlayRenderer _renderer;
    private bool _drawing;
    private bool _hintVisible;
    private bool _loggedFirstStroke;
    private bool _trackingLeave;
    private PointF? _cursor;

    public OverlayWindow(Rectangle bounds, InkCanvas canvas)
        : this(bounds, canvas, show: true)
    {
    }

    private OverlayWindow(Rectangle bounds, InkCanvas canvas, bool show)
        : base(
            ClassName,
            NativeMethods.LoadCursor(0, NativeMethods.IdcCross),
            (uint)WindowStyle.Popup,
            (uint)Styles,
            bounds,
            0)
    {
        _canvas = canvas;
        _renderer = new GdiOverlayRenderer(Handle);
        _renderer.Resize(bounds.Size);
        _hintVisible = show;
        _cursor = PointerWithin(bounds);
        Render();

        NativeMethods.SetTimer(Handle, TopMostTimerId, TopMostInterval, 0);
        if (show)
        {
            NativeMethods.SetTimer(Handle, HintTimerId, HintDuration, 0);
            NativeMethods.ShowWindow(Handle, NativeMethods.SwShowNoActivate);
        }

        Log.Write($"overlay: created hwnd=0x{Handle:X} rect={bounds.X},{bounds.Y} {bounds.Width}x{bounds.Height} shown={show}");
    }

    internal GdiOverlayRenderer Renderer => _renderer;

    internal static OverlayWindow CreateForTest(Rectangle bounds, InkCanvas canvas) => new(bounds, canvas, show: false);

    internal void RenderFrame() => Render();

    public override void Dispose()
    {
        Log.Write($"overlay: disposing hwnd=0x{Handle:X}");
        _renderer.Dispose();
        base.Dispose();
    }

    protected override nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        try
        {
            return HandleMessage(hwnd, message, wParam, lParam);
        }
        catch (Exception ex)
        {
            Log.Error($"overlay message 0x{message:X}", ex);
            return 0;
        }
    }

    private nint HandleMessage(nint hwnd, uint message, nint wParam, nint lParam)
    {
        switch (message)
        {
            case WindowsMessages.MouseMove:
                _cursor = ToPoint(lParam);
                TrackLeave(hwnd);
                if (_drawing)
                {
                    _canvas.ExtendStroke(_cursor.Value);
                }

                Render();
                return 0;

            case WindowsMessages.MouseLeave:
                _cursor = null;
                _trackingLeave = false;
                Render();
                return 0;

            case WindowsMessages.SetCursor when (lParam & 0xFFFF) == NativeMethods.HitTestClient:
                NativeMethods.SetCursor(0);
                return 1;

            case WindowsMessages.LeftButtonDown:
                NativeMethods.SetCapture(hwnd);
                _drawing = true;
                _canvas.BeginStroke(ToPoint(lParam));
                Render();
                return 0;

            case WindowsMessages.LeftButtonUp:
                FinishStroke();
                return 0;

            case WindowsMessages.CaptureChanged:
                FinishStroke();
                return 0;

            case WindowsMessages.RightButtonDown:
                _canvas.Clear();
                Render();
                return 0;

            case WindowsMessages.MiddleButtonDown:
                _canvas.Undo();
                Render();
                return 0;

            case WindowsMessages.MouseWheel:
                _canvas.Width += WheelDelta(wParam) > 0 ? 1f : -1f;
                Render();
                return 0;

            case WindowsMessages.Timer when wParam == TopMostTimerId:
                ReassertTopMost();
                return 0;

            case WindowsMessages.Timer when wParam == HintTimerId:
                _hintVisible = false;
                NativeMethods.KillTimer(hwnd, HintTimerId);
                Render();
                return 0;

            case WindowsMessages.Destroy:
                NativeMethods.KillTimer(hwnd, TopMostTimerId);
                NativeMethods.KillTimer(hwnd, HintTimerId);
                return 0;
        }

        return base.WindowProc(hwnd, message, wParam, lParam);
    }

    private void FinishStroke()
    {
        if (!_drawing)
        {
            return;
        }

        _drawing = false;
        var points = _canvas.Active?.Points.Count ?? 0;
        _canvas.EndStroke();
        NativeMethods.ReleaseCapture();

        if (!_loggedFirstStroke)
        {
            _loggedFirstStroke = true;
            Log.Write($"ink: first stroke received, {points} point(s)");
        }

        Render();
    }

    private void Render()
    {
        _renderer.Render(
            _canvas,
            new OverlayHud(Border: true, Hint: _hintVisible ? Hint : null, Cursor: _cursor, BrushWidth: _canvas.Width));
        _renderer.Present();
    }

    private void TrackLeave(nint hwnd)
    {
        if (_trackingLeave)
        {
            return;
        }

        _trackingLeave = true;
        var track = new TrackMouseEventData
        {
            Size = (uint)Marshal.SizeOf<TrackMouseEventData>(),
            Flags = NativeMethods.TrackLeave,
            Window = hwnd,
        };

        NativeMethods.TrackMouseEvent(ref track);
    }

    private static PointF? PointerWithin(Rectangle bounds)
    {
        if (NativeMethods.GetCursorPos(out var cursor) == 0)
        {
            return null;
        }

        var local = new Point(cursor.X - bounds.X, cursor.Y - bounds.Y);
        return new Rectangle(Point.Empty, bounds.Size).Contains(local) ? local : null;
    }

    private void ReassertTopMost() =>
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopMost,
            0,
            0,
            0,
            0,
            (uint)(SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize | SetWindowPosFlags.NoActivate));

    private static PointF ToPoint(nint lParam) =>
        new(unchecked((short)(long)lParam), unchecked((short)((long)lParam >> 16)));

    private static int WheelDelta(nint wParam) => unchecked((short)((long)wParam >> 16));
}
