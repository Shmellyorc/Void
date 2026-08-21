// ============================================================================
//  MacOsMount.cs
// ============================================================================
//  Mount for accessing assets within a macOS application bundle's Resources folder.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// A mount that provides access to assets within a macOS application bundle's Resources folder.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MacOsMount"/> class automatically detects whether the application
/// is running from a bundled .app directory and maps virtual paths to the
/// appropriate resource location. In development, it falls back to the
/// content root directory.
/// </para>
/// <para>
/// This mount is automatically added by the <see cref="AssetManager"/> when
/// running on macOS.
/// </para>
/// <para>
/// <b>Bundle Detection:</b>
/// <list type="bullet">
///   <item><description><b>Bundled:</b> Paths are mapped to the .app/Contents/Resources directory</description></item>
///   <item><description><b>Development:</b> Paths are mapped to the configured content root</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // The mount is automatically added by AssetManager on macOS
/// // No manual creation is required
/// 
/// // To add a custom mount alongside the macOS mount:
/// AssetManager.Instance.AddMountToStart(new MyCustomMount());
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe as it only reads from the file system.
/// </para>
/// </remarks>
public sealed class MacOsMount : IMount
{
    private readonly string _resourcePath;

    /// <summary>
    /// Gets the name of the mount.
    /// </summary>
    public string Name => "MacOs Bundle";

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOsMount"/> class.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not macOS.</exception>
    public MacOsMount()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsMount is only supported on MacOs");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        if (bundlePath.Contains(".app/Contents/MacOs"))
        {
            _resourcePath = bundlePath.Replace("MacOs", "Resources");
        }
        else
        {
            _resourcePath = GameSettings.Instance.AppContentRoot;
        }

        if (!Directory.Exists(_resourcePath))
            _resourcePath = GameSettings.Instance.AppContentRoot;
    }

    /// <summary>
    /// Determines whether a file exists at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public bool HasFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Reads the file at the specified virtual path from the bundle's Resources folder.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public byte[] ReadFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.ReadAllBytes(fullPath);
    }
}