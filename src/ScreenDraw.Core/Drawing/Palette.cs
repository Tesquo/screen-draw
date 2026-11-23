namespace ScreenDraw.Core.Drawing;

public static class Palette
{
    public static readonly Color Yellow = Color.FromArgb(255, 255, 214, 64);
    public static readonly Color Red = Color.FromArgb(255, 255, 77, 77);
    public static readonly Color Cyan = Color.FromArgb(255, 64, 224, 255);
    public static readonly Color Green = Color.FromArgb(255, 120, 240, 140);
    public static readonly Color White = Color.FromArgb(255, 245, 245, 245);

    public static readonly Color[] Colors = [Yellow, Red, Cyan, Green, White];

    public static readonly string[] Names = ["yellow", "red", "cyan", "green", "white"];

    public static Color Default => Yellow;
}
