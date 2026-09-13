# Void.Templates

Project templates for creating runnable VOID Engine games on .NET 10.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Templates)](https://www.nuget.org/packages/Void.Templates)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Install

```bash
dotnet new install Void.Templates
```

To update an existing installation:

```bash
dotnet new update
```

## Create a Game

Create a new project in a new folder:

```bash
dotnet new voidgame -n MyGame
cd MyGame
dotnet run
```

Or use the current folder:

```bash
mkdir MyGame
cd MyGame
dotnet new voidgame
dotnet run
```

When no project name is supplied, the template uses the current folder name.

## Custom Options

```bash
dotnet new voidgame \
    -n MyGame \
    --appCompany MyStudio \
    --appTitle "My Game"
```

| Option | Description | Default |
| --- | --- | --- |
| `-n, --name` | Project name | Current folder name |
| `--appCompany` | Company name used for application data folders | `MyCompany` |
| `--appTitle` | Display title of the game window | `My Game` |
| `--TargetFrameworkOverride` | Overrides the target framework | `net10.0` |

## What You Get

The `voidgame` template creates a complete runnable project containing:

- `Program.cs` — application entry point and game settings
- `<ProjectName>Game.cs` — main `Game` subclass
- `Content/` — game asset folder
- a preconfigured `.csproj` referencing `Void.Engine`

The generated game class includes the normal VOID lifecycle methods:

```csharp
protected override void OnEnter() { }
protected override void OnUpdate(FrameTime frameTime) { }
protected override void OnDraw(FrameTime frameTime) { }
protected override void OnExit() { }
```

## VOID 2.0

Current templates target the VOID 2.0 workflow on .NET 10.

VOID 2.0 uses SDL3 for the platform layer, Silk.NET.OpenGL for the built-in renderer, and renderer-neutral public graphics contracts for custom backends.

For migration and renderer details, see:

- [Getting Started](https://github.com/Shmellyorc/Void/wiki/Getting-Started)
- [Migrating to VOID 2.0](https://github.com/Shmellyorc/Void/wiki/Migrating-to-2.0)
- [Custom Renderers](https://github.com/Shmellyorc/Void/wiki/Custom-Renderers)

## Requirements

- .NET 10

Runtime dependencies are restored through `Void.Engine`.

## Documentation

- [VOID Wiki](https://github.com/Shmellyorc/Void/wiki)
- [GitHub Repository](https://github.com/Shmellyorc/Void)
- [Void.Engine on NuGet](https://www.nuget.org/packages/Void.Engine)

## License

MIT. No royalties. No engine fees.
