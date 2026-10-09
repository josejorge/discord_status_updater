<!--
File: auto-detect.md
Description: How automatic app detection works and how to configure rules, placeholders and per-tool Discord applications.
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-10-09
Last edit date: 2026-10-09
Version: 1.1.0
-->

# Auto-Detect

Auto-detect watches the apps running on your PC and keeps your Discord status in sync with them: VS Code, Visual Studio, Wave Terminal, JetBrains IDEs and more. Discord itself only recognises games; this covers everything else.

## Turning it on

1. Enter your **Application ID** on the Dashboard (see [discord-app-setup.md](discord-app-setup.md)).
2. Switch on **Auto-detect** (sidebar, or tray menu, *Auto-detect apps*).

The app connects to Discord by itself as soon as it finds a tracked app, and clears your status when none is running (configurable). Pressing **Disconnect** also switches auto-detect off.

## How a winner is chosen

Every few seconds (default 3 s) the app checks which processes your enabled **rules** care about. If several match:

1. With *Prefer the app I am working in* on (default), the app that currently has keyboard focus wins.
2. Otherwise, or if no matching app has focus, the rule highest in the list wins.

A change must be seen on **two consecutive scans** before it is applied, so brief focus changes and short-lived helper windows do not make your status flap. Text-only changes (a different file in the same editor) are sent at most once every 15 seconds because Discord rate-limits updates.

## Rules

A rule has:

| Field | Meaning |
|---|---|
| Name | Display name; available as `{app}` |
| Process names | Comma-separated executable names without `.exe` (`Code, Code - Insiders`) |
| Preset | The preset that supplies the text, images and buttons |
| Title regex (optional) | Only match when the window title matches, e.g. `\.(html\|css)\b` for web files |
| Own Application ID (optional) | See below |

Use **Add from a running app** to create a rule (and a starter preset) from any open window. Rules ship for VS Code, Visual Studio, Cursor, JetBrains IDEs, Android Studio, Wave Terminal, Windows Terminal, Notepad++, Obsidian, Postman, Docker Desktop, GitHub Desktop, SSMS, Blender and Unity. They are stored in `%AppData%\DiscordStatusUpdater\rules.json`.

## Placeholders

Preset text fields (Details, State and both image tooltips) accept:

| Token | Value |
|---|---|
| `{app}` | The rule name, e.g. `Visual Studio Code` |
| `{process}` | The process name, e.g. `Code` |
| `{title}` | The full window title |
| `{file}` | First part of an editor-style title (`main.cs - my-project - Visual Studio Code` gives `main.cs`) |
| `{workspace}` | Project/solution part (`my-project`) |

Text in `[square brackets]` is **dropped entirely when a placeholder inside it is empty**: `[Editing {file}]` shows nothing instead of a dangling "Editing". Unknown tokens become empty. Results shorter than 2 or longer than 128 characters are omitted/truncated (Discord limits).

## Showing the tool's name ("Playing Visual Studio Code")

Discord displays **"Playing &lt;name of the Discord application&gt;"**. With one Application ID every rule shows that same name, and your text goes in Details/State. To get the tool's own name on your profile, create a Discord application named like the tool, upload its icon, and paste its ID into the rule's **Own Application ID**. The app reconnects with the right ID whenever the detected tool changes.

### Shipped applications

Twelve shipped rules already point at ready-made Discord applications (named and iconed like the tool), so they show "Playing Visual Studio Code", "Playing Wave Terminal" and so on out of the box: Visual Studio Code, Visual Studio, Cursor, Android Studio, Wave Terminal, Windows Terminal, Notepad++, Obsidian, Docker Desktop, GitHub Desktop, Blender and Unity. Their presets leave the large image key empty, which makes Discord show the application's own icon. JetBrains IDE, Postman and SSMS use your default Application ID until you create applications for them. Changing a rule's Own Application ID on the Auto-Detect page overrides the shipped one.

## Privacy

Window titles can contain file and project names. Turn off **Show file and project names from window titles** in Settings to publish only the app name. Nothing is sent anywhere except Discord's local IPC socket.

## Known limits

- File names containing ` - ` (space, hyphen, space) are split into extra segments; use `{title}` in such presets.
- Apps with no window title (observed with Wave Terminal) give empty `{file}`/`{workspace}`.
- Only one activity is shown at a time.
