# Presets Guide

Presets let you save your favourite activity configurations and switch between them instantly.

## Built-in Presets

The app ships with 8 presets in `Resources/Presets/default_presets.json`. These are loaded the first time you run the app. After that, your presets are stored in:

```
%AppData%\DiscordStatusUpdater\presets.json
```

## Creating a Preset

1. Click **New** to clear all fields
2. Fill in your activity fields (Details, State, images, buttons)
3. Type a name in the **Preset Name** field
4. Click **Save**

## Loading a Preset

Select a preset from the **Load Preset** dropdown — the fields update immediately.

## Editing a Preset

1. Load the preset
2. Change the fields you want
3. Keep the same Preset Name (or rename it)
4. Click **Save** — it overwrites the existing preset with that name

## Deleting a Preset

1. Load the preset (or just select it in the dropdown)
2. Click **Delete**

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
