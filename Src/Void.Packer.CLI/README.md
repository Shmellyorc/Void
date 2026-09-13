# Void.Packer.CLI

Command-line tool for building, verifying, inspecting, extracting, and updating VOID asset packs.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Packer.CLI)](https://www.nuget.org/packages/Void.Packer.CLI)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

## Install

```bash
dotnet tool install --global Void.Packer.CLI
```

The installed command is:

```bash
void-packer
```

To update an existing installation:

```bash
dotnet tool update --global Void.Packer.CLI
```

## Commands

### Build

Build one or more packs from a content directory:

```bash
void-packer build -c Content/ -o Packs/
```

Common options:

| Option | Short | Description | Default |
| --- | --- | --- | --- |
| `--content` | `-c` | Content directory to pack | Required |
| `--output` | `-o` | Output directory for `.pack` and `.key` files | Required |
| `--name` | `-n` | Base output name | `GameAssets` |
| `--include` | `-i` | Comma-separated include patterns | All matched content |
| `--exclude` | `-e` | Comma-separated exclude patterns | None |
| `--encrypt` |  | Enable encryption | `true` |
| `--compress` |  | `None`, `Deflate`, or `Brotli` | `Deflate` |
| `--adaptive` |  | Use adaptive compression | `true` |
| `--max-files` |  | Maximum files per pack | `65535` |
| `--compression-level` |  | Compression level from 1-9 | `6` |
| `--case-sensitive` |  | Use case-sensitive virtual paths | `false` |
| `--chunk-size` |  | Encryption chunk size in KB; `0` uses solid encryption | `1024` |
| `--verbose` | `-v` | Verbose output | `false` |
| `--no-wait` |  | Do not wait for a key press after completion | `false` |
| `--no-color` |  | Disable colored output | `false` |

Examples:

```bash
# Build with exclusions
void-packer build -c Content/ -o Packs/ -e "**/*.ase*"

# Build with a custom pack name
void-packer build -c Content/ -o Packs/ -n MyGameAssets

# Build with a custom chunk size
void-packer build -c Content/ -o Packs/ --chunk-size 512
```

### Verify

Verify pack integrity:

```bash
void-packer verify --pack Packs/GameAssets.pack
```

Use `--key` to provide a key file explicitly when needed.

### List

List files in a pack:

```bash
void-packer list --pack Packs/GameAssets.pack
```

Show size, compression, and CRC details:

```bash
void-packer list --pack Packs/GameAssets.pack --detailed
```

### Extract

Extract all files from a pack:

```bash
void-packer extract --pack Packs/GameAssets.pack -o Extracted/
```

Use `--key` to provide a key file explicitly when needed.

### Update

Update an existing pack:

```bash
void-packer update \
    --pack Packs/GameAssets.pack \
    --add Content/newfile.png \
    --remove old/texture.png
```

By default the existing pack is overwritten. Use `-o` / `--output` to write the updated pack elsewhere.

## Encryption and Integrity

The CLI uses `Void.Packer` and supports:

- AES-GCM authenticated encryption
- adaptive compression
- per-file integrity verification
- configurable chunked encryption
- streaming reads
- incremental updates

The pack system is intended to make casual extraction and unauthorized reuse more difficult while maintaining practical runtime access.

> No client-side asset format can make shipped assets impossible for a determined attacker to recover.

## Automation

For build scripts and CI, `--no-wait` prevents the tool from waiting for input after completion.

Use `--no-color` when plain terminal output is preferred.

## Requirements

- .NET 10

## Documentation

- [VOID Wiki](https://github.com/Shmellyorc/Void/wiki)
- [Void.Packer](https://www.nuget.org/packages/Void.Packer)
- [GitHub Repository](https://github.com/Shmellyorc/Void)

## License

MIT. No royalties. No engine fees.
