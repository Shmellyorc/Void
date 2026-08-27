<p align="center">
  <img src="Src/Void.Engine/Resources/Data/ImageSmallScaled.png" alt="Void Engine Logo" width="400">
  <br>
  <strong>A lightweight, extensible 2D game framework for .NET.</strong>
</p>

## Philosophy

**Extend, don't modify.**

Most game engines try to do everything. They have physics, networking, UI, animation, and everything else you can think of. The problem is your game is unique. Your physics needs are different. Your UI is different. Yet these engines force you to use their way of doing things. You fight the engine instead of making your game.

At the other extreme, frameworks like MonoGame give you almost nothing. You end up rebuilding things every game needs: saving, audio management, pathfinding. These are solved problems. Why rebuild them?

Void sits in the middle. It gives you the essentials and gets out of your way.

## Extensibility

Void was built to be extended, not just used.

Every major system is built around interfaces and base classes that you can replace, customize, or ignore entirely. The engine doesn't lock you into its solutions. You can swap out any part without fighting the framework.

**What you can extend:**

- **IAsset**: Define new asset types (custom models, compressed data, encrypted files)
- **IMount**: Add custom asset sources (network drives, cloud storage, proprietary archives)
- **IAtlasPacker**: Plug in your own texture packing algorithm
- **ILogSink**: Send logs anywhere (databases, remote servers, custom formats)
- **IBatcher**: Custom rendering logic
- **IRenderTarget**: Custom render surfaces
- **ContentTypeWriterReader<T>**: Any save data type you can imagine

**How it works:**

GameSettings.Instance.SetAtlasPacker(typeof(MyAtlasPacker));

AssetManager.Instance.AddMountToStart(new CloudMount());

AssetManager.Instance.RegisterAssetType<MyAsset>(new[] { ".myext" }, (id, data, tag) => new MyAsset(id, data, tag));

Logger.Instance.AddSink(new DatabaseSink());

No engine code modification. No forking the repo. No fighting the framework. Just clean, simple extension points that work the way you need them to.

## Asset Packer

Void includes a CLI tool and API for packing assets into encrypted, tamper-proof archives. Your assets stay yours.

**Why pack your assets?**

When you release a game, your assets are your intellectual property. Within days of release, someone will extract your art, music, and levels and upload them to asset stores for profit or use them in their own games. Void prevents this.

**CLI Tool**

Build a pack:
```
void-packer build -c Content/ -o Packs/
```

Update a pack (fast incremental updates, seconds not minutes):
```
void-packer update --pack GameAssets.pack --add Content/newfile.png
```

Extract a pack:
```
void-packer extract --pack GameAssets.pack --output Extracted/
```

Verify pack integrity:
```
void-packer verify --pack GameAssets.pack
```

List files in a pack:
```
void-packer list --pack GameAssets.pack --detailed
```
**Security Features**

- AES-GCM 256-bit encryption (same standard used by governments and militaries)
- Separate encryption for header and data sections
- PBKDF2 key derivation with salt
- Per-file CRC32 integrity verification
- Adaptive compression (never makes files larger)
- Fast incremental updates (streaming, not loading into memory)

**How it works**

The pack format uses a bootstrap header that is never encrypted. It contains just enough information to decode the rest of the file: magic bytes, version, flags, header size, data size, file count, and the encryption nonce.

The header contains the file table with virtual paths, offsets, sizes, and CRC32 checksums. This is encrypted separately from the data.

The data section contains your actual assets. Each file is compressed individually and encrypted. Without the key, the pack is just random bytes.

**API Usage**

Load a pack in your game:

AssetManager.Instance.LoadPack("GameAssets.pack", "GameAssets.key");
AssetManager.Instance.AddMountToStart(pack);

Then load assets normally:

var texture = AssetManager.Instance.Load<Texture>("player.png");

Your code doesn't change whether assets are loose or packed. The engine handles everything.

**Key Management**

The key is stored separately from the pack. You decide how to distribute it:

- Embed in the game executable
- Download from a CDN
- Store on a secure server
- Distribute with the game

## Features

| System | What It Does |
|--------|--------------|
| Rendering | Batched sprite/primitive rendering, texture atlasing, shaders |
| Assets | Mount-based virtual file system, encrypted pack loading, LRU eviction |
| Input | Keyboard, mouse, gamepad (SDL mapping), action system |
| Audio | Sound pooling, priority-based voice stealing, category volumes |
| Saving | AES-GCM encrypted saves with manifest verification |
| Pathfinding | A*, Dijkstra, BFS, flow fields |
| Coroutines | Tweens, sequencing, delays, 33 easing functions |
| Logging | Async logging with console and file sinks |
| Math | Vectors, rectangles, colors, easing, random |


## Getting Started

## Getting Started

Create a new console project:
```
dotnet new console -n MyGame
cd MyGame
```

Add Void.Engine:
```
dotnet add package Void.Engine
```

Create a new class called MyGame.cs:
```
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

Replace Program.cs with:
```
using Void.Engine;

var settings = GameSettings.Instance
    .SetAppCompany("MyStudio")
    .SetAppName("MyGame")
    .SetWindow(1280, 720)
    .Build();

using var game = new MyGame(settings);
game.Run();
```

Run your game:
```
dotnet run
```

## Demos

- **FlappyBirb**: A Flappy Bird clone
- **Scavengers**: A rogue-lite zombie survival clone

## Supported Platforms

| Platform | Status |
|----------|--------|
| Windows | Full support |
| macOS | Full support |
| Linux | Full support |

## Requirements

- .NET 10
- SFML.Net 3.0

## License

MIT. Use it for anything. No royalties. No fees.

**Void Engine — Made by developers who care about your work.**