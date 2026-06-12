# Changelog

All notable changes to this project will be documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.1.0] — 2026-06-12

### Added
- System tray icon — app hides to tray instead of closing; double-click to restore
- Tray context menu — Open, Update Status, Clear Status, Exit (Discord dark theme styling)
- **Minimize to Tray** setting — toggled in the new App Settings section
- **Start Minimized** setting — launches directly to the system tray on startup
- **Open with Windows** setting — writes/removes a registry startup key (`HKCU\...\Run`)
- App icon embedded as a WPF resource (used as window icon and tray icon)
- Modernized UI — hero banner with gradient, blurple accent bars on section headers, pill-shaped status badge, gradient card borders

### Changed
- Window close button now hides to tray (when Minimize to Tray is on) instead of exiting; use the tray Exit entry or the force-close path to quit

---

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
