// ============================================================================
//  MacOsPackMount.cs
// ============================================================================
//  Pack mount that resolves its archive from macOS application resources.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Provides access to a VOID asset pack stored with a macOS application.
/// </summary>
/// <remarks>
/// <para>
/// The pack path is resolved from the application resource directory when a
/// bundle is detected, or from <see cref="GameSettings.AppContentRoot"/> when
/// running outside a bundle. Pack operations are delegated to an internal
/// <see cref="PackMount"/>.
/// </para>
/// <para>
/// When no key is supplied, the constructor looks for a <c>.key</c> file next
/// to the pack using the same base file name. Creating this mount does not add
/// it to <see cref="AssetManager"/> automatically.
/// </para>
/// </remarks>
public sealed class MacOsPackMount : IMount, IDisposable
{
    private readonly PackMount _packMount;
    private readonly string _resourcePath;
    private bool _disposed;

    /// <summary>
    /// Gets the display name of the underlying pack mount.
    /// </summary>
    public string Name => _packMount?.Name ?? "MacOs Pack Mount";

    /// <summary>
    /// Initializes a mount for a pack stored with a macOS application.
    /// </summary>
    /// <param name="packFileName">The pack file name relative to the resource directory.</param>
    /// <param name="key">
    /// The optional pack key. When omitted, a sibling <c>.key</c> file is used
    /// when one exists.
    /// </param>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform is not macOS.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// The resolved pack file does not exist.
    /// </exception>
    public MacOsPackMount(string packFileName, byte[] key = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsPackMount is only supported on MacOS");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        string resourcePath;
        if (bundlePath.Contains("Contents/MacOS"))
        {
            resourcePath = bundlePath.Replace("MacOS", "Resources");
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

        if (key == null)
        {
            string keyPath = Path.ChangeExtension(packPath, ".key");
            if (File.Exists(keyPath))
            {
                key = File.ReadAllBytes(keyPath);
            }
        }

        _packMount = new PackMount(packPath, key, Path.GetFileNameWithoutExtension(packFileName));
    }

    /// <summary>
    /// Determines whether the pack contains the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file exists in the pack; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool HasFile(string virtualPath)
        => _packMount.HasFile(virtualPath);

    /// <summary>
    /// Reads a file from the pack.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to read.</param>
    /// <returns>The file contents.</returns>
    public byte[] ReadFile(string virtualPath)
        => _packMount.ReadFile(virtualPath);

    /// <summary>
    /// Verifies the integrity information stored by the pack format.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when verification succeeds; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool VerifyIntegrity()
        => _packMount.VerifyIntegrity();

    /// <summary>
    /// Enumerates the virtual file paths stored in the pack.
    /// </summary>
    /// <returns>The file paths reported by the pack reader.</returns>
    public IEnumerable<string> ListFiles()
        => _packMount.ListFiles();

    /// <summary>
    /// Releases the underlying pack reader.
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