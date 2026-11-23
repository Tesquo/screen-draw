namespace ScreenDraw.Core.Overlay;

public sealed class SnapshotEventArgs(Bitmap image) : EventArgs
{
    public Bitmap Image { get; } = image;
}
