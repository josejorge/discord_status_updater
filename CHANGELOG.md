<!--
File: CHANGELOG.md
Description: Release history of Discord Status Updater (Keep a Changelog format).
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-06-01
Last edit date: 2026-10-09
Version: 1.1.0
-->

# Changelog

All notable changes to this project will be documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.0.0] — 2026-10-09

### Added
- **Auto-detect**: watches running apps and updates the Discord status on its own; switches when you change app, updates when you open a different file, and clears the status when nothing tracked is running (see [docs/auto-detect.md](docs/auto-detect.md))
- **Detection rules**: 15 shipped (VS Code, Visual Studio, Cursor, JetBrains, Android Studio, Wave Terminal, Windows Terminal, Notepad++, Obsidian, Postman, Docker Desktop, GitHub Desktop, SSMS, Blender, Unity); editable, reorderable, with optional title regex and per-rule Discord Application ID; "Add from a running app" helper
- **Placeholders** in presets: `{app} {process} {title} {file} {workspace}` and `[optional sections]`
- Focus-aware winner selection, 2-scan debounce and 15 s update throttle
- Settings: poll interval, prefer focused app, clear when idle, hide file/project names
- Tray menu entry "Auto-detect apps"
- Unit tests (`tests/DiscordStatusUpdater.Tests`, 35 tests) and a CI test step
- `DISCORD_STATUS_UPDATER_DATA` environment variable to relocate the settings folder

### Changed
- **Redesigned UI**: sidebar navigation (Dashboard, Auto-Detect, Presets, Settings), live "Now detected" card, preset list + editor with live preview, toggle switches; window is now wider (1020x720)
- Version shown in the UI is read from the assembly

### Fixed
- Built-in presets were never loaded on a fresh install (embedded resource was read as a file)
- Switching Discord Application IDs no longer shows a spurious "Disconnected" state

---

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
