// ============================================================================
//  PackMount.cs
// ============================================================================
//  Read-only mount for VOID asset pack archives.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;

using Void.Packer;
using Void.Packer.Utils;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Provides read-only asset access to a VOID pack archive.
/// </summary>
/// <remarks>
/// <para>
/// Paths are normalized before lookup and an internal case-insensitive path
/// cache maps normalized virtual paths to the names stored in the archive.
/// Packs may be opened with an optional key when required by the archive.
/// </para>
/// <para>
/// Creating a <see cref="PackMount"/> does not add it to the asset search
/// order. Register the mount with <see cref="AssetManager"/> before loading
/// assets through it, and remove it before disposal when it is no longer used.
/// </para>
/// <code>
/// var mount = new PackMount("Content/game.pack", mountName: "GameAssets");
/// AssetManager.Instance.AddMountToStart(mount);
///
/// Texture icon = AssetManager.Instance.Load&lt;Texture&gt;("ui/icon.png");
///
/// AssetManager.Instance.RemoveMount(mount);
/// mount.Dispose();
/// </code>
/// </remarks>
public sealed class PackMount : IMount, IDisposable
{
    private readonly SolidPackReader _reader;
    private readonly string _mountName;
    private readonly Dictionary<string, string> _pathCache;
    private readonly Lock _cacheLock = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a mount from an existing pack file.
    /// </summary>
    /// <param name="packPath">The file-system path to the pack archive.</param>
    /// <param name="key">The optional key required to read the pack.</param>
    /// <param name="mountName">
    /// The display name for the mount. When omitted, a name is generated from
    /// the number of files in the archive.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="packPath"/> is null or empty.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// The pack file does not exist.
    /// </exception>
    public PackMount(string packPath, byte[] key = null, string mountName = null)
    {
        if (string.IsNullOrEmpty(packPath))
            throw new ArgumentException("Pack path cannot be null or empty.", nameof(packPath));

        if (!File.Exists(packPath))
            throw new FileNotFoundException($"Pack file not found: {packPath}");

        _reader = new SolidPackReader(packPath, key);
        _mountName = mountName ?? $"Pack mount ({_reader.FileCount}) files";
        _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        BuildPathCache();
    }

    /// <summary>
    /// Gets the display name of the mount.
    /// </summary>
    public string Name => _mountName;

    /// <summary>
    /// Determines whether the pack contains the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file exists in the pack; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The mount has been disposed.</exception>
    public bool HasFile(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PackMount));

        var normalized = FileHelper.Normalize(virtualPath);

        lock (_cacheLock)
        {
            if (_pathCache.TryGetValue(normalized, out _))
                return true;

            bool exists = _reader.FileExists(normalized);
            if (exists)
                _pathCache[normalized] = normalized;

            return exists;
        }
    }

    /// <summary>
    /// Reads the complete contents of a file from the pack.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to read.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="ObjectDisposedException">The mount has been disposed.</exception>
    /// <exception cref="FileNotFoundException">The requested path is not present in the pack.</exception>
    public byte[] ReadFile(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PackMount));

        var normalized = FileHelper.Normalize(virtualPath);

        lock (_cacheLock)
        {
            if (_pathCache.TryGetValue(normalized, out var originalPath))
            {
                return _reader.ReadFile(originalPath);
            }
        }

        if (_reader.FileExists(normalized))
        {
            return _reader.ReadFile(normalized);
        }

        throw new FileNotFoundException(
            $"File '{virtualPath}' not found in pack mount '{_mountName}'"
        );
    }

    /// <summary>
    /// Verifies the integrity information stored by the pack format.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when verification succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool VerifyIntegrity() => _reader.VerifyIntegrity();

    /// <summary>
    /// Enumerates the virtual file paths stored in the pack.
    /// </summary>
    /// <returns>The file paths reported by the pack reader.</returns>
    public IEnumerable<string> ListFiles() => _reader.ListFiles();

    private void BuildPathCache()
    {
        lock (_cacheLock)
        {
            _pathCache.Clear();
            foreach (var filepath in _reader.ListFiles())
            {
                var normalized = FileHelper.Normalize(filepath);
                _pathCache[normalized] = filepath;
            }
        }
    }

    /// <summary>
    /// Releases the pack reader and clears the path cache.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _reader?.Dispose();
            _pathCache.Clear();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
