<!--
File: presets-guide.md
Description: How to create, edit, share and use presets (including placeholders).
Author: Jose-Jorge HERNANDEZ
Company: Parlee Conseiller, Inc.
Date: 2026-06-01
Last edit date: 2026-10-09
Version: 1.1.0
-->

# Presets Guide

Presets let you save your favourite activity configurations and switch between them instantly.

## Built-in Presets

The app ships with 23 presets (8 general ones plus one per auto-detect rule), compiled into the exe from `Resources/Presets/default_presets.json`. They are loaded the first time you run the app. After that, your presets are stored in:

```
%AppData%\DiscordStatusUpdater\presets.json
```

## Creating a Preset

1. On the **Presets** page click **New** to clear all fields
2. Fill in your activity fields (Details, State, images, buttons)
3. Type a name in the **Preset Name** field
4. Click **Save preset**

## Loading a Preset

Select a preset in the list on the left; the editor updates immediately.

## Editing a Preset

1. Load the preset
2. Change the fields you want
3. Keep the same Preset Name (or rename it)
4. Click **Save preset**; it overwrites the existing preset with that name

## Deleting a Preset

1. Select the preset in the list
2. Click **Delete**

## Placeholders

Details, State and the image tooltips can contain `{app}`, `{process}`, `{title}`, `{file}` and `{workspace}`; text in `[square brackets]` disappears when a placeholder inside it is empty. They are filled in when a preset is used by an auto-detect rule, e.g. `[Editing {file}]` becomes `Editing main.cs`. When you send a preset by hand there is no detected app, so placeholders expand to nothing. See [auto-detect.md](auto-detect.md).

## Sharing Presets

Presets are stored as plain JSON at `%AppData%\DiscordStatusUpdater\presets.json`. You can:
- Back them up by copying the file
- Share the file with other users (they replace their own `presets.json`)
- Edit the JSON directly in any text editor

### Preset JSON format

```json
{
  "Name": "Coding in VS Code",
  "Details": "Editing main.cs",
  "State": "In workspace",
  "LargeImageKey": "vscode",
  "LargeImageText": "Visual Studio Code",
  "SmallImageKey": "csharp",
  "SmallImageText": "C#",
  "ShowElapsedTime": true,
  "Button1Label": "View on GitHub",
  "Button1Url": "https://github.com/your-username/your-repo",
  "Button2Label": "",
  "Button2Url": ""
}
```

## Field Limits (Discord enforces these)

| Field | Max length |
|-------|-----------|
| Details | 128 characters |
| State | 128 characters |
| Large Image Text | 128 characters |
| Small Image Text | 128 characters |
| Button Label | 32 characters |
| Button URL | 512 characters (must start with `https://` or `http://`) |
