# CLAUDE.md — Discord Status Updater

WPF / .NET 8 desktop app that sets Discord Rich Presence via local IPC. Windows-only.

## Common commands

```powershell
# Run from repo root
dotnet run --project src/DiscordStatusUpdater/DiscordStatusUpdater.csproj

# Debug build
dotnet build src/DiscordStatusUpdater/DiscordStatusUpdater.csproj

# Self-contained single-file exe (goes to bin/Release/net8.0-windows/win-x64/publish/)
dotnet publish src/DiscordStatusUpdater/DiscordStatusUpdater.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Project layout

```
src/DiscordStatusUpdater/
├── Models/
│   ├── ActivityPreset.cs       # Name + all Rich Presence fields
│   └── AppSettings.cs          # ApplicationId, last preset, tray/startup toggles
├── Services/
│   ├── DiscordRpcService.cs    # Wraps DiscordRpcClient (Lachee NuGet v1.2.1.24)
│   ├── SettingsService.cs      # JSON R/W → %AppData%\DiscordStatusUpdater\
│   └── TrayIconService.cs      # WinForms NotifyIcon + dark context menu
├── ViewModels/
│   ├── MainViewModel.cs        # All UI state; hand-rolled MVVM
│   └── RelayCommand.cs         # ICommand wrapper
├── Converters/                 # ColorStringToBrush, InverseBool, StringToVisibility
├── Resources/
│   ├── Themes/DarkTheme.xaml   # Full WPF style library (Discord dark palette)
│   └── Presets/default_presets.json
├── App.xaml / App.xaml.cs      # Startup: creates TrayIconService, wires events
├── MainWindow.xaml             # All UI; WindowStyle=None + WindowChrome
└── MainWindow.xaml.cs          # Tray-aware close/minimize logic
assets/
├── icon.ico                    # App icon (window + tray), embedded as WPF Resource
└── icon.png                    # Used in hero banner (18×18)
```

## Architecture

- **MVVM** — hand-rolled, no third-party framework. `RelayCommand` uses `CommandManager.RequerySuggested` for CanExecute refresh.
- **Settings** persist to `%AppData%\DiscordStatusUpdater\settings.json` and `presets.json`. Falls back to `Resources/Presets/default_presets.json` on first run.
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

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `DiscordRichPresence` (Lachee) | 1.2.1.24 | Discord IPC |
| `Newtonsoft.Json` | 13.0.3 | Settings serialization |

## Discord color palette (DarkTheme.xaml)

| Token | Hex | Usage |
|-------|-----|-------|
| Bg | `#313338` | Main background |
| BgSecondary | `#2B2D31` | Cards, panels |
| BgTertiary | `#1E1F22` | Deep backgrounds, title bar |
| Blurple | `#5865F2` | Accent, buttons |
| Success | `#23A559` | Connected state |
| Danger | `#F23F43` | Disconnect, error |
