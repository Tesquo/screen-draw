using System.Drawing.Imaging;
using ScreenDraw.Core.Capture;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Input;
using ScreenDraw.Core.Interop;
using ScreenDraw.Core.Overlay;
using ScreenDraw.Core.Rendering;

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

        Check("tools describe themselves", () =>
        {
            canvas.SetColor(1);
            var coloured = canvas.Describe() == "yellow · 4 px";
            canvas.ToggleEraser();
            var erasing = canvas.Describe() == "eraser · 4 px";
            canvas.UsePen();
            canvas.ToggleHighlighter();
            var highlighting = canvas.Describe() == "highlighter · yellow · 4 px";
            canvas.UsePen();
            return coloured && erasing && highlighting && canvas.Tool == InkTool.Pen;
        });

        if (SyntheticInput.KeyDown((ushort)VirtualKey.OemCloseBrackets) == 0)
        {
            log.WriteLine("SKIP  held keys (input synthesis unavailable)");
        }
        else
        {
            var held = KeyHold.IsDown(VirtualKey.OemCloseBrackets);
            SyntheticInput.KeyUp((ushort)VirtualKey.OemCloseBrackets);
            Thread.Sleep(30);
            var released = !KeyHold.IsDown(VirtualKey.OemCloseBrackets);

            if (held && released)
            {
                log.WriteLine("PASS  held keys are observable");
            }
            else
            {
                failures++;
                log.WriteLine($"FAIL  held keys are observable (held={held}, released={released})");
            }
        }

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
                Check("empty pixels stay hit-testable but invisible", () => frame.GetPixel(300, 180).A is >= 1 and <= 2);

                window.Renderer.Render(canvas, new OverlayHud(Cursor: new PointF(120f, 140f), BrushWidth: 24f));
                Check("brush ring marks the cursor", () => frame.GetPixel(120, 128).A > 0);
                Check("brush ring is hollow", () => frame.GetPixel(120, 135).A <= 2);

                window.Renderer.Render(canvas, new OverlayHud(Cursor: new PointF(120f, 140f), BrushWidth: 40f));
                Check("brush ring follows the brush size", () => frame.GetPixel(120, 121).A > 0);

                var marker = new InkCanvas { Tool = InkTool.Highlighter, Width = 28f };
                marker.BeginStroke(new PointF(200f, 60f));
                marker.ExtendStroke(new PointF(300f, 60f));
                marker.EndStroke();
                window.Renderer.Render(marker, OverlayHud.None);
                var markerAlpha = frame.GetPixel(250, 60).A;
                Check("highlighter stays translucent", () => markerAlpha > 40 && markerAlpha < 250);

                window.Renderer.Render(
                    canvas,
                    new OverlayHud(Cursor: new PointF(160f, 150f), BrushWidth: 24f, BrushColor: Palette.Black));

                var lightHalo = 0;
                for (var y = 132; y <= 168; y++)
                {
                    for (var x = 142; x <= 178; x++)
                    {
                        var pixel = frame.GetPixel(x, y);
                        if (pixel.A > 40 && pixel.R > 150 && pixel.G > 150 && pixel.B > 150)
                        {
                            lightHalo++;
                        }
                    }
                }

                Check("ring halo stays light for dark ink", () => lightHalo > 0);

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

        Check("screen capture returns content", () =>
        {
            using var captured = ScreenCapture.Capture(new Rectangle(0, 0, 256, 256));
            var lowest = 255;
            var highest = 0;

            for (var offset = 0; offset < 256; offset += 16)
            {
                var pixel = captured.GetPixel(offset, offset);
                var light = (pixel.R + pixel.G + pixel.B) / 3;
                lowest = Math.Min(lowest, light);
                highest = Math.Max(highest, light);
            }

            return highest - lowest > 8;
        });

        var snapshotCanvas = new InkCanvas();        snapshotCanvas.Width = 6f;
        snapshotCanvas.BeginStroke(new PointF(20f, 20f));
        snapshotCanvas.ExtendStroke(new PointF(90f, 60f));
        snapshotCanvas.ExtendStroke(new PointF(170f, 120f));
        snapshotCanvas.EndStroke();

        try
        {
            using var visible = new OverlayWindow(new Rectangle(60, 60, 220, 160), snapshotCanvas);
            Thread.Sleep(150);
            using var snapshot = visible.Snapshot();

            var ink = 0;
            var brightest = 0;
            for (var x = 20; x < 170; x += 3)
            {
                for (var y = 20; y < 120; y += 3)
                {
                    var pixel = snapshot.GetPixel(x, y);
                    brightest = Math.Max(brightest, (pixel.R + pixel.G + pixel.B) / 3);
                    if (pixel.R > 180 && pixel.G > 150 && pixel.B < 120)
                    {
                        ink++;
                    }
                }
            }

            if (ink > 20)
            {
                log.WriteLine("PASS  snapshot captures the ink without chrome");
            }
            else if (brightest < 10)
            {
                log.WriteLine("SKIP  snapshot (no visible desktop to capture)");
            }
            else
            {
                failures++;
                log.WriteLine($"FAIL  snapshot captures the ink without chrome (ink pixels {ink}, brightest {brightest})");
            }
        }
        catch (Exception ex)
        {
            failures++;
            log.WriteLine($"FAIL  snapshot ({ex.GetType().Name}: {ex.Message})");
        }

        using (var hotkeys = new HotkeyManager())
        {
            var fired = false;
            if (!hotkeys.TryRegister(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    VirtualKey.F9,
                    () => fired = true,
                    out var id,
                    out var error))
            {
                log.WriteLine($"SKIP  hotkey registration ({error})");
            }
            else
            {
                log.WriteLine("PASS  hotkey registration");

                var injected = SyntheticInput.Press(0x11, 0x12, 0x10, 0x78);
                var deadline = Environment.TickCount64 + 1500;
                while (!fired && Environment.TickCount64 < deadline)
                {
                    PumpMessages();
                    Thread.Sleep(10);
                }

                if (injected == 0)
                {
                    log.WriteLine("SKIP  hotkey delivery (input synthesis unavailable)");
                }
                else
                {
                    Check("hotkey delivers WM_HOTKEY", () => fired);
                }

                hotkeys.Unregister(id);
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private static void PumpMessages()
    {
        while (NativeMethods.PeekMessage(out var message, 0, 0, 0, NativeMethods.PmRemove) != 0)
        {
            NativeMethods.TranslateMessage(ref message);
            NativeMethods.DispatchMessage(ref message);
        }
    }
}
