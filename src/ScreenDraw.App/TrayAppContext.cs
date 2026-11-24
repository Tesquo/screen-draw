using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using ScreenDraw.Core.Diagnostics;
using ScreenDraw.Core.Input;
using ScreenDraw.Core.Overlay;

namespace ScreenDraw.App;

internal sealed class TrayAppContext : ApplicationContext
{
    private static readonly (HotkeyModifiers Modifiers, VirtualKey Key, string Label)[] HotkeyCandidates =
    [
        (HotkeyModifiers.Control | HotkeyModifiers.Alt, VirtualKey.D, "Ctrl + Alt + D"),
        (HotkeyModifiers.Control | HotkeyModifiers.Shift, VirtualKey.D, "Ctrl + Shift + D"),
        (HotkeyModifiers.Control | HotkeyModifiers.Alt, VirtualKey.Q, "Ctrl + Alt + Q"),
    ];

    private readonly NotifyIcon _tray;
    private readonly Icon _icon;
    private readonly HotkeyManager _hotkeys;
    private readonly OverlayController _overlay;
    private readonly SettingsForm _settings;
    private readonly AppSettings _appSettings;
    private readonly System.Windows.Forms.Timer _tracker = new() { Interval = 300 };

    public TrayAppContext(bool armOnStart)
    {
        _hotkeys = new HotkeyManager();
        _overlay = new OverlayController(_hotkeys);
        _appSettings = AppSettings.Load();
        _settings = new SettingsForm(_overlay.Toggle, _appSettings);
        _icon = IconAssets.Tray();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Draw on last window", null, (_, _) => _overlay.Toggle());
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
        _overlay.ActiveChanged += (_, _) => _tray.Text = _overlay.IsActive ? "Screen Draw - drawing" : "Screen Draw";
        _overlay.SnapshotTaken += (_, args) => SaveSnapshot(args.Image);

        var hotkey = RegisterHotkey();
        _settings.SetHotkey(hotkey);
        if (hotkey is null)
        {
            _tray.ShowBalloonTip(
                8000,
                "Screen Draw",
                "No hotkey could be registered. Use the tray menu or the button in this window.",
                ToolTipIcon.Warning);
        }

        _overlay.NoteForeground();
        _tracker.Tick += (_, _) => _overlay.NoteForeground();
        _tracker.Start();

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
            _tracker.Dispose();
            _tray.Dispose();
            _icon.Dispose();
            _overlay.Dispose();
            _hotkeys.Dispose();
            _settings.Dispose();
        }

        base.Dispose(disposing);
    }

    private string? RegisterHotkey()
    {
        foreach (var candidate in HotkeyCandidates)
        {
            if (_hotkeys.TryRegister(candidate.Modifiers, candidate.Key, _overlay.Toggle, out _, out var error))
            {
                Log.Write($"hotkey: using {candidate.Label}");
                return candidate.Label;
            }

            Log.Write($"hotkey: {candidate.Label} unavailable ({error})");
        }

        Log.Write("hotkey: nothing available");
        return null;
    }

    private async Task ArmAfterDelayAsync()
    {
        await Task.Delay(2000);
        _overlay.Toggle();
    }

    private void SaveSnapshot(Bitmap image)
    {
        try
        {
            var folder = _appSettings.SnapshotFolder;
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Write($"snapshot: '{folder}' unusable ({ex.Message}); falling back to the pictures folder");
                folder = AppSettings.DefaultFolder;
                Directory.CreateDirectory(folder);
            }

            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var path = Path.Combine(folder, $"ScreenDraw-{stamp}.png");
            for (var suffix = 2; File.Exists(path); suffix++)
            {
                path = Path.Combine(folder, $"ScreenDraw-{stamp}-{suffix}.png");
            }

            var started = Environment.TickCount64;
            image.Save(path, ImageFormat.Png);
            var copied = TryCopyToClipboard(image);

            Log.Write(
                $"snapshot: {path} ({image.Width}x{image.Height}, {Environment.TickCount64 - started} ms, clipboard={copied})");

            _overlay.Announce(copied
                ? $"saved + copied  ·  {image.Width}×{image.Height}"
                : "saved  ·  clipboard busy");
        }
        catch (Exception ex)
        {
            Log.Error("snapshot: save", ex);
            _overlay.Announce("snapshot failed");
        }
        finally
        {
            image.Dispose();
        }
    }

    private static bool TryCopyToClipboard(Bitmap image)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(40);
            }
        }

        return false;
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
