# Void.Packer

Asset packing library for VOID Engine with authenticated encryption, compression, integrity verification, streaming reads, and incremental updates.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Packer)](https://www.nuget.org/packages/Void.Packer)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Install

```bash
dotnet add package Void.Packer
```

## What It Does

`Void.Packer` packages game assets into SolidPack archives designed for practical runtime use.

It supports authenticated encryption, compression, integrity checks, streaming reads, and updating existing packs without requiring the rest of VOID Engine.

The pack system is intended to make casual extraction and unauthorized reuse more difficult while maintaining practical runtime access.

> No client-side asset format can make shipped assets impossible for a determined attacker to recover.

## Features

- AES-GCM authenticated encryption
- separate handling for pack metadata and file data
- per-file integrity verification
- adaptive compression
- configurable chunked encryption
- stream-based reads from disk
- incremental pack updates
- multiple-pack output when file limits require splitting
- non-throwing `TryLoadPack` helpers
- concurrent read support

## High-Level API

`Packer` provides helpers for common archive operations:

- `Pack(...)`
- `Unpack(...)`
- `Verify(...)`
- `ListFiles(...)`
- `Update(...)`
- `TryLoadPack(...)`

### Create a pack

```csharp
using Void.Packer;

var files = new[]
{
    new PackFile
    {
        VirtualPath = "Data/example.txt",
        Data = File.ReadAllBytes("example.txt")
    }
};

var result = Packer.Pack(files);

File.WriteAllBytes("GameAssets.pack", result.Packs[0].Data);

if (result.Packs[0].Key is not null)
    File.WriteAllBytes("GameAssets.key", result.Packs[0].Key);
```

### Load a pack without exceptions

```csharp
using Void.Packer;

if (!Packer.TryLoadPack("GameAssets.pack", out var reader, out var error))
{
    Console.WriteLine($"Unable to load pack: {error}");
    return;
}

using (reader)
{
    foreach (var path in reader.ListFiles())
        Console.WriteLine(path);
}
```

When no key is supplied, the reader can detect a matching `.key` file next to the pack.

### Verify pack integrity

```csharp
byte[] data = File.ReadAllBytes("GameAssets.pack");
bool valid = Packer.Verify(data);
```

### Update an existing pack

```csharp
var update = Packer.Update(
    File.ReadAllBytes("GameAssets.pack"),
    new[]
    {
        new PackFile
        {
            VirtualPath = "Data/newfile.txt",
            Data = File.ReadAllBytes("newfile.txt")
        }
    },
    filesToRemove: new[] { "Data/oldfile.txt" }
);
```

## CLI

For command-line workflows, install `Void.Packer.CLI`:

```bash
dotnet tool install --global Void.Packer.CLI
```

Then use:

```bash
void-packer build -c Content/ -o Packs/
void-packer verify --pack Packs/GameAssets.pack
void-packer list --pack Packs/GameAssets.pack --detailed
void-packer extract --pack Packs/GameAssets.pack -o Extracted/
```

See the [VOID Wiki](https://github.com/Shmellyorc/Void/wiki) and the [`Void.Packer.CLI` package](https://www.nuget.org/packages/Void.Packer.CLI) for command-line usage.

## Requirements

- .NET 10

## License

MIT. No royalties. No engine fees.

[GitHub Repository](https://github.com/Shmellyorc/Void)
