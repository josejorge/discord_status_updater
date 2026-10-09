<!--
File: BUGS.md
Description: Authoritative log of gotchas (internal bugs and external surprises) found while building the app.
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-10-09
Last edit date: 2026-10-09
Version: 1.0.0
-->

# Bugs & Gotchas

Format: `[Internal|External] **Title** — Symptom. Root cause. Fix/workaround. File.`

[Internal] **Default presets never loaded** — Symptom: a fresh install showed an empty preset list; the "8 built-in presets" never appeared. Root cause: `default_presets.json` is embedded as a WPF `Resource` (not copied next to the exe) but was read with `File.Exists(BaseDirectory\Resources\Presets\...)`. Fix: read it through `Application.GetResourceStream` with a `pack://application:,,,/Resources/...` URI. File: `src/DiscordStatusUpdater/Services/SettingsService.cs`.

[Internal] **Old RPC client's events corrupt UI state when switching Application IDs** — Symptom: after auto-detect swapped to a rule's own Application ID the status could flip to "Disconnected". Root cause: disposing the previous `DiscordRpcClient` raises `OnClose`, which was forwarded as a real disconnect. Fix: detach `_client` first and ignore events from any client that is no longer current (`ReferenceEquals` check in the handlers). File: `src/DiscordStatusUpdater/Services/DiscordRpcService.cs`.

[External] **`%APPDATA%` environment variable is ignored by `Environment.SpecialFolder.ApplicationData`** — Symptom: a test run started with `APPDATA` pointed at a scratch folder still wrote to the real profile. Root cause: .NET resolves the folder through the shell known-folder API, not the variable. Workaround: `DISCORD_STATUS_UPDATER_DATA` overrides the data directory (also useful for portable installs). File: `src/DiscordStatusUpdater/Services/SettingsService.cs`.

[External] **Discord shows the application's name, not custom text** — Symptom: every auto-detected tool appears as "Playing &lt;same name&gt;". Root cause: Rich Presence takes the activity title from the Discord application behind the Application ID; it cannot be set over IPC. Workaround: one Discord application per tool, set as the rule's *Own Application ID*. File: `docs/auto-detect.md`.

[Internal] **File names containing " - " are split into extra title segments** — Symptom: `My Project - notes.txt - Notepad` yields `{file}` = "My Project". Root cause: editors use " - " both inside names and as the segment separator, so the split is ambiguous. Workaround: use `{title}` in presets for such apps; documented as a known limit. File: `src/DiscordStatusUpdater/Services/TitleParser.cs`.

[External] **Wave Terminal exposes no window title** — Symptom: `{title}`, `{file}` and `{workspace}` are empty for Wave Terminal (observed 2026-10-09). Root cause: none of its `Wave.exe` processes report a `MainWindowTitle`. Workaround: the shipped Wave preset uses fixed text only. File: `src/DiscordStatusUpdater/Resources/Presets/default_presets.json`.
