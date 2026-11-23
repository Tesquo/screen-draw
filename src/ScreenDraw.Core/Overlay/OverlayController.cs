using ScreenDraw.Core.Drawing;
using ScreenDraw.Core.Input;

namespace ScreenDraw.Core.Overlay;

public sealed class OverlayController : IDisposable
{
    private readonly HotkeyManager _hotkeys;
    private OverlayWindow? _window;
    private int? _escapeHotkey;

    public OverlayController(HotkeyManager hotkeys)
    {
        _hotkeys = hotkeys;
    }

    public InkCanvas Canvas { get; } = new();

    public bool IsActive => _window is not null;

    public event EventHandler<string>? StatusChanged;

    public event EventHandler? ActiveChanged;

    public void Toggle()
    {
        if (IsActive)
        {
            Dismiss();
            return;
        }

        var target = WindowTarget.Foreground();
        if (target is not { } window)
        {
            Report("Focus the window you want to draw on, then press the hotkey again.");
            return;
        }

        Canvas.Clear();

        try
        {
            _window = new OverlayWindow(window.Bounds, Canvas);
            _escapeHotkey = _hotkeys.Register(HotkeyModifiers.None, VirtualKey.Escape, Dismiss);
        }
        catch (Exception ex)
        {
            Dismiss();
            Report($"Could not start drawing: {ex.Message}");
            return;
        }

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

        _window.Dispose();
        _window = null;
        Report("Overlay closed.");
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => Dismiss();

    private void Report(string message) => StatusChanged?.Invoke(this, message);
}
