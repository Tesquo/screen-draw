using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Diagnostics;

public static class ConsoleBridge
{
    public static void AttachToParent()
    {
        if (NativeMethods.AttachConsole(NativeMethods.AttachParentProcess) == 0)
        {
            return;
        }

        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    }
}
