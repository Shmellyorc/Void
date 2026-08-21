// ============================================================================
//  MacOsPackMount.cs
// ============================================================================
//  macOS-specific mount for loading asset packs from the application bundle.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// A macOS-specific mount for loading asset packs from the application bundle's Resources folder.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MacOsPackMount"/> class combines macOS bundle resource loading
/// with pack archive functionality. It automatically locates the pack file
/// within the bundle's Resources directory and creates a <see cref="PackMount"/>
/// for asset access.
/// </para>
/// <para>
/// This mount is useful for distributing encrypted or compressed assets in a
/// macOS application bundle.
/// </para>
/// <para>
/// <b>Bundle Detection:</b>
/// <list type="bullet">
///   <item><description><b>Bundled:</b> Pack is loaded from the .app/Contents/Resources directory</description></item>
///   <item><description><b>Development:</b> Pack is loaded from the configured content root</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a pack from the macOS bundle
/// var packMount = new MacOsPackMount("game_data.pack");
/// AssetManager.Instance.AddMountToEnd(packMount);
/// 
/// // Load with encryption key
/// var encryptedPack = new MacOsPackMount("secure.pack", encryptionKey);
/// AssetManager.Instance.AddMountToStart(encryptedPack);
/// 
/// // Verify pack integrity
/// if (packMount.VerifyIntegrity())
/// {
///     // Pack is valid
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
/// This class delegates to <see cref="PackMount"/> which is thread-safe.
/// </para>
/// </remarks>
public sealed class MacOsPackMount : IMount, IDisposable
{
    private readonly PackMount _packMount;
    private readonly string _resourcePath;
    private bool _disposed;

    /// <summary>
    /// Gets the name of the mount.
    /// </summary>
    public string Name => _packMount?.Name ?? "MacOs Pack Mount";

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOsPackMount"/> class.
    /// </summary>
    /// <param name="packFileName">The name of the pack file within the bundle's Resources folder.</param>
    /// <param name="key">The optional encryption key for the pack.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not macOS.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the pack file is not found.</exception>
    public MacOsPackMount(string packFileName, byte[] key = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsPackMount is only supported on MacOS");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        string resourcePath;
        if (bundlePath.Contains(".app/Contents/MacOs"))
        {
            resourcePath = bundlePath.Replace("MacOs", "Resources");
        }
        else
        {
            resourcePath = GameSettings.Instance.AppContentRoot;
        }

        if (!Directory.Exists(resourcePath))
            resourcePath = GameSettings.Instance.AppContentRoot;

        _resourcePath = resourcePath;

        string packPath = Path.Combine(_resourcePath, packFileName);

        if (!File.Exists(packPath))
            throw new FileNotFoundException($"Pack file not found: {packPath}");

        byte[] packData = File.ReadAllBytes(packPath);

        if (key == null)
        {
            string keyPath = Path.ChangeExtension(packPath, ".key");
            if (File.Exists(keyPath))
            {
                key = File.ReadAllBytes(keyPath);
            }
        }

        _packMount = new PackMount(packData, key, Path.GetFileNameWithoutExtension(packFileName));
    }

    /// <summary>
    /// Determines whether a file exists in the pack.
    /// </summary>
    public bool HasFile(string virtualPath)
        => _packMount.HasFile(virtualPath);

    /// <summary>
    /// Reads a file from the pack.
    /// </summary>
    public byte[] ReadFile(string virtualPath)
        => _packMount.ReadFile(virtualPath);

    /// <summary>
    /// Verifies the integrity of the pack.
    /// </summary>
    /// <returns><see langword="true"/> if the pack integrity is valid; otherwise, <see langword="false"/>.</returns>
    public bool VerifyIntegrity()
        => _packMount.VerifyIntegrity();

    /// <summary>
    /// Lists all files contained in the pack.
    /// </summary>
    /// <returns>An enumerable of file paths.</returns>
    public IEnumerable<string> ListFiles()
        => _packMount.ListFiles();

    /// <summary>
    /// Disposes the pack mount and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _packMount?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}