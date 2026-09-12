// ============================================================================
//  VirtualFileSystemMount.cs
// ============================================================================
//  File-system mount rooted at the configured content directory.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Provides direct file-system access beneath the configured content root.
/// </summary>
/// <remarks>
/// <para>
/// Virtual paths are resolved relative to <see cref="GameSettings.AppContentRoot"/>.
/// <see cref="AssetManager"/> installs this as a default mount.
/// </para>
/// <para>
/// Resolved paths are checked before access so that a path cannot escape the
/// configured content root through parent-directory traversal.
/// </para>
/// </remarks>
public sealed class VirtualFileSystemMount : IMount
{
    /// <summary>
    /// Gets the display name of the mount.
    /// </summary>
    public string Name => "Virtual File System";

    /// <summary>
    /// Determines whether a file exists beneath the content root.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file exists; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// The resolved path falls outside the configured content root.
    /// </exception>
    public bool HasFile(string virtualPath)
    {
        string fullPath = GetFullPath(virtualPath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Reads a file beneath the content root.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to read.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// The resolved path falls outside the configured content root.
    /// </exception>
    /// <exception cref="FileNotFoundException">The requested file does not exist.</exception>
    public byte[] ReadFile(string virtualPath)
    {
        string fullPath = GetFullPath(virtualPath);
        return File.ReadAllBytes(fullPath);
    }

    private string GetFullPath(string virtualPath)
    {
        string contentRoot = GameSettings.Instance.AppContentRoot;

        if (!contentRoot.EndsWith('/') && !contentRoot.EndsWith('\\'))
            contentRoot += Path.DirectorySeparatorChar;

        string fullPath = Path.GetFullPath(Path.Combine(contentRoot, virtualPath));

        if (!fullPath.StartsWith(Path.GetFullPath(contentRoot)))
            throw new UnauthorizedAccessException($"Cannot access files outside of ContentRoot: {virtualPath}");

        return fullPath;
    }
}
