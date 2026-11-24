using System.Reflection;
using ScreenDraw.Core.Diagnostics;

namespace ScreenDraw.App;

internal static class IconAssets
{
    private const string TrayResource = "ScreenDraw.tray.ico";
    private const string ApplicationResource = "ScreenDraw.app.ico";

    public static Icon Tray() => Load(TrayResource, SystemInformation.SmallIconSize);

    public static Icon Window() => Load(ApplicationResource, new Size(32, 32));

    private static Icon Load(string resource, Size size)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
        if (stream is null)
        {
            Log.Write($"icons: '{resource}' is missing, falling back to the stock icon");
            return (Icon)SystemIcons.Application.Clone();
        }

        return new Icon(stream, size);
    }
}
