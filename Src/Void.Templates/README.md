# Void.Templates
Project templates for Void Engine games.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Templates)](https://www.nuget.org/packages/Void.Templates)


## Install
```bash
dotnet new install Void.Templates
```


## Usage

### Create a new game in a new folder
```bash
dotnet new voidgame -n MyGame
```


### Create a new game in the current folder
```bash
mkdir MyGame
cd MyGame
dotnet new voidgame
```
The template will use the current folder name as the project name.


### With custom company and title
```bash
dotnet new voidgame -n MyGame --appCompany MyStudio --appTitle "My Game"
```


### Available Options
| Option | Description | Default |
|--------|-------------|---------|
| `-n, --name` | The project name | Current folder name |
| `--appCompany` | The company name (used for AppData folders) | `MyCompany` |
| `--appTitle` | The display title of the game window | `My Game` |
| `--TargetFrameworkOverride` | Overrides the target framework | `net10.0` |


## What You Get
The template generates a complete, runnable game project with:

- `Program.cs` — Entry point with game settings
- `MyGameGame.cs` — Main game class with `OnEnter`, `OnUpdate`, `OnDraw`, `OnExit`
- `Content/` — Folder for your assets
- Pre-configured `.csproj` with `Void.Engine` reference


## Requirements
- .NET 10


## License

MIT. Use it for anything. No royalties. No fees.


**Full documentation: https://github.com/Shmellyorc/Void/wiki**