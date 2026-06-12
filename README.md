# Discord Status Updater

A Windows desktop application that lets you set fully custom Discord Rich Presence activity messages — complete with icons, details, state text, elapsed timers, and clickable buttons.

![Discord Status Updater Screenshot](docs/screenshot.png)

## Features

- **Custom Rich Presence** — set Details, State, large/small images, and up to 2 clickable buttons
- **Preset system** — save, load, and delete named presets for quick switching
- **Elapsed time** — toggle an "00:00 elapsed" timer that resets when you update your status
- **Live preview** — see a mock Discord presence card as you type
- **Persistent settings** — Application ID and last-used preset remembered across sessions
- **8 built-in presets** — Coding in VS Code, Visual Studio, Gaming, Music, YouTube, Web Dev, Writing Docs, Studying
- **System tray** — minimize to tray, double-click icon to restore, tray context menu (Open / Update / Clear / Exit)
- **Startup options** — start minimized to tray, open with Windows (registry startup key)
- **Modernized dark UI** — hero banner, blurple accent bars, pill status badge, gradient card borders

## Requirements

| Requirement | Version |
|------------|---------|
| Windows    | 10 or 11 |
| .NET       | 8.0+ (Runtime) — **not needed** if using the self-contained `.exe` |
| Discord    | Desktop app, running and logged in |

## Quick Start

1. **Create a Discord Application** — follow [docs/discord-app-setup.md](docs/discord-app-setup.md)
2. **Copy your Application ID** from the Discord Developer Portal
3. **Download the latest release** from the [Releases page](../../releases)
4. **Run** `DiscordStatusUpdater.exe`
5. **Paste your Application ID** and click **Connect**
6. **Choose a preset** (or fill in the fields manually) and click **Update Status**

## Building from Source

See [docs/building.md](docs/building.md).

```bash
git clone https://github.com/your-username/discord_status_updater.git
cd discord_status_updater
dotnet build src/DiscordStatusUpdater/DiscordStatusUpdater.csproj
dotnet run --project src/DiscordStatusUpdater/DiscordStatusUpdater.csproj
```

## How It Works

Discord Rich Presence uses a local IPC socket that applications on your PC can connect to. This app connects to that socket using your Discord Application ID and sends structured presence data (details, state, images, timestamps) that Discord then displays on your profile.

The images you set (large/small image keys) must be **assets uploaded to your Discord Application** in the Developer Portal. See [docs/discord-app-setup.md](docs/discord-app-setup.md) for how to upload them.

## Project Structure

```
discord_status_updater/
├── src/
│   └── DiscordStatusUpdater/
│       ├── Models/           # ActivityPreset, AppSettings
│       ├── Services/         # DiscordRpcService, SettingsService
│       ├── ViewModels/       # MainViewModel, RelayCommand
│       ├── Converters/       # WPF value converters
│       └── Resources/
│           ├── Themes/       # DarkTheme.xaml
│           └── Presets/      # default_presets.json
├── docs/                     # Setup guides
├── README.md
├── CONTRIBUTING.md
├── TODO.md
└── CHANGELOG.md
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT — see [LICENSE](LICENSE).
