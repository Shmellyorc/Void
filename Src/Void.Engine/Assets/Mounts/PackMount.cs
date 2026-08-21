// ============================================================================
//  PackMount.cs
// ============================================================================
//  Mount for encrypted and/or compressed asset pack archives.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Void.Packer;
using Void.Packer.Utils;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// A mount for encrypted and/or compressed asset pack archives.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackMount"/> class provides read-only access to assets stored
/// in a pack archive. Packs can optionally be encrypted and/or compressed
/// for secure and efficient asset delivery.
/// </para>
/// <para>
/// Packs are loaded as mounts and become part of the asset search order.
/// They support integrity verification to ensure data hasn't been corrupted
/// or tampered with.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Encrypted asset storage with optional key</description></item>
///   <item><description>Automatic path normalization and caching</description></item>
///   <item><description>Integrity verification</description></item>
///   <item><description>Thread-safe file access</description></item>
///   <item><description>Path cache for fast lookups</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a pack from file data
/// byte[] packData = File.ReadAllBytes("assets.pack");
/// var packMount = new PackMount(packData, null, "GameAssets");
/// AssetManager.Instance.AddMountToEnd(packMount);
/// 
/// // Load an encrypted pack
/// byte[] key = File.ReadAllBytes("assets.key");
/// var encryptedPack = new PackMount(packData, key, "SecureAssets");
/// AssetManager.Instance.AddMountToStart(encryptedPack);
/// 
/// // Verify pack integrity
/// if (packMount.VerifyIntegrity())
/// {
///     // Pack is valid and not corrupted
/// }
/// 
/// // List all files in the pack
/// foreach (var file in packMount.ListFiles())
/// {
///     Console.WriteLine(file);
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. File operations are synchronized using locks
/// and the underlying reader is thread-safe.
/// </para>
/// </remarks>
public sealed class PackMount : IMount, IDisposable
{
    private readonly SolidPackReader _reader;
    private readonly string _mountName;
    private readonly Dictionary<string, string> _pathCache;
    private readonly Lock _cacheLock = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackMount"/> class.
    /// </summary>
    /// <param name="packData">The raw pack data bytes.</param>
    /// <param name="key">The optional encryption key for the pack.</param>
    /// <param name="mountName">The name of the mount for identification.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="packData"/> is null or empty.</exception>
    public PackMount(byte[] packData, byte[] key = null, string mountName = null)
    {
        if (packData.IsEmpty())
            throw new ArgumentException("Pack data cannot be null or empty.", nameof(packData));

        _reader = new SolidPackReader(packData, key);
        _mountName = mountName ?? $"Pack mount ({_reader.FileCount}) files";
        _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        BuildPathCache();
    }

    /// <summary>
    /// Gets the name of the mount.
    /// </summary>
    public string Name => _mountName;

    /// <summary>
    /// Determines whether a file exists in the pack.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the mount has been disposed.</exception>
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
    /// Reads a file from the pack and returns its contents as a byte array.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the mount has been disposed.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist in the pack.</exception>
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
    /// Verifies the integrity of the pack data.
    /// </summary>
    /// <returns><see langword="true"/> if the pack integrity is valid; otherwise, <see langword="false"/>.</returns>
    public bool VerifyIntegrity() => _reader.VerifyIntegrity();

    /// <summary>
    /// Lists all files contained in the pack.
    /// </summary>
    /// <returns>An enumerable of file paths.</returns>
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
    /// Disposes the pack mount and releases all resources.
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