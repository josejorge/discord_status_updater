# Building Discord Status Updater

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (includes the runtime)
- Windows 10 or 11 (WPF is Windows-only)
- Git

## Build

```bash
git clone https://github.com/your-username/discord_status_updater.git
cd discord_status_updater
dotnet build src/DiscordStatusUpdater/DiscordStatusUpdater.csproj
```

## Run (debug)

```bash
dotnet run --project src/DiscordStatusUpdater/DiscordStatusUpdater.csproj
```

## Publish (self-contained executable)

Creates a single `.exe` with the .NET runtime bundled (no install required on the target machine):

```bash
dotnet publish src/DiscordStatusUpdater/DiscordStatusUpdater.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish
```

The output is at `./publish/DiscordStatusUpdater.exe`.

## Open in Visual Studio

Double-click `DiscordStatusUpdater.sln` — Visual Studio will restore NuGet packages automatically.

## NuGet Packages Used

| Package | Version | Purpose |
|---------|---------|---------|
| `DiscordRichPresence` | 1.2.1.24 | Discord IPC Rich Presence client |
| `Newtonsoft.Json` | 13.0.3 | JSON serialization for settings/presets |

Both are fetched automatically by `dotnet restore` / MSBuild.
