# Contributing to Discord Status Updater

Thanks for your interest in contributing!

## Getting Started

1. **Fork** this repo and clone your fork
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Build the project: `dotnet build src/DiscordStatusUpdater`
4. Make your changes
5. Test manually (see below)
6. Submit a Pull Request

## Development Setup

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) or VS Code with C# extension
- Discord desktop app installed and running

**Build:**
```bash
dotnet build src/DiscordStatusUpdater
```

**Run:**
```bash
dotnet run --project src/DiscordStatusUpdater
```

See [docs/building.md](docs/building.md) for detailed build instructions.

## Code Style

- Follow existing patterns in the codebase (MVVM, `RelayCommand`, `INotifyPropertyChanged`)
- No comments unless the "why" is non-obvious
- `private` fields use `_camelCase`, properties use `PascalCase`
- Keep the ViewModel free of UI dependencies (no `System.Windows.Controls` imports in ViewModels)
- Services should not throw unless it's a programming error — use events for runtime failures

## What to Work On

Check [TODO.md](TODO.md) for planned features. Issues labeled `good first issue` are a great place to start.

## Pull Request Guidelines

- Keep PRs focused on a single change
- Include a clear description of what changed and why
- Test with Discord running before submitting
- Update TODO.md / CHANGELOG.md if appropriate

## Reporting Bugs

Open an issue with:
- Steps to reproduce
- Expected vs actual behavior
- Your Windows version and .NET version
- Whether Discord was running at the time
