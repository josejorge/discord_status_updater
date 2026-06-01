# Changelog

All notable changes to this project will be documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.0.0] — 2025-06-01

### Added
- Initial release
- Connect to Discord via a Discord Application ID (uses Discord Rich Presence IPC)
- Set Rich Presence with: Details, State, large image (key + tooltip), small image (key + tooltip), elapsed time, up to 2 clickable buttons
- Preset management — create, save, load, and delete named activity presets
- 8 built-in default presets: VS Code, Visual Studio, Gaming, Music, YouTube, Web Dev, Writing Docs, Studying
- Settings persistence — Application ID and last-used preset saved to `%AppData%\DiscordStatusUpdater\`
- Live preview card showing how the presence will appear in Discord
- Status bar with real-time feedback messages
- Discord-inspired dark UI (WPF, .NET 8, custom WindowChrome title bar)
- MVVM architecture (no third-party MVVM framework required)
