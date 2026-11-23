using ScreenDraw.Core.Diagnostics;
using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Input;

namespace ScreenDraw.Core.Overlay;

public sealed class OverlayController : IDisposable
{
    private static readonly VirtualKey[] ColorKeys =
        [VirtualKey.D1, VirtualKey.D2, VirtualKey.D3, VirtualKey.D4, VirtualKey.D5];

    private const long HoldDelay = 350;

    private readonly HotkeyManager _hotkeys;
    private readonly List<int> _sessionHotkeys = [];
    private OverlayWindow? _window;
    private nint _lastTarget;
    private long _bracketPressedAt;

    public OverlayController(HotkeyManager hotkeys)
    {
        _hotkeys = hotkeys;
    }

    public InkCanvas Canvas { get; } = new();

    public bool IsActive => _window is not null;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler? ActiveChanged;

    /// <summary>Raised on every snapshot. The receiver owns the image and must dispose it.</summary>
    public event EventHandler<SnapshotEventArgs>? SnapshotTaken;

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

        _window.Held += (_, _) => RepeatWidth();

        RegisterSessionHotkey(VirtualKey.Escape, Dismiss, "Esc");
        RegisterToolHotkeys();
        RegisterSessionHotkey(VirtualKey.Space, TakeSnapshot, "Space");

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

        foreach (var id in _sessionHotkeys)
        {
            _hotkeys.Unregister(id);
        }

        _sessionHotkeys.Clear();
        _window.Dispose();
        _window = null;
        Log.Write("overlay: dismissed");
        Report("Overlay closed.");
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => Dismiss();

    public void Announce(string text) => _window?.Announce(text);

    private void TakeSnapshot()
    {
        if (_window is null)
        {
            return;
        }

        Bitmap image;
        try
        {
            image = _window.Snapshot();
        }
        catch (Exception ex)
        {
            Log.Error("snapshot: capture", ex);
            Announce("snapshot failed");
            return;
        }

        try
        {
            if (SnapshotTaken is { } handler)
            {
                handler.Invoke(this, new SnapshotEventArgs(image));
            }
            else
            {
                image.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error("snapshot: handler", ex);
            image.Dispose();
            Announce("snapshot failed");
        }
    }

    private void RegisterToolHotkeys()
    {
        for (var index = 0; index < Palette.Colors.Length && index < ColorKeys.Length; index++)
        {
            var color = index;
            RegisterSessionHotkey(ColorKeys[index], () =>
            {
                Canvas.SetColor(color);
                Announce();
            }, $"colour {index + 1}");
        }

        RegisterSessionHotkey(VirtualKey.E, () =>
        {
            Canvas.ToggleEraser();
            Announce();
        }, "E");

        RegisterSessionHotkey(VirtualKey.B, () =>
        {
            Canvas.UsePen();
            Announce();
        }, "B");

        RegisterSessionHotkey(VirtualKey.OemOpenBrackets, () => StepWidth(-1f), "[");
        RegisterSessionHotkey(VirtualKey.OemCloseBrackets, () => StepWidth(1f), "]");
    }

    private void RegisterSessionHotkey(VirtualKey key, Action action, string label)
    {
        if (_hotkeys.TryRegister(HotkeyModifiers.None, key, action, out var id, out var error))
        {
            _sessionHotkeys.Add(id);
            return;
        }

        Log.Write($"arm: {label} unavailable ({error})");
    }

    private void StepWidth(float delta)
    {
        Canvas.AdjustWidth(delta);
        _bracketPressedAt = Environment.TickCount64;
        Announce();
    }

    private void RepeatWidth()
    {
        if (_window is null || Environment.TickCount64 - _bracketPressedAt < HoldDelay)
        {
            return;
        }

        var narrower = KeyHold.IsDown(VirtualKey.OemOpenBrackets);
        var wider = KeyHold.IsDown(VirtualKey.OemCloseBrackets);
        if (!narrower && !wider)
        {
            return;
        }

        Canvas.AdjustWidth(narrower ? -1f : 1f);
        _bracketPressedAt = Environment.TickCount64;
        Announce();
    }

    private void Announce() => _window?.Announce(Canvas.Describe());

    private void Report(string message) => StatusChanged?.Invoke(this, message);
}
