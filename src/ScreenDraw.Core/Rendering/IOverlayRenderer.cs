namespace ScreenDraw.Core.Rendering;

public interface IOverlayRenderer : IDisposable
{
    Size Size { get; }

    void Resize(Size size);

    void Render(Drawing.InkCanvas canvas, OverlayHud hud);

    void Present();
}
