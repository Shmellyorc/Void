# Void Packer CLI

Command-line tool for creating, updating, and managing asset packs for Void Engine.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Packer.CLI)](https://www.nuget.org/packages/Void.Packer.CLI)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

---

## Install

```bash
dotnet tool install --global Void.Packer.CLI
```

Or run from source:

```bash
dotnet run --project Void.Packer.CLI -- build -c Content/ -o Packs/
```

---

## Commands

### Build

Creates a new pack from your content directory.

```bash
packer build -c Content/ -o Packs/ -n GameAssets
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--content` | `-c` | Content directory to pack | Required |
| `--output` | `-o` | Output directory | Required |
| `--name` | `-n` | Base name for output files | GameAssets |
| `--include` | `-i` | Include patterns (comma separated) | `*/` |
| `--exclude` | `-e` | Exclude patterns (comma separated) | None |
| `--encrypt` | | Enable encryption | true |
| `--compress` | | Compression: None, Deflate, Brotli | Deflate |
| `--adaptive` | | Use adaptive compression | true |
| `--compression-level` | | Compression level (1-9) | 6 |
| `--chunk-size` | | Chunk size in KB (0 = solid) | 1024 |
| `--verbose` | `-v` | Verbose output | false |

### Update

Fast incremental updates — seconds, not minutes.

```bash
packer update --pack GameAssets.pack --add Content/newfile.png --remove old/texture.png
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--pack` | | Existing pack file to update | Required |
| `--add` | `-a` | Files or folders to add | None |
| `--remove` | `-r` | Virtual paths to remove | None |
| `--key` | | Key file | Auto-detected |
| `--output` | `-o` | Output path | Overwrite |

### Extract

Extracts all files from a pack.

```bash
packer extract --pack GameAssets.pack --output Extracted/
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--pack` | | Pack file to extract | Required |
| `--output` | `-o` | Output directory | Required |
| `--key` | | Key file | Auto-detected |

### Verify

Verifies pack integrity via CRC32 checksums.

```bash
packer verify --pack GameAssets.pack
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--pack` | | Pack file to verify | Required |
| `--key` | | Key file | Auto-detected |

### List

Lists all files in a pack.

```bash
packer list --pack GameAssets.pack --detailed
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--pack` | | Pack file to inspect | Required |
| `--key` | | Key file | Auto-detected |
| `--detailed` | | Show detailed info | false |

---

## Auto Key Detection

All commands automatically detect the key file. If you have `GameAssets.pack`, the tool looks for `GameAssets.key` in the same directory.

---

## Error Output

User-friendly error messages instead of raw exceptions:

```
❌ Error: Invalid encryption key.
❌ Error: Pack file not found.
```

---

## Exit Codes

- `0` = Success
- `1` = Failure (CI/CD friendly)

---

## Examples

```bash
# Build with exclusions
packer build -c Content/ -o Packs/ -e "**/*.ase*"

# Build with custom chunk size
packer build -c Content/ -o Packs/ --chunk-size 512

# Update a pack
packer update --pack Packs/GameAssets.pack --add Content/newlevel.json

# Extract a pack
packer extract --pack Packs/GameAssets.pack --output Extracted/

# Verify before shipping
packer verify --pack Packs/GameAssets.pack
```

---

## Requirements

- .NET 10

---

## License

MIT