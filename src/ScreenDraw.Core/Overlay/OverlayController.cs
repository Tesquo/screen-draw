using ScreenDraw.Core.Diagnostics;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Input;

namespace ScreenDraw.Core.Overlay;

public sealed class OverlayController : IDisposable
{
    private static readonly VirtualKey[] ColorKeys =
        [VirtualKey.D1, VirtualKey.D2, VirtualKey.D3, VirtualKey.D4, VirtualKey.D5];

    private readonly HotkeyManager _hotkeys;
    private readonly List<int> _toolHotkeys = [];
    private OverlayWindow? _window;
    private int? _escapeHotkey;
    private nint _lastTarget;

    public OverlayController(HotkeyManager hotkeys)
    {
        _hotkeys = hotkeys;
    }

    public InkCanvas Canvas { get; } = new();

    public bool IsActive => _window is not null;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler? ActiveChanged;

    public void NoteForeground()
    {
        if (WindowTarget.Foreground() is { } target)
        {
            _lastTarget = target.Handle;
        }
    }

    public void Toggle()
    {
        if (IsActive)
        {
            Dismiss();
            return;
        }

        var target = WindowTarget.Foreground() ?? WindowTarget.FromHandle(_lastTarget);
        if (target is not { } window)
        {
            Log.Write("arm: no drawable window");
            Report("Focus the window you want to draw on, then press the hotkey again.");
            return;
        }

        Canvas.Clear();
        _lastTarget = window.Handle;

        try
        {
            _window = new OverlayWindow(window.Bounds, Canvas);
        }
        catch (Exception ex)
        {
            Log.Error("arm: overlay creation", ex);
            Dismiss();
            Report($"Could not start drawing: {ex.Message}");
            return;
        }

        if (_hotkeys.TryRegister(HotkeyModifiers.None, VirtualKey.Escape, Dismiss, out var escape, out var escapeError))
        {
            _escapeHotkey = escape;
        }
        else
        {
            Log.Write($"arm: Esc unavailable ({escapeError}); close with the main hotkey instead");
        }

        RegisterToolHotkeys();
        Log.Write($"arm: drawing on '{window.Title}' hwnd=0x{window.Handle:X} rect={window.Bounds}");
        Report($"Drawing on {window.Title}. Left drag to ink, middle click to undo, right click to clear, Esc to finish.");
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dismiss()
    {
        if (_window is null)
        {
            return;
        }

        if (_escapeHotkey is { } escape)
        {
            _hotkeys.Unregister(escape);
            _escapeHotkey = null;
        }

        foreach (var id in _toolHotkeys)
        {
            _hotkeys.Unregister(id);
        }

        _toolHotkeys.Clear();
        _window.Dispose();
        _window = null;
        Log.Write("overlay: dismissed");
        Report("Overlay closed.");
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => Dismiss();

    private void RegisterToolHotkeys()
    {
        for (var index = 0; index < Palette.Colors.Length && index < ColorKeys.Length; index++)
        {
            var color = index;
            RegisterTool(ColorKeys[index], () =>
            {
                Canvas.SetColor(color);
                Announce();
            });
        }

        RegisterTool(VirtualKey.E, () =>
        {
            Canvas.ToggleEraser();
            Announce();
        });

        RegisterTool(VirtualKey.B, () =>
        {
            Canvas.UsePen();
            Announce();
        });

        RegisterTool(VirtualKey.OemOpenBrackets, () =>
        {
            Canvas.AdjustWidth(-1f);
            Announce();
        });

        RegisterTool(VirtualKey.OemCloseBrackets, () =>
        {
            Canvas.AdjustWidth(1f);
            Announce();
        });
    }

    private void RegisterTool(VirtualKey key, Action action)
    {
        if (_hotkeys.TryRegister(HotkeyModifiers.None, key, action, out var id, out var error))
        {
            _toolHotkeys.Add(id);
            return;
        }

        Log.Write($"tools: {key} unavailable ({error})");
    }

    private void Announce() => _window?.Announce(Canvas.Describe());

    private void Report(string message) => StatusChanged?.Invoke(this, message);
}
