# Discord Application Setup

This guide walks you through creating a Discord Application (needed to get an Application ID and upload custom images).

## 1 — Create the Application

1. Go to [discord.com/developers/applications](https://discord.com/developers/applications) and log in
2. Click **New Application** (top-right)
3. Give it a name — this name appears as the activity title in Discord (e.g. "Coding in VS Code")
4. Click **Create**

## 2 — Copy Your Application ID

1. In your new application, open the **General Information** tab
2. Copy the **Application ID** (also called Client ID) — it looks like `1234567890123456789`
3. Paste it into the **Application ID** field in Discord Status Updater

> **Note:** Never share your application's Client Secret (a different field). The Application ID itself is safe to share.

## 3 — Upload Rich Presence Images

Images in Discord Rich Presence are referenced by a short key name (e.g. `vscode`, `csharp`). You upload the actual image files here.

1. In your application, go to **Rich Presence → Art Assets**
2. Click **Add Image(s)**
3. Upload a PNG or JPG (recommended size: **512×512** or larger, square)
4. Give it a **key name** — this is what you type in the "Asset key" fields in Discord Status Updater
5. Click **Save Changes**

### Recommended image keys

These match the built-in presets:

| Key name       | Suggested image         |
|----------------|-------------------------|
| `vscode`       | VS Code icon            |
| `visualstudio` | Visual Studio icon      |
| `csharp`       | C# logo                 |
| `game`         | Any game icon           |
| `playing`      | Small play-button icon  |
| `music`        | Music note or headphones|
| `youtube`      | YouTube logo            |
| `webdev`       | Browser or code icon    |
| `html`         | HTML5 logo              |
| `markdown`     | Markdown logo or pen    |
| `study`        | Book or graduation cap  |

You can use any key name — just make sure what you type in the app matches exactly what you uploaded (case-sensitive).

## 4 — Test It

1. Make sure Discord desktop is running and you are logged in
2. Open Discord Status Updater
3. Paste your Application ID and click **Connect** (status dot should turn green)
4. Fill in the fields and click **Update Status**
5. Open Discord — click your own profile, you should see the custom activity

## Troubleshooting

| Symptom | Cause / Fix |
|---------|-------------|
| Status dot stays yellow | Discord is not running, or the Application ID is wrong |
| Images don't appear | The image key doesn't match the asset name you uploaded; wait a minute after uploading |
| Activity not showing | Your Discord privacy settings may hide activity status — check User Settings → Activity Privacy |
| "Not connected" after Connect | Restart Discord and try again; occasionally Discord's IPC socket needs a refresh |
