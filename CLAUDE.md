<!--
File: CLAUDE.md
Description: Guidance for Claude Code sessions: commands, layout, architecture and gotchas of Discord Status Updater.
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-06-12
Last edit date: 2026-10-09
Version: 2.2.0
-->

# CLAUDE.md — Discord Status Updater

WPF / .NET 8 desktop app that sets Discord Rich Presence via local IPC. Windows-only. Since 2.0 it also **detects running apps** (VS Code, Visual Studio, Wave Terminal...) and drives the status automatically from editable rules.

## Common commands

```powershell
# Run from repo root
dotnet run --project src/DiscordStatusUpdater/DiscordStatusUpdater.csproj

# Debug build
dotnet build src/DiscordStatusUpdater/DiscordStatusUpdater.csproj

# Unit tests (pure detection logic: TitleParser, TemplateEngine, RuleMatcher, AutoDetectService, PresenceBuilder)
dotnet test tests/DiscordStatusUpdater.Tests/DiscordStatusUpdater.Tests.csproj

# Run against a throw-away settings folder (the %APPDATA% variable is NOT honoured, see Key gotchas)
$env:DISCORD_STATUS_UPDATER_DATA = "$env:TEMP\dsu-test"; dotnet run --project src/DiscordStatusUpdater/DiscordStatusUpdater.csproj

# Self-contained single-file exe (goes to bin/Release/net8.0-windows/win-x64/publish/)
dotnet publish src/DiscordStatusUpdater/DiscordStatusUpdater.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Project layout

```
src/DiscordStatusUpdater/
├── Models/
│   ├── ActivityPreset.cs       # Name + all Rich Presence fields (text fields may contain {tokens})
│   ├── DetectionRule.cs        # INPC rule: process names, title regex, preset name, optional own App ID
│   ├── RunningProcess.cs       # Process snapshot + DetectionMatch records (WPF/Process free)
│   └── AppSettings.cs          # ApplicationId, tray/startup toggles, auto-detect options
├── Services/
│   ├── DiscordRpcService.cs    # Wraps DiscordRpcClient (Lachee NuGet v1.2.1.24); can swap App IDs
│   ├── SettingsService.cs      # JSON R/W (settings, presets, rules) -> %AppData%\DiscordStatusUpdater\
│   ├── ProcessScanner.cs       # Process.GetProcesses + Win32 foreground window/title
│   ├── RuleMatcher.cs          # PURE: which rule/process matches, focus-aware winner
│   ├── AutoDetectService.cs    # Background poller + 2-scan debounce; scanner is injectable for tests
│   ├── TemplateEngine.cs       # PURE: {tokens} and [optional sections]
│   ├── TitleParser.cs          # PURE: "file - folder - App" -> File/Workspace
│   ├── PresenceBuilder.cs      # ActivityPreset + context -> RichPresence
│   └── TrayIconService.cs      # WinForms NotifyIcon + dark context menu
├── ViewModels/
│   ├── MainViewModel.cs        # Connection, presets, settings (partial)
│   ├── MainViewModel.AutoDetect.cs  # Rules editing, detector lifecycle, live card, push/throttle (partial)
│   └── RelayCommand.cs         # ICommand wrapper
├── Controls/PresenceCard.xaml  # Discord-style card used for live status + preset preview
├── Converters/                 # ColorStringToBrush, InverseBool, InverseBoolToVisibility, StringToVisibility, Equals
├── Resources/
│   ├── Themes/DarkTheme.xaml   # Style library (Discord dark palette, NavButton, ToggleSwitch, CardList)
│   └── Presets/                # default_presets.json (27) + default_rules.json (19), embedded as WPF Resources
├── App.xaml / App.xaml.cs      # Startup: creates TrayIconService, wires events
├── MainWindow.xaml             # Sidebar + 4 pages (Dashboard, AutoDetect, Presets, Settings); WindowChrome
└── MainWindow.xaml.cs          # Tray-aware close/minimize logic
tests/DiscordStatusUpdater.Tests/   # xUnit; references the WPF project (net8.0-windows)
technical_memory/technical_memory.ipynb   # decision log (the "why")
docs/                           # incl. auto-detect.md and BUGS.md (authoritative gotcha log)
assets/
├── icon.ico                    # App icon (window + tray), embedded as WPF Resource
└── icon.png                    # Used in hero banner (18×18)
```

## Architecture

- **MVVM** — hand-rolled, no third-party framework. `RelayCommand` uses `CommandManager.RequerySuggested` for CanExecute refresh.
- **Settings** persist to `%AppData%\DiscordStatusUpdater\settings.json`, `presets.json` and `rules.json`. The shipped defaults are WPF *Resources* read via pack URIs (they are not files on disk). On first run of a version that has rules, only the presets those rules reference are merged into an existing `presets.json`.
- **Auto-detect flow** — `AutoDetectService` (thread-pool poll, default 3 s) -> `ProcessScanner` snapshot -> `RuleMatcher.FindMatches/PickWinner` (focused app beats rule order) -> debounce (winner must repeat on 2 scans) -> events -> `MainViewModel.AutoDetect` marshals to the UI thread, renders the preset with `TemplateEngine`, and pushes via `PushPresence`, reconnecting if the rule's own Application ID differs. Title-only refreshes are throttled to one per 15 s (Discord rate limit); app switches go out immediately.
- **Rules are read from a background thread** through an immutable snapshot (`_rulesSnapshot`); edits mark the snapshot + a 600 ms debounced save (flushed by the 1 s `DispatcherTimer`).
- **Disconnect switches auto-detect off** on purpose, otherwise the next scan would reconnect.
- **Tray lifecycle** — `_forceClose` flag in `MainWindow` distinguishes hide-to-tray from real exit. `OnClosing` cancels and hides when `MinimizeToTray=true` and not force-closing. Tray "Exit" and title-bar X both set `_forceClose=true`.
- **Open with Windows** — writes/removes `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\DiscordStatusUpdater`. Source of truth on load is the registry, not the saved setting.

## Key gotchas

### WinForms global using conflict
`<UseWindowsForms>true</UseWindowsForms>` injects `global using System.Drawing` and `global using System.Windows.Forms` project-wide, which conflicts with WPF's `Application`, `Color`, `MessageBox`, etc.

**Fix in .csproj:**
```xml
<ItemGroup>
  <Using Remove="System.Drawing"/>
  <Using Remove="System.Windows.Forms"/>
</ItemGroup>
```
`TrayIconService.cs` then adds explicit `using System.Drawing;` and `using System.Windows.Forms;` since it actually needs them. All other files stay unambiguous.

### WPF placeholder text
WPF `TextBox` has no `PlaceholderText` property (added in .NET 9). Use the `Tag` property instead — `DarkTheme.xaml` has a `ControlTemplate` with a `MultiTrigger` that shows `Tag` as placeholder text when `Text=""` AND `IsFocused=False`.

### StackPanel.Spacing not in .NET 8
`Spacing` on `StackPanel` was added in .NET 9. Use `Margin` on individual children instead.

### System.IO not implicit in WPF temp projects
WPF XAML compilation creates a temp project that doesn't inherit `<ImplicitUsings>`. Add `using System.IO;` explicitly in any service file that uses `Path`, `File`, or `Directory`.

### Embedded assets (pack URIs)
Icons are embedded as WPF `Resource` items in the csproj and accessible via pack URIs even in single-file publish:
```xml
<Resource Include="..\..\assets\icon.ico">
  <Link>Assets/icon.ico</Link>
</Resource>
```
```csharp
GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"))
```

### Embedded JSON defaults are not files
`default_presets.json` / `default_rules.json` are `<Resource>` items. Read them with `Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Presets/..."))`; `File.Exists(BaseDirectory\...)` is always false (this silently produced an empty preset list before 2.0).

### Shipped Application IDs and empty large image keys
16 of the 19 rules in `default_rules.json` carry the ID of a Discord application created for that tool (named and iconed like it). Their presets in `default_presets.json` have an **empty `LargeImageKey`** on purpose: with no large image Discord shows the application's own icon, so no Rich Presence art needs uploading. JetBrains, Postman and SSMS have no application and still use the default ID plus image keys. The presets of VMware Workstation, Tabularis, Remote Desktop Manager and OneCommander use **fixed text only** on purpose: their window titles contain connection names, host names, folder paths and VM names. The IDs are public identifiers (fine in the repo); the local reference list `discord_app_ids.txt` is gitignored. A user's existing `rules.json`/`presets.json` is never overwritten, so shipped changes only reach new installs unless the user deletes those files.

### `%APPDATA%` variable does not move the settings folder
`Environment.GetFolderPath(SpecialFolder.ApplicationData)` ignores the env var. Use `DISCORD_STATUS_UPDATER_DATA` for tests/screenshots, or the run will write to the real profile.

### Discord shows the *application* name
"Playing X" is the Discord application's name, not text we send. Per-tool names need per-rule Application IDs (`DetectionRule.ApplicationId`). `DiscordRpcService.Connect` ignores events from replaced clients (`ReferenceEquals`), otherwise the old client's `OnClose` shows as a disconnect.

### Visibility switching of pages
`EqualsConverter` returns `Visibility` when the target is a `Visibility`, and `bool` for RadioButton `IsChecked` (TwoWay, writes the parameter back). Pages are all in the tree; do not `x:Name` into them from code-behind.

### Verifying UI changes
Run the exe with `DISCORD_STATUS_UPDATER_DATA` set, leave the Application ID empty (so nothing is sent to the user's Discord) and drive/screenshot it with UI Automation. Never kill processes by name; only PIDs you started.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `DiscordRichPresence` (Lachee) | 1.2.1.24 | Discord IPC |
| `Newtonsoft.Json` | 13.0.3 | Settings serialization |
| `xunit` + `Microsoft.NET.Test.Sdk` | 2.9.2 / 17.11.1 | Tests only (`tests/`) |

## Discord color palette (DarkTheme.xaml)

| Token | Hex | Usage |
|-------|-----|-------|
| Bg | `#313338` | Main background |
| BgSecondary | `#2B2D31` | Cards, panels |
| BgTertiary | `#1E1F22` | Deep backgrounds, title bar |
| Blurple | `#5865F2` | Accent, buttons |
| Success | `#23A559` | Connected state |
| Danger | `#F23F43` | Disconnect, error |
