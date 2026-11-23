using System.Runtime.InteropServices;
using ScreenDraw.Core.Diagnostics;

namespace ScreenDraw.App;

internal static class Program
{
    private const string MutexName = "ScreenDraw.SingleInstance";

    [STAThread]
    private static int Main(string[] args)
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        Log.Write($"---- Screen Draw {version} | {RuntimeInformation.OSDescription} | {RuntimeInformation.ProcessArchitecture} | pid {Environment.ProcessId} ----");

        if (HasFlag(args, "--self-test"))
        {
            return RunSelfTest();
        }

        if (HasFlag(args, "--bench"))
        {
            return RunBenchmark(args);
        }

        using var mutex = new Mutex(true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "Screen Draw is already running. Look for it in the system tray.",
                "Screen Draw",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var context = new TrayAppContext(HasFlag(args, "--arm"));
        Application.Run(context);
        return 0;
    }

    private static int RunSelfTest()
    {
        var path = Path.Combine(Path.GetTempPath(), "screendraw-selftest.txt");

        try
        {
            int exitCode;
            using (var log = new StreamWriter(path))
            {
                exitCode = SelfTest.Run(log);
                log.WriteLine($"RESULT: {(exitCode == 0 ? "PASS" : "FAIL")}  ({path})");
            }

            ConsoleBridge.AttachToParent();
            Console.WriteLine(File.ReadAllText(path));
            return exitCode;
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }
    }

    private static int RunBenchmark(string[] args)
    {
        var size = new Size(2560, 1440);
        foreach (var argument in args)
        {
            var parts = argument.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
            {
                size = new Size(width, height);
            }
        }

        var path = Path.Combine(Path.GetTempPath(), "screendraw-bench.txt");

        try
        {
            using (var log = new StreamWriter(path))
            {
                Benchmark.Run(log, size, 200);
            }

            ConsoleBridge.AttachToParent();
            Console.WriteLine(File.ReadAllText(path));
            return 0;
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }
    }

    private static int Fail(string path, Exception exception)
    {
        try
        {
            File.WriteAllText(path, exception.ToString());
        }
        catch (IOException)
        {
        }

        ConsoleBridge.AttachToParent();
        Console.WriteLine(exception);
        return 1;
    }

    private static bool HasFlag(string[] args, string flag) => args.Contains(flag, StringComparer.OrdinalIgnoreCase);
}
