using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Capture;

public static class ScreenCapture
{
    public static Bitmap Capture(Rectangle region)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(region.Width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(region.Height, 1);

        var result = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppRgb);
        var screenDc = NativeMethods.GetDC(0);

        try
        {
            using var graphics = Graphics.FromImage(result);
            var destinationDc = graphics.GetHdc();

            try
            {
                if (NativeMethods.BitBlt(
                        destinationDc,
                        0,
                        0,
                        region.Width,
                        region.Height,
                        screenDc,
                        region.Left,
                        region.Top,
                        NativeMethods.SrcCopy | NativeMethods.CaptureBlt) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not copy the screen region.");
                }
            }
            finally
            {
                graphics.ReleaseHdc(destinationDc);
            }
        }
        catch
        {
            result.Dispose();
            throw;
        }
        finally
        {
            NativeMethods.ReleaseDC(0, screenDc);
        }

        return result;
    }
}
