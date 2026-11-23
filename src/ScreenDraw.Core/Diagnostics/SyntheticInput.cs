using System.Runtime.InteropServices;
using ScreenDraw.Core.Interop;

namespace ScreenDraw.Core.Diagnostics;

internal static class SyntheticInput
{
    public static uint Press(ushort first, ushort second, ushort third, ushort fourth)
    {
        ushort[] keys = [first, second, third, fourth];
        var inputs = new InputEvent[keys.Length * 2];

        for (var i = 0; i < keys.Length; i++)
        {
            inputs[i] = Key(keys[i], up: false);
        }

        for (var i = 0; i < keys.Length; i++)
        {
            inputs[keys.Length + i] = Key(keys[keys.Length - 1 - i], up: true);
        }

        return NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<InputEvent>());
    }

    public static uint KeyDown(ushort virtualKey) => Send(Key(virtualKey, up: false));

    public static uint KeyUp(ushort virtualKey) => Send(Key(virtualKey, up: true));

    private static uint Send(InputEvent input)
    {
        InputEvent[] inputs = [input];
        return NativeMethods.SendInput(1, inputs, Marshal.SizeOf<InputEvent>());
    }

    private static InputEvent Key(ushort virtualKey, bool up) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = virtualKey,
                Flags = up ? NativeMethods.KeyEventKeyUp : 0,
            },
        },
    };
}
