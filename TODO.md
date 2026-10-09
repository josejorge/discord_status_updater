<!--
File: TODO.md
Description: Roadmap and completed items.
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-06-01
Last edit date: 2026-10-09
Version: 1.1.0
-->

# TODO

Items are roughly ordered by priority. Feel free to pick any up!

## v1.1 — Quality of Life

- [ ] **Import / Export presets** — save/load a `.json` file so users can share preset packs
- [ ] **Maximize / restore button** in title bar
- [ ] **Drag-to-resize** visual grip indicator in bottom-right corner
- [ ] **Keyboard shortcuts** — `Ctrl+Enter` to update status, `Ctrl+D` to disconnect

## v1.2 — Advanced Features

- [ ] **Schedule / timer** — auto-switch presets on a time-based schedule (e.g. "switch to Gaming at 7 PM")
- [ ] **Global hotkeys** — update/clear status from anywhere on the desktop
- [ ] **Multiple profiles** — store different Application IDs with separate preset lists
- [ ] **Party info** — expose the party size fields (current / max) for multiplayer game contexts
- [ ] **Custom elapsed start time** — let users set an arbitrary start timestamp

## v1.3 — Polish

- [ ] **Dark / light theme toggle**
- [ ] **Animated connection status** — pulsing dot while connecting
- [ ] **Character counter** on Details, State, and button label fields (Discord limits: 128 chars each)
- [ ] **URL validation** feedback inline on button URL fields
- [ ] **Localization** (i18n) support

## Next ideas (auto-detect)

- [ ] **Validate image keys** against the Discord application's asset list and show real icons in the preview
- [ ] **Language / file-type small image**: pick the small image from the open file's extension
- [ ] **Multiple simultaneous activities** (one connection per Application ID)
- [ ] **Import / export rule packs**

## Done

- [x] Auto-detect running apps with editable rules, focus-aware winner, debounce and rate limiting (2.0.0)
- [x] Preset placeholders (`{file}`, `{workspace}`...) and optional `[sections]` (2.0.0)
- [x] Sidebar UI redesign with live "now detected" card (2.0.0)
- [x] Unit tests for the detection logic (2.0.0)

- [x] Connect to Discord via Application ID
- [x] Set Rich Presence (details, state, large/small images, timestamps, buttons)
- [x] Save / load / delete named presets
- [x] Persist settings (Application ID, last preset) across sessions
- [x] 8 built-in default presets
- [x] Live preview card
- [x] Status bar feedback messages
- [x] Custom Discord dark theme
- [x] WindowChrome (resize + snap) with custom title bar
- [x] System tray support (minimize to tray, tray context menu, balloon tips)
- [x] Auto-start on Windows login (registry startup key, toggled in settings)
- [x] Start minimized to tray option
- [x] App icon (window icon + tray icon)
- [x] Modernized UI (hero banner, accent bars, pill status badge, gradient card borders)
