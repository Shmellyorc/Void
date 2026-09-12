// ============================================================================
//  MacOsMount.cs
// ============================================================================
//  File-system mount for resources stored in a macOS application bundle.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Provides file-system access to resources for a macOS application.
/// </summary>
/// <remarks>
/// <para>
/// When a bundle resource directory is detected, virtual paths are resolved
/// relative to that directory. Otherwise the mount falls back to
/// <see cref="GameSettings.AppContentRoot"/> for development use.
/// </para>
/// <para>
/// <see cref="AssetManager"/> adds this mount automatically when VOID is
/// running on macOS.
/// </para>
/// </remarks>
public sealed class MacOsMount : IMount
{
    private readonly string _resourcePath;

    /// <summary>
    /// Gets the display name of the mount.
    /// </summary>
    public string Name => "MacOs Bundle";

    /// <summary>
    /// Initializes a macOS resource mount.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform is not macOS.
    /// </exception>
    public MacOsMount()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsMount is only supported on MacOs");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        if (bundlePath.Contains("Contents/MacOS"))
        {
            _resourcePath = bundlePath.Replace("MacOS", "Resources");
        }
        else
        {
            _resourcePath = GameSettings.Instance.AppContentRoot;
        }

        if (!Directory.Exists(_resourcePath))
            _resourcePath = GameSettings.Instance.AppContentRoot;
    }

    /// <summary>
    /// Determines whether a file exists in the resolved resource directory.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool HasFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Reads a file from the resolved resource directory.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to read.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">
    /// The requested file does not exist.
    /// </exception>
    public byte[] ReadFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.ReadAllBytes(fullPath);
    }
}