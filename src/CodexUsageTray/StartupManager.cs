using Microsoft.Win32;

namespace CodexUsageTray;

internal static class StartupManager
{
    private const string KeyName = "CodexUsageTray";
    public static bool IsEnabled => Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")?.GetValue(KeyName) is not null;
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true)
            ?? throw new InvalidOperationException("Windows-Autostart-Einstellung konnte nicht geöffnet werden.");
        if (enabled) key.SetValue(KeyName, $"\"{Application.ExecutablePath}\""); else key.DeleteValue(KeyName, false);
    }
}
