using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Input;

internal static class KeyHold
{
    public static bool IsDown(VirtualKey key) => (NativeMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;
}
