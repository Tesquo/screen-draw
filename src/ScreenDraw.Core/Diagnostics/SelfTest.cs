using System.ComponentModel;
using System.Drawing.Imaging;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Input;
using ScreenDraw.Core.Overlay;

namespace ScreenDraw.Core.Diagnostics;

public static class SelfTest
{
    public static int Run(TextWriter? output = null)
    {
        var log = output ?? Console.Out;
        var failures = 0;

        void Check(string name, Func<bool> assertion)
        {
            var passed = false;
            var detail = string.Empty;

            try
            {
                passed = assertion();
            }
            catch (Exception ex)
            {
                detail = $" ({ex.GetType().Name}: {ex.Message})";
            }

            if (!passed)
            {
                failures++;
            }

            log.WriteLine($"{(passed ? "PASS" : "FAIL")}  {name}{detail}");
        }

        var canvas = new InkCanvas();
        canvas.BeginStroke(new PointF(10, 10));
        canvas.ExtendStroke(new PointF(45, 25));
        canvas.ExtendStroke(new PointF(80, 40));
        canvas.EndStroke();

        Check("stroke commits to the canvas", () => canvas.Strokes.Count == 1 && canvas.Active is null && canvas.Version == 1);

        canvas.BeginStroke(new PointF(120, 120));
        canvas.EndStroke();
        canvas.Undo();

        Check("undo removes the last stroke", () => canvas.Strokes.Count == 1 && canvas.Version == 3);

        Check("width is clamped", () =>
        {
            canvas.Width = 500f;
            var clampedHigh = Math.Abs(canvas.Width - 32f) < float.Epsilon;
            canvas.Width = 0f;
            var clampedLow = Math.Abs(canvas.Width - 1f) < float.Epsilon;
            canvas.Width = 4f;
            return clampedHigh && clampedLow;
        });

        var bounds = new Rectangle(0, 0, 320, 200);
        try
        {
            using var window = OverlayWindow.CreateForTest(bounds, canvas);
            window.RenderFrame();
            var frame = window.Renderer.Frame;
            Check("overlay frame is allocated", () => frame is not null);

            if (frame is not null)
            {
                Check("ink is visible in the frame", () => frame.GetPixel(45, 25).A > 0);
                Check("empty pixels stay transparent", () => frame.GetPixel(300, 180).A == 0);

                var preview = Path.Combine(Path.GetTempPath(), "screendraw-selftest.png");
                frame.Save(preview, ImageFormat.Png);
                log.WriteLine($"      preview: {preview}");
            }
        }
        catch (Exception ex)
        {
            failures++;
            log.WriteLine($"FAIL  overlay window ({ex.GetType().Name}: {ex.Message})");
            log.WriteLine(ex.ToString());
        }

        using (var hotkeys = new HotkeyManager())
        {
            try
            {
                var id = hotkeys.Register(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    VirtualKey.F9,
                    () => { });
                hotkeys.Unregister(id);
                log.WriteLine("PASS  hotkey registration");
            }
            catch (Win32Exception ex)
            {
                log.WriteLine($"SKIP  hotkey registration ({ex.Message})");
            }
        }

        return failures == 0 ? 0 : 1;
    }
}
