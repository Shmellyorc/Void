<p align="center">
  <img src="Images/ImageSmallScaled.png" alt="Void Engine Logo" width="400">
  <br>
  <strong>A modular, extensible 2D game framework for .NET.</strong>
</p>

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## NuGet Packages

| Package | Version | NuGet |
|---|---:|---|
| [Void.Engine](https://www.nuget.org/packages/Void.Engine) | 1.2.1 | [![NuGet](https://img.shields.io/nuget/v/Void.Engine)](https://www.nuget.org/packages/Void.Engine) |
| [Void.Packer](https://www.nuget.org/packages/Void.Packer) | 1.0.0 | [![NuGet](https://img.shields.io/nuget/v/Void.Packer)](https://www.nuget.org/packages/Void.Packer) |
| [Void.Packer.CLI](https://www.nuget.org/packages/Void.Packer.CLI) | 1.0.0 | [![NuGet](https://img.shields.io/nuget/v/Void.Packer.CLI)](https://www.nuget.org/packages/Void.Packer.CLI) |
| [Void.Templates](https://www.nuget.org/packages/Void.Templates) | 1.0.0 | [![NuGet](https://img.shields.io/nuget/v/Void.Templates)](https://www.nuget.org/packages/Void.Templates) |

## What is VOID?

**VOID Engine** is a modular, extensible 2D game framework for .NET.

It provides the systems most games need without forcing you into a giant all-in-one engine: rendering, assets, input, audio, saving, pathfinding, coroutines, logging, window and display management, and more.

VOID is built around a simple idea:

> **Give developers a solid foundation, expose the important pieces, and stay out of the way.**

You can use the built-in systems as they are, replace them, extend them, or ignore the ones you do not need.

## Quick Install

### Engine

```bash
dotnet add package Void.Engine
```

### CLI Tool

```bash
dotnet tool install --global Void.Packer.CLI
```

### Project Template

```bash
dotnet new install Void.Templates
```

Create a game:

```bash
dotnet new voidgame -n MyGame
cd MyGame
dotnet run
```

## Features

| System | What It Does |
|---|---|
| Rendering | Batched sprite and primitive rendering, texture atlasing, shaders, render targets, post-processing |
| Renderer API | Pluggable renderer architecture with public graphics contracts for custom backends |
| Platform | SDL3-powered windowing, displays, fullscreen modes, events, keyboard, mouse, and gamepads |
| Assets | Mount-based virtual file system, custom asset types, encrypted pack loading, LRU eviction |
| Audio | OpenAL playback, sound pooling, priority-based voice stealing, category volumes |
| Saving | AES-GCM encrypted saves with manifest verification |
| Pathfinding | A*, Dijkstra, BFS, flow fields |
| Coroutines | Tweens, sequencing, delays, waits, and easing |
| Logging | Async logging with console and file sinks |
| Math | Vectors, rectangles, colors, easing, random helpers |
| Tooling | Project templates, asset packing CLI, pack verification, listing, extraction, and updates |

## Philosophy

### Extend, don't modify.

Large engines often try to solve every possible problem. That can be useful, but it can also mean working around systems that do not fit your game.

At the other extreme, low-level frameworks give you complete freedom but leave you rebuilding the same infrastructure over and over.

VOID sits in the middle.

It gives you useful, production-minded building blocks while keeping the architecture open enough to replace the pieces that do not fit.

**No engine fork required. No fighting hidden internals. No one-size-fits-all workflow.**

- [VOID Wiki](https://github.com/Shmellyorc/Void/wiki)
- [Full VOID spec sheet and technical details](VoidSpecSheet.pdf)

## Rendering Architecture

VOID no longer depends on SFML.

The built-in renderer uses **Silk.NET.OpenGL**, while **SDL3-CS** owns the platform layer:

- native window creation and destruction
- keyboard, mouse, and gamepad input
- display and multi-monitor support
- fullscreen and window modes
- focus and window events
- OpenGL context creation
- swap buffers and swap interval control

Audio is handled through **Silk.NET.OpenAL**.

SDL remains an internal implementation detail for normal engine users. Renderer plugins work through VOID's public renderer contracts instead of depending directly on the engine's SDL implementation.

### Pluggable Renderers

The built-in OpenGL backend is only one renderer implementation.

Custom renderers can be selected through `GameSettings`:

```csharp
var settings = GameSettings.Instance
    .SetRenderer(() => new MyRenderer())
    .Build();
```

A renderer can implement VOID's public graphics contracts and provide its own backend for APIs such as:

- Vulkan
- Direct3D
- Metal
- another OpenGL implementation
- a custom renderer

The renderer context also exposes safe access to platform-native handles when a backend needs them, without requiring the renderer to depend on VOID's internal SDL classes.

This keeps game code and the higher-level engine systems renderer-neutral.

## Extensibility

VOID was built to be extended, not just used.

Major systems expose interfaces, abstractions, and extension points so you can replace or customize behavior without modifying the engine source.

### Examples

- **`IAsset`** — define custom asset types
- **`IMount`** — add custom asset sources
- **`IAtlasPacker`** — plug in a different texture packing algorithm
- **`ILogSink`** — send logs to your own destination or format
- **`IRendererBackend`** — provide a completely different graphics backend
- **`IGraphicsDevice`** — implement renderer-specific GPU resource behavior
- **`IBatcher`** — provide custom batching/rendering logic
- **`IRenderTarget`** — implement custom render surfaces
- **`ContentTypeWriterReader<T>`** — support your own save-data types

Example:

```csharp
GameSettings.Instance.SetAtlasPacker(typeof(MyAtlasPacker));

AssetManager.Instance.AddMountToStart(new CloudMount());

AssetManager.Instance.RegisterAssetType<MyAsset>(
    new[] { ".myext" },
    (id, data, tag) => new MyAsset(id, data, tag)
);

Logger.Instance.AddSink(new DatabaseSink());
```

No engine fork. No source modification. Just extension points.

## Asset Packer

VOID includes both an API and a CLI for packaging game assets into authenticated, encrypted archives.

The pack system is designed to make casual extraction and unauthorized reuse more difficult while also providing integrity checking, compression, streaming access, and fast incremental updates.

> Encryption can protect packaged data at rest, but no client-side game asset format can make shipped assets impossible to recover by a determined attacker. VOID's pack system is designed to raise that barrier while keeping the runtime practical.

### CLI

Build a pack:

```bash
void-packer build -c Content/ -o Packs/
```

Build with a custom chunk size:

```bash
void-packer build -c Content/ -o Packs/ --chunk-size 512
```

Disable chunking and use solid encryption:

```bash
void-packer build -c Content/ -o Packs/ --chunk-size 0
```

Extract a pack:

```bash
void-packer extract --pack GameAssets.pack --output Extracted/
```

Verify pack integrity:

```bash
void-packer verify --pack GameAssets.pack
```

List files:

```bash
void-packer list --pack GameAssets.pack --detailed
```

Incrementally update a pack:

```bash
void-packer update --pack GameAssets.pack --add Content/newfile.png --remove oldfile.txt
```

### Pack Features

- AES-GCM 256-bit authenticated encryption
- separate encrypted header and data sections
- per-file CRC32 verification
- adaptive compression that avoids expanding incompressible files
- streaming incremental updates
- configurable chunked encryption
- per-chunk authentication
- stream-based reads without loading the full pack into memory
- lazy open and idle close behavior
- thread-safe concurrent asset loading

### How It Works

The pack begins with a small bootstrap header containing the information required to locate and decode the encrypted portions of the file.

The encrypted header stores the file table, including virtual paths, offsets, sizes, and CRC32 values.

The data section stores the asset payloads. When chunking is enabled, large packs are split into independently authenticated encrypted chunks. Reading a file only requires the relevant data instead of decrypting an entire pack at once.

Authentication failures are detected if encrypted data is modified.

### API Usage

Load a pack:

```csharp
var pack = AssetManager.Instance.LoadPack("GameAssets.pack");
AssetManager.Instance.AddMountToStart(pack);
```

The key can be loaded from `GameAssets.key` next to the pack.

Load multiple packs with priority control:

```csharp
var graphicsPack = AssetManager.Instance.LoadPack("Graphics.pack");
var audioPack = AssetManager.Instance.LoadPack("Audio.pack");
var levelsPack = AssetManager.Instance.LoadPack("Levels.pack");

AssetManager.Instance.AddMountToStart(graphicsPack);
AssetManager.Instance.AddMountToStart(audioPack);
AssetManager.Instance.AddMountToStart(levelsPack);
```

Load all indexed packs in a directory:

```csharp
var packs = AssetManager.Instance.LoadAllPacks("Packs/");

foreach (var pack in packs)
{
    AssetManager.Instance.AddMountToEnd(pack);
}
```

Graceful error handling:

```csharp
if (!Packer.TryLoadPack("Mod.pack", out var reader, out var error))
{
    Console.WriteLine($"Failed to load mod: {error}");
    return;
}

using (reader)
{
    var pack = new PackMount(reader);
    AssetManager.Instance.AddMountToStart(pack);
}
```

Game code does not need to care whether assets came from loose files or mounted packs.

### Key Management

The encryption key is stored separately from the pack. How you distribute it is up to your game:

- embed it in the executable
- download it from a service
- store it on your own server
- distribute it alongside the game

Choose the approach that fits your threat model and deployment requirements.

## Getting Started

### Using the Project Template

Install the template:

```bash
dotnet new install Void.Templates
```

Create a new game:

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
```

### Customizing Your Game

```bash
dotnet new voidgame -n MyGame --appCompany MyStudio --appTitle "My Game"
```

| Option | Description | Default |
|---|---|---|
| `-n, --name` | Project name | Current folder name |
| `--appCompany` | Company name used for application data paths | `MyCompany` |
| `--appTitle` | Display title of the game window | `My Game` |
| `--TargetFrameworkOverride` | Overrides the target framework | `net10.0` |

### What You Get

The template generates a complete runnable project with:

- `Program.cs` — entry point and game settings
- `MyGameGame.cs` — main game class with `OnEnter`, `OnUpdate`, `OnDraw`, and `OnExit`
- `Content/` — asset directory
- a preconfigured `.csproj` referencing `Void.Engine`

## Manual Setup

```bash
dotnet new console -n MyGame
cd MyGame
dotnet add package Void.Engine
```

Create `MyGame.cs`:

```csharp
using Void.Engine;

public class MyGame : Game
{
    public MyGame(GameSettings settings) : base(settings) { }

    protected override void OnEnter() { }

    protected override void OnUpdate(FrameTime frameTime) { }

    protected override void OnDraw(FrameTime frameTime) { }

    protected override void OnExit() { }
}
```

Replace `Program.cs`:

```csharp
using Void.Engine;

var settings = GameSettings.Instance
    .SetAppCompany("MyStudio")
    .SetAppName("MyGame")
    .SetWindow(1280, 720)
    .Build();

using var game = new MyGame(settings);
game.Run();
```

Run:

```bash
dotnet run
```

## Demos

- **FlappyBirb** — Flappy Bird-style example
- **Scavengers** — rogue-lite zombie survival example

## Supported Platforms

| Platform | Status |
|---|---|
| Windows | Supported |
| macOS | Supported |
| Linux | Supported |

VOID uses SDL3 for the platform layer, with the built-in renderer using OpenGL.

## Requirements

- .NET 10

Runtime graphics, platform, input, and audio dependencies are provided through the engine package.

## License

MIT.

Use VOID for personal, commercial, open-source, or closed-source projects.

**No royalties. No engine fees.**

---

<p align="center">
  <strong>VOID Engine: the foundation is yours. Build the rest your way.</strong>
</p>
