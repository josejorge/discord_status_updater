// File: DetectionRule.cs
// Description: A user-editable rule that maps running processes to an activity preset for automatic status detection.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.1.0

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DiscordStatusUpdater.Models;

/// <summary>
/// Describes which running application triggers which preset. Rules are evaluated in list order,
/// so a rule higher in the list wins when several matching applications are open.
/// Implements change notification because the editor and the rule list bind to the same instance.
/// </summary>
public class DetectionRule : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _enabled = true;
    private string _processNames = string.Empty;
    private string _titleRegex = string.Empty;
    private string _presetName = string.Empty;
    private string _applicationId = string.Empty;

    /// <summary>Stable identity so the matcher/debouncer can track a rule across renames and re-ordering.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Display name; also exposed to templates as the {app} token.</summary>
    public string Name { get => _name; set => Set(ref _name, value); }

    /// <summary>Disabled rules are kept in the list but ignored by the matcher.</summary>
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

    /// <summary>Comma-separated process names without ".exe", e.g. "Code, Code - Insiders".</summary>
    public string ProcessNames { get => _processNames; set => Set(ref _processNames, value); }

    /// <summary>Optional regex the window title must match (empty = any title).</summary>
    public string TitleRegex { get => _titleRegex; set => Set(ref _titleRegex, value); }

    /// <summary>Name of the <see cref="ActivityPreset"/> that supplies the presence text and images.</summary>
    public string PresetName { get => _presetName; set => Set(ref _presetName, value); }

    /// <summary>
    /// Optional Discord Application ID. Discord shows "Playing &lt;application name&gt;", so a rule needs
    /// its own application to display a custom name. Empty = use the default Application ID.
    /// </summary>
    public string ApplicationId { get => _applicationId; set => Set(ref _applicationId, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public override string ToString() => Name;
}
