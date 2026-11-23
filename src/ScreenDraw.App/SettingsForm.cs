using ScreenDraw.Core.Diagnostics;

namespace ScreenDraw.App;

internal sealed class SettingsForm : Form
{
    private readonly Label _status;
    private readonly Label _hotkey;

    public SettingsForm(Action draw)
    {
        Text = "Screen Draw";
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(430, 372);

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
            Text = "arms and disarms the overlay",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(196, 93),
        };

        var controls = new Label
        {
            Text = "Left drag      ink\nMouse wheel    brush size\nMiddle click   undo\nRight click    clear\nEsc            finish",
            Font = new Font("Consolas", 9.5f),
            AutoSize = true,
            Location = new Point(23, 124),
        };

        _status = new Label
        {
            Text = "Focus the window you want to draw on, then press the hotkey.",
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
            Location = new Point(23, 246),
            Size = new Size(384, 46),
        };

        var drawButton = new Button
        {
            Text = "Draw on last window",
            Location = new Point(233, 302),
            Size = new Size(174, 30),
        };

        drawButton.Click += (_, _) => draw();

        var logButton = new Button
        {
            Text = "Open log",
            Location = new Point(23, 302),
            Size = new Size(96, 30),
        };

        logButton.Click += (_, _) => OpenLog();

        var footer = new Label
        {
            Text = "M0.1 · no capture, no injection, no focus stealing",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(23, 342),
        };

        Controls.AddRange([title, subtitle, _hotkey, hotkeyHint, controls, _status, drawButton, logButton, footer]);
    }

    public void SetStatus(string message) => _status.Text = message;

    public void SetHotkey(string? label) => _hotkey.Text = label ?? "(none — use the tray menu)";

    private static void OpenLog()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Log.FilePath)!);
            if (!File.Exists(Log.FilePath))
            {
                File.WriteAllText(Log.FilePath, string.Empty);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Log.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show($"Could not open {Log.FilePath}.{Environment.NewLine}{ex.Message}", "Screen Draw");
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
