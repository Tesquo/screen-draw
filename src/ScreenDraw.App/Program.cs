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
        int exitCode;

        using (var log = new StreamWriter(path))
        {
            exitCode = SelfTest.Run(log);
            log.WriteLine($"RESULT: {(exitCode == 0 ? "PASS" : "FAIL")}  ({path})");
        }

        ConsoleBridge.AttachToParent();
        Console.WriteLine(File.ReadAllText(path));
        return exitCode;
    }    private static bool HasFlag(string[] args, string flag) => args.Contains(flag, StringComparer.OrdinalIgnoreCase);
}
