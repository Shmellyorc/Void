# VOID Engine

A lightweight, modular, extensible 2D game framework for .NET.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Engine)](https://www.nuget.org/packages/Void.Engine)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## What is VOID?

**VOID Engine** provides the systems most 2D games need without trying to become a giant all-in-one engine.

VOID is built around a simple idea:

> **Give developers solid defaults without assuming those defaults are right for every game.**

Use the built-in systems as they are, extend them, replace them, or ignore the ones you do not need.

VOID deliberately stays focused on framework-level systems. Features such as full physics and full UI frameworks are left to your game or the libraries you choose.

## Install

```bash
dotnet add package Void.Engine
```

Or install the project template:

```bash
dotnet new install Void.Templates
```

Then create and run a game:

```bash
dotnet new voidgame -n MyGame
cd MyGame
dotnet run
```

## Features

| System | What It Does |
| --- | --- |
| **Rendering** | Batched sprite and primitive rendering, texture atlasing, shaders, render targets, post-processing |
| **Renderer API** | Public renderer-neutral contracts with a pluggable backend architecture |
| **Platform** | SDL3 windowing, displays, fullscreen modes, events, keyboard, mouse, and gamepads |
| **Assets** | Mount-based virtual file system, custom asset types, pack loading, LRU eviction |
| **Audio** | OpenAL playback, sound pooling, priority-based voice stealing, category volumes |
| **Saving** | AES-GCM encrypted saves with manifest verification |
| **Pathfinding** | A*, Dijkstra, BFS, and flow fields |
| **Coroutines** | Tweens, sequencing, delays, waits, and easing |
| **Logging** | Async logging with console and file sinks |
| **Math** | Vectors, matrices, rectangles, colors, easing, and random helpers |
| **Tooling** | Project templates and authenticated encrypted asset packing tools |
| **LDtk** | LDtk level and asset integration |

## Philosophy

### Extend, don't modify.

Large engines often try to solve every possible problem.

That can be useful, but it can also leave developers working around systems that do not fit their game.

At the other extreme, very low-level frameworks provide freedom but leave you rebuilding common infrastructure yourself.

**VOID sits in the middle.**

It provides useful defaults while exposing the places where different games may reasonably need different solutions.

The built-in implementation is not assumed to be the only implementation.

**No engine fork required. No fighting hidden internals. No one-size-fits-all workflow.**

VOID also aims to keep the normal path simple. Advanced systems should exist underneath the framework without making basic tasks complicated.

Performance-sensitive code is written with allocation behavior, thread safety, and hot-path cost in mind.

## VOID 2.0 Rendering Architecture

VOID 2.0 no longer depends on SFML.

The built-in renderer uses **Silk.NET.OpenGL**, while **SDL3-CS** handles the platform layer and **Silk.NET.OpenAL** handles audio.

The OpenGL renderer is a default implementation, not the definition of VOID's rendering system.

Custom renderer backends can be selected through `GameSettings`:

```csharp
var settings = GameSettings.Instance
    .SetRenderer(() => new MyRenderer())
    .Build();
```

Renderer plugins can implement VOID's public graphics contracts for APIs such as:

- Vulkan
- Direct3D
- Metal
- OpenGL
- custom renderers

VOID exposes native window handles through `IRendererContext` when a backend requires them.

Higher-level game and engine code remains renderer-neutral.

[Read the Custom Renderer documentation](https://github.com/Shmellyorc/Void/wiki/Custom-Renderers)

## Extensibility

VOID provides extension points where alternate implementations make sense.

| Extension Point | Purpose |
| --- | --- |
| `IAsset` | Define custom asset types |
| `IMount` | Add custom asset sources |
| `IAtlasPacker` | Replace the texture packing algorithm |
| `ILogSink` | Add custom logging destinations |
| `IRendererBackend` | Provide another graphics backend |
| `IGraphicsDevice` | Implement renderer-specific GPU behavior |
| `IBatcher` | Add custom batching strategies |
| `IRenderTarget` | Provide custom render surfaces |
| `BaseCamera` | Build specialized camera behavior |
| `ContentTypeWriterReader<T>` | Support custom save-data types |

Examples:

```csharp
GameSettings.Instance.SetAtlasPacker(typeof(MyAtlasPacker));

AssetManager.Instance.AddMountToStart(new CloudMount());

AssetManager.Instance.RegisterAssetType<MyAsset>(
    new[] { ".myext" },
    (id, data, tag) => new MyAsset(id, data, tag)
);

Logger.Instance.AddSink(new DatabaseSink());
```

The defaults are there when you want them.

The extension points are there when you do not.

## Asset Packer

VOID includes an API and command-line tool for packaging assets into authenticated, encrypted archives.

Features include:

- AES-GCM authenticated encryption
- adaptive compression
- per-file integrity verification
- configurable chunked encryption
- streaming reads
- incremental updates
- concurrent asset loading support

Install the CLI:

```bash
dotnet tool install --global Void.Packer.CLI
```

Build a pack:

```bash
void-packer build -c Content/ -o Packs/
```

Verify it:

```bash
void-packer verify --pack GameAssets.pack
```

The pack system is intended to make casual extraction and unauthorized reuse more difficult while maintaining practical runtime access.

> No client-side asset format can make shipped assets impossible for a determined attacker to recover.

## Quick Start Without the Template

Create a normal console project and add VOID:

```bash
dotnet new console -n MyGame
cd MyGame
dotnet add package Void.Engine
```

Create a game class:

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

Configure and run it:

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

## Demos

### FlappyBirb

A small Flappy Bird-style example demonstrating the basic VOID workflow.

### Scavengers

A larger rogue-lite zombie survival example demonstrating more of the framework working together.

## Supported Platforms

| Platform | Status |
| --- | --- |
| Windows | Supported |
| macOS | Supported |
| Linux | Supported |

VOID uses SDL3 for its platform layer. The built-in graphics backend uses OpenGL.

## Requirements

- .NET 10

Runtime graphics, platform, input, and audio dependencies are provided through the engine package.

## Documentation

- [VOID Wiki](https://github.com/Shmellyorc/Void/wiki)
- [Getting Started](https://github.com/Shmellyorc/Void/wiki/Getting-Started)
- [Migrating to VOID 2.0](https://github.com/Shmellyorc/Void/wiki/Migrating-to-2.0)
- [Custom Renderers](https://github.com/Shmellyorc/Void/wiki/Custom-Renderers)
- [GitHub Repository](https://github.com/Shmellyorc/Void)

## License

MIT.

Use VOID for personal, commercial, open-source, or closed-source projects.

**No royalties. No engine fees.**

---

**VOID Engine: the foundation is yours. Build the rest your way.**
