using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Interop;
using ScreenDraw.Core.Rendering;

namespace ScreenDraw.Core.Overlay;

public sealed class OverlayWindow : NativeWindowBase
{
    private const string ClassName = "ScreenDrawOverlayWindow";
    private static readonly nint TopMostTimerId = 1;
    private const uint TopMostInterval = 500;
    private const WindowExStyle Styles = WindowExStyle.Layered | WindowExStyle.TopMost | WindowExStyle.ToolWindow | WindowExStyle.NoActivate;

    private readonly InkCanvas _canvas;
    private readonly GdiOverlayRenderer _renderer;
    private bool _drawing;

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
        Render();
        NativeMethods.SetTimer(Handle, TopMostTimerId, TopMostInterval, 0);

        if (show)
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SwShowNoActivate);
        }
    }

    internal GdiOverlayRenderer Renderer => _renderer;

    internal static OverlayWindow CreateForTest(Rectangle bounds, InkCanvas canvas) => new(bounds, canvas, show: false);

    internal void RenderFrame() => Render();

    public override void Dispose()
    {
        _renderer.Dispose();
        base.Dispose();
    }

    protected override nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        switch (message)
        {
            case WindowsMessages.MouseMove:
                if (_drawing)
                {
                    _canvas.ExtendStroke(ToPoint(lParam));
                    Render();
                }

                return 0;

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
                return 0;

            case WindowsMessages.Timer:
                if (wParam == TopMostTimerId)
                {
                    ReassertTopMost();
                }

                return 0;

            case WindowsMessages.Destroy:
                NativeMethods.KillTimer(hwnd, TopMostTimerId);
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
        _canvas.EndStroke();
        NativeMethods.ReleaseCapture();
        Render();
    }

    private void Render()
    {
        _renderer.Render(_canvas);
        _renderer.Present();
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
