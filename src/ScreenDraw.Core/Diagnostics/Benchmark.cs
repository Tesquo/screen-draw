using System.Diagnostics;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Overlay;
using ScreenDraw.Core.Rendering;

namespace ScreenDraw.Core.Diagnostics;

public static class Benchmark
{
    public static void Run(TextWriter log, Size size, int frames)
    {
        var canvas = new InkCanvas();
        var random = new Random(7);

        for (var stroke = 0; stroke < 30; stroke++)
        {
            var x = 40f + (float)(random.NextDouble() * (size.Width - 240));
            var y = 40f + (float)(random.NextDouble() * (size.Height - 240));

            canvas.BeginStroke(new PointF(x, y));
            for (var point = 1; point < 12; point++)
            {
                canvas.ExtendStroke(new PointF(x + (point * 9f), y + ((point % 3) * 6f)));
            }

            canvas.EndStroke();
        }

        var bounds = new Rectangle(0, 0, size.Width, size.Height);
        using var window = OverlayWindow.CreateForTest(bounds, canvas);
        var hud = new OverlayHud(
            Border: true,
            Cursor: new PointF(size.Width / 2f, size.Height / 2f),
            BrushWidth: 8f);

        for (var i = 0; i < 15; i++)
        {
            window.Renderer.Render(canvas, hud);
        }

        var raster = Measure(frames, () => window.Renderer.Render(canvas, hud));

        for (var i = 0; i < 15; i++)
        {
            window.Renderer.Present();
        }

        var present = Measure(frames, window.Renderer.Present);

        log.WriteLine($"{size.Width}x{size.Height}, 30 strokes, {frames} frames");
        Report(log, "raster", raster);
        Report(log, "present", present);
        Report(log, "frame", raster.Zip(present, (draw, push) => draw + push).ToArray());
    }

    private static double[] Measure(int frames, Action action)
    {
        var samples = new double[frames];
        var stopwatch = new Stopwatch();

        for (var i = 0; i < frames; i++)
        {
            stopwatch.Restart();
            action();
            samples[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        return samples;
    }

    private static void Report(TextWriter log, string label, double[] samples)
    {
        var sorted = samples.OrderBy(value => value).ToArray();
        var average = sorted.Average();
        var p95 = sorted[(int)Math.Min(sorted.Length - 1, Math.Floor(sorted.Length * 0.95))];

        log.WriteLine(
            $"  {label,-8} avg {average,7:0.000} ms   p95 {p95,7:0.000} ms   max {sorted[^1],7:0.000} ms   ceiling {1000 / Math.Max(average, 0.0001),6:0} fps");
    }
}
