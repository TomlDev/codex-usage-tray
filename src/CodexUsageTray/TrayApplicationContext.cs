using CodexUsageTray.Core;

namespace CodexUsageTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon tray = new();
    private readonly UsagePopup popup = new();
    private readonly CodexAppServerClient client = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 60_000 };
    private UsageSnapshot? snapshot;
    private Icon? currentIcon;

    public TrayApplicationContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Jetzt aktualisieren", null, async (_, _) => await RefreshAsync());
        var startup = new ToolStripMenuItem("Mit Windows starten") { Checked = StartupManager.IsEnabled, CheckOnClick = true };
        startup.CheckedChanged += (_, _) => StartupManager.SetEnabled(startup.Checked);
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitThread());
        tray.ContextMenuStrip = menu;
        tray.Text = "Codex: Nutzung wird abgefragt…";
        SetIcon(TrayIconRenderer.Render(0, error: true));
        tray.Visible = true;
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) TogglePopup(); };
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            snapshot = await client.ReadUsageAsync(timeout.Token);
            var remaining = snapshot.LongestWindow?.RemainingPercent ?? 0;
            SetIcon(TrayIconRenderer.Render(remaining));
            tray.Text = $"Codex: {remaining:0}% verfügbar";
            popup.ShowSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            SetIcon(TrayIconRenderer.Render(0, error: true));
            tray.Text = "Codex: Abfrage fehlgeschlagen";
            popup.ShowError(FriendlyError(ex));
        }
    }

    private static string FriendlyError(Exception ex) => ex switch
    {
        System.ComponentModel.Win32Exception => "Codex CLI nicht gefunden. Installieren bzw. zum PATH hinzufügen und dann aktualisieren.",
        OperationCanceledException => "Codex hat zu lange nicht geantwortet.",
        _ when ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) => "Codex ist nicht bei ChatGPT angemeldet. Mit „codex login“ anmelden und erneut versuchen.",
        _ => $"Limit konnte nicht gelesen werden: {ex.Message}"
    };

    private void TogglePopup()
    {
        if (popup.Visible) popup.Hide();
        else { if (snapshot is not null) popup.ShowSnapshot(snapshot); popup.ShowAtTray(); }
    }

    private void SetIcon(Icon icon) { var old = currentIcon; currentIcon = icon; tray.Icon = icon; old?.Dispose(); }

    protected override void ExitThreadCore()
    {
        timer.Stop(); tray.Visible = false; tray.Dispose(); popup.Dispose(); currentIcon?.Dispose(); client.DisposeAsync().AsTask().GetAwaiter().GetResult(); base.ExitThreadCore();
    }
}
