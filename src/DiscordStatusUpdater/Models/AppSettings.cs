namespace DiscordStatusUpdater.Models;

public class AppSettings
{
    public string ApplicationId { get; set; } = string.Empty;
    public string LastPresetName { get; set; } = string.Empty;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool OpenWithWindows { get; set; } = false;
}
