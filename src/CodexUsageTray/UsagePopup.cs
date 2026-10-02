using CodexUsageTray.Core;

namespace CodexUsageTray;

internal sealed class UsagePopup : Form
{
    private readonly Label title = new() { Text = "Codex", Font = new Font("Segoe UI Semibold", 14), AutoSize = true };
    private readonly FlowLayoutPanel buckets = new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Width = 330, Height = 175 };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5f) };

    public UsagePopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(31, 33, 38);
        ForeColor = Color.White;
        ClientSize = new Size(354, 248);
        Padding = new Padding(12);
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        layout.Controls.Add(title); layout.Controls.Add(buckets); layout.Controls.Add(status);
        Controls.Add(layout);
        Deactivate += (_, _) => Hide();
    }

    public void ShowSnapshot(UsageSnapshot snapshot)
    {
        buckets.Controls.Clear();
        foreach (var bucket in snapshot.Buckets)
        {
            AddWindow(bucket.Name ?? bucket.Id, bucket.Primary);
            if (bucket.Secondary is not null) AddWindow("Sekundäres Limit", bucket.Secondary);
        }
        if (buckets.Controls.Count == 0) buckets.Controls.Add(new Label { Text = "Keine Limits gemeldet.", AutoSize = true });
        status.Text = $"Aktualisiert {snapshot.FetchedAt:HH:mm:ss} · Rechtsklick für Optionen";
    }

    public void ShowError(string message)
    {
        buckets.Controls.Clear();
        buckets.Controls.Add(new Label { Text = message, AutoSize = false, Width = 320, Height = 120, ForeColor = Color.FromArgb(255, 190, 100) });
        status.Text = "Wird automatisch erneut versucht.";
    }

    private void AddWindow(string name, UsageWindow? window)
    {
        if (window is null) return;
        var panel = new Panel { Width = 320, Height = 68, Margin = new Padding(0, 5, 0, 2) };
        var heading = new Label { Text = $"{name} · {DisplayFormatting.WindowName(window.Duration)}", AutoSize = true, Location = new Point(0, 0) };
        var value = new Label { Text = $"{window.RemainingPercent:0}% verfügbar", AutoSize = true, Font = new Font("Segoe UI Semibold", 10), Location = new Point(0, 23) };
        var reset = new Label { Text = DisplayFormatting.ResetText(window.ResetsAt, DateTimeOffset.Now), AutoSize = true, ForeColor = Color.Silver, Location = new Point(190, 25) };
        var bar = new ProgressBar { Minimum = 0, Maximum = 100, Value = (int)Math.Round(window.RemainingPercent), Width = 320, Height = 8, Location = new Point(0, 50) };
        panel.Controls.AddRange([heading, value, reset, bar]); buckets.Controls.Add(panel);
    }

    public void ShowAtTray()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Cursor.Position);
        Location = new Point(area.Right - Width - 10, area.Bottom - Height - 10);
        Show(); Activate();
    }
}
