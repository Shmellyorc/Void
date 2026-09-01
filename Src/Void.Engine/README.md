# Void Engine

A lightweight, extensible 2D game framework for .NET.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Engine)](https://www.nuget.org/packages/Void.Engine)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

---

## Philosophy

**Extend, don't modify.**

Most game engines try to do everything. They have physics, networking, UI, animation, and everything else you can think of. The problem is your game is unique. Your physics needs are different. Your UI is different. Yet these engines force you to use their way of doing things. You fight the engine instead of making your game.

At the other extreme, frameworks like MonoGame give you almost nothing. You end up rebuilding things every game needs: saving, audio management, pathfinding. These are solved problems. Why rebuild them?

**Void sits in the middle.** It gives you the essentials and gets out of your way.

---

## Features

| System | What It Does |
|--------|--------------|
| **Rendering** | Batched sprite and primitive rendering, texture atlasing, shaders, post-processing |
| **Assets** | Mount-based virtual file system, encrypted pack loading, LRU eviction |
| **Input** | Keyboard, mouse, gamepad with SDL mapping, action system |
| **Audio** | Sound pooling, priority-based voice stealing, category volumes |
| **Saving** | AES-GCM encrypted saves with manifest verification |
| **Pathfinding** | A*, Dijkstra, BFS, flow fields |
| **Coroutines** | Tweens, sequencing, delays, 33 easing functions |
| **Logging** | Async logging with console and file sinks |
| **Math** | Vectors, rectangles, colors, easing, random |
| **LDtk** | Full level editor support with entities, tilesets, and custom fields |
| **Modding** | Mount-based virtual file system for mod support |

---

## Quick Install

```bash
dotnet add package Void.Engine
```

---

## Quick Start

### 1. Create a new console project

```bash
dotnet new console -n MyGame
cd MyGame
```

### 2. Add Void.Engine

```bash
dotnet add package Void.Engine
```

### 3. Create your game class

```csharp
using Void.Engine;

public class MyGame : Game
{
    public MyGame(GameSettings settings) : base(settings) { }

    protected override void OnEnter()
    {
        // Called when the game starts
    }

    protected override void OnUpdate(FrameTime frameTime)
    {
        // Called every frame
    }

    protected override void OnDraw(FrameTime frameTime)
    {
        // Called every frame after OnUpdate
    }

    protected override void OnExit()
    {
        // Called when the game exits
    }
}
```

### 4. Run your game

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

---

## Demos

- **FlappyBirb**: A Flappy Bird clone
- **Scavengers**: A rogue-lite zombie survival clone

---

## Asset Packer

Void includes a CLI tool and API for packing assets into encrypted, tamper-proof archives.

### CLI Tool

```bash
# Build a pack
void-packer build -c Content/ -o Packs/

# Extract a pack
void-packer extract --pack GameAssets.pack --output Extracted/

# Verify pack integrity
void-packer verify --pack GameAssets.pack

# List files in a pack
void-packer list --pack GameAssets.pack --detailed

# Update a pack (fast incremental updates)
void-packer update --pack GameAssets.pack --add Content/newfile.png --remove oldfile.txt
```

### Security Features

- AES-GCM 256-bit encryption
- Separate encryption for header and data sections
- Per-file CRC32 integrity verification
- Adaptive compression
- Chunked encryption with per-chunk authentication
- Stream-based reading from disk (no full pack loaded into memory)
- Thread-safe for concurrent asset loading

### API Usage

```csharp
// Load a pack in your game
var pack = AssetManager.Instance.LoadPack("GameAssets.pack");
AssetManager.Instance.AddMountToStart(pack);

// Load multiple packs with priority control
var graphicsPack = AssetManager.Instance.LoadPack("Graphics.pack");
var audioPack = AssetManager.Instance.LoadPack("Audio.pack");
AssetManager.Instance.AddMountToStart(graphicsPack);
AssetManager.Instance.AddMountToStart(audioPack);

// Graceful error handling
if (!Packer.TryLoadPack("Mod.pack", out var reader, out var error))
{
    Console.WriteLine($"Failed to load mod: {error}");
    return;
}
```

---

## Extensibility

Void was built to be extended, not just used.

Every major system is built around interfaces and base classes that you can replace, customize, or ignore entirely.

**What you can extend:**

| Interface | Purpose |
|-----------|---------|
| `IAsset` | Define new asset types |
| `IMount` | Add custom asset sources |
| `IAtlasPacker` | Plug in your own texture packing algorithm |
| `ILogSink` | Send logs anywhere |
| `IBatcher` | Custom rendering logic |
| `IRenderTarget` | Custom render surfaces |
| `ContentTypeWriterReader` | Any save data type |

**How it works:**

```csharp
GameSettings.Instance.SetAtlasPacker(typeof(MyAtlasPacker));
AssetManager.Instance.AddMountToStart(new CloudMount());
Logger.Instance.AddSink(new DatabaseSink());
```

No engine code modification. No forking the repo. No fighting the framework.

---

## Supported Platforms

| Platform | Status |
|----------|--------|
| Windows | ✅ Full support |
| macOS | ✅ Full support |
| Linux | ✅ Full support |

---

## Requirements

- .NET 10
- SFML.Net 3.0

---

## License

MIT. Use it for anything. No royalties. No fees.

---

**Void Engine: Made by developers who care about your work.**
