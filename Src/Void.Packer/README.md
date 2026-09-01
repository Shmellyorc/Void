# Void Packer

Asset packing library with AES-GCM encryption, chunked reading, and CRC32 verification.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Void.Packer)](https://www.nuget.org/packages/Void.Packer)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

---

## What It Does

Void.Packer protects your assets from extraction and theft. When you release a game, your assets are your intellectual property. Within days, someone will extract your art, music, and levels and upload them to asset stores. Void.Packer prevents this by encrypting your assets into tamper-proof archives.

---

## Features

| Feature | Description |
|---------|-------------|
| **AES-GCM 256-bit** | Government/military-grade encryption |
| **Header + Data separation** | Different encryption for header and data sections |
| **PBKDF2 key derivation** | Salted key derivation for added security |
| **CRC32 verification** | Per-file integrity checking |
| **Adaptive compression** | Never makes files larger |
| **Chunked encryption** | Per-chunk authentication, only decrypts what you need |
| **Stream-based reading** | No full pack loaded into memory |
| **Thread-safe** | Safe for concurrent asset loading |
| **Incremental updates** | Fast updates in seconds, not minutes |

---

## Install

```bash
dotnet add package Void.Packer