using System.ComponentModel;
using ScreenDraw.Core.Input;
using ScreenDraw.Core.Overlay;

namespace ScreenDraw.App;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;
    private readonly HotkeyManager _hotkeys;
    private readonly OverlayController _overlay;
    private readonly SettingsForm _settings;

    public TrayAppContext(bool armOnStart)
    {
        _hotkeys = new HotkeyManager();
        _overlay = new OverlayController(_hotkeys);
        _settings = new SettingsForm();
        _icon = (Icon)SystemIcons.Application.Clone();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show window", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Exit());

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "Screen Draw",
            Visible = true,
            ContextMenuStrip = menu,
        };

        _tray.DoubleClick += (_, _) => ShowSettings();

        _overlay.StatusChanged += (_, message) => _settings.SetStatus(message);
        _overlay.ActiveChanged += (_, _) => _tray.Text = _overlay.IsActive ? "Screen Draw — drawing" : "Screen Draw";

        try
        {
            _hotkeys.Register(HotkeyModifiers.Control | HotkeyModifiers.Alt, VirtualKey.D, _overlay.Toggle);
        }
        catch (Win32Exception ex)
        {
            _settings.SetStatus($"Could not register Ctrl+Alt+D. {ex.Message}");
        }

        if (armOnStart)
        {
            _ = ArmAfterDelayAsync();
        }
        else
        {
            ShowSettings();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _icon.Dispose();
            _overlay.Dispose();
            _hotkeys.Dispose();
            _settings.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task ArmAfterDelayAsync()
    {
        await Task.Delay(2000);
        _overlay.Toggle();
    }

    private void ShowSettings()
    {
        _settings.Show();
        _settings.WindowState = FormWindowState.Normal;
        _settings.Activate();
    }

    private void Exit()
    {
        _tray.Visible = false;
        ExitThread();
    }
}
