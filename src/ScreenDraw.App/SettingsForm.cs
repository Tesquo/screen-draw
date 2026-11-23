namespace ScreenDraw.App;

internal sealed class SettingsForm : Form
{
    private readonly Label _status;

    public SettingsForm()
    {
        Text = "Screen Draw";
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(430, 342);

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

        var hotkey = new Label
        {
            Text = "Ctrl + Alt + D",
            Font = new Font("Segoe UI Semibold", 13f),
            AutoSize = true,
            Location = new Point(22, 86),
        };

        var hotkeyHint = new Label
        {
            Text = "arms and disarms the overlay",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(168, 93),
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
            Location = new Point(23, 244),
            Size = new Size(384, 44),
        };

        var footer = new Label
        {
            Text = "M0 · no capture, no injection, no focus stealing",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(23, 306),
        };

        Controls.AddRange([title, subtitle, hotkey, hotkeyHint, controls, _status, footer]);
    }

    public void SetStatus(string message) => _status.Text = message;

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
