namespace DiscordStatusUpdater.Models;

public class ActivityPreset
{
    public string Name { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string LargeImageKey { get; set; } = string.Empty;
    public string LargeImageText { get; set; } = string.Empty;
    public string SmallImageKey { get; set; } = string.Empty;
    public string SmallImageText { get; set; } = string.Empty;
    public bool ShowElapsedTime { get; set; } = true;
    public string Button1Label { get; set; } = string.Empty;
    public string Button1Url { get; set; } = string.Empty;
    public string Button2Label { get; set; } = string.Empty;
    public string Button2Url { get; set; } = string.Empty;

    public override string ToString() => Name;
}
