using ScreenDraw.Core.Diagnostics;

namespace ScreenDraw.App;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Label _status;
    private readonly Label _hotkey;
    private readonly Label _folder;

    public SettingsForm(Action draw, AppSettings settings)
    {
        _settings = settings;

        Text = "Screen Draw";
        Icon = IconAssets.Window();
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(430, 462);

        var title = new Label
        {
            Text = "Screen Draw",
            Font = new Font("Segoe UI Semibold", 17f),
            AutoSize = true,
            Location = new Point(20, 16),
        };

        var subtitle = new Label
        {
            Text = "Draw on a window while it keeps running.",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(23, 52),
        };

        _hotkey = new Label
        {
            Text = "(none)",
            Font = new Font("Segoe UI Semibold", 13f),
            AutoSize = true,
            Location = new Point(22, 86),
        };

        var hotkeyHint = new Label
        {
            Text = "Arms and disarms the overlay",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(196, 93),
        };

        var controls = new Label
        {
            Text = "Ink            left drag\nBrush size     mouse wheel  /  [  ]  hold\nColour         1 – 6\nEraser         E\nHighlighter    H\nUndo / clear   middle / right click\nSave + copy    Space\nFinish         Esc",
            Font = new Font("Consolas", 9.5f),
            AutoSize = true,
            Location = new Point(23, 124),
        };

        _status = new Label
        {
            Text = "Focus the window you want to draw on, then press the hotkey.",
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
            Location = new Point(23, 252),
            Size = new Size(384, 38),
        };

        var snapshots = new Label
        {
            Text = "Snapshots",
            Font = new Font("Segoe UI Semibold", 9f),
            AutoSize = true,
            Location = new Point(23, 300),
        };

        _folder = new Label
        {
            Text = settings.SnapshotFolder,
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
            Location = new Point(23, 320),
            Size = new Size(384, 18),
        };

        var changeFolder = new Button
        {
            Text = "Change folder…",
            Location = new Point(23, 344),
            Size = new Size(130, 28),
        };

        changeFolder.Click += (_, _) => ChooseFolder();

        var openFolder = new Button
        {
            Text = "Open folder",
            Location = new Point(163, 344),
            Size = new Size(104, 28),
        };

        openFolder.Click += (_, _) => OpenFolder();

        var drawButton = new Button
        {
            Text = "Draw on last window",
            Location = new Point(233, 388),
            Size = new Size(174, 30),
        };

        drawButton.Click += (_, _) => draw();

        var logButton = new Button
        {
            Text = "Open log",
            Location = new Point(23, 388),
            Size = new Size(96, 30),
        };

        logButton.Click += (_, _) => OpenPath(Log.FilePath);

        var footer = new Label
        {
            Text = "No capture hooking, no injection, no focus stealing",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(23, 432),
        };

        Controls.AddRange([title, subtitle, _hotkey, hotkeyHint, controls, _status, snapshots, _folder, changeFolder, openFolder, drawButton, logButton, footer]);
    }

    public void SetStatus(string message) => _status.Text = message;

    public void SetHotkey(string? label) => _hotkey.Text = label ?? "(none — use the tray menu)";

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Where should snapshots be saved?",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_settings.SnapshotFolder) ? _settings.SnapshotFolder : AppSettings.DefaultFolder,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        _settings.SnapshotFolder = dialog.SelectedPath;
        _settings.Save();
        _folder.Text = _settings.SnapshotFolder;
    }

    private void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(_settings.SnapshotFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not open {_settings.SnapshotFolder}. {ex.Message}");
            return;
        }

        OpenPath(_settings.SnapshotFolder);
    }

    private void OpenPath(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (!File.Exists(path) && Path.HasExtension(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            SetStatus($"Could not open {path}. {ex.Message}");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }
}
