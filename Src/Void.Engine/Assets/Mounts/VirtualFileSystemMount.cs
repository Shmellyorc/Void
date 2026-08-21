// ============================================================================
//  VirtualFileSystemMount.cs
// ============================================================================
//  Mount that provides direct file system access to the content root.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// A mount that provides direct file system access to the content root.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="VirtualFileSystemMount"/> class maps virtual paths to physical
/// paths within the configured content root directory. It is the default
/// mount used by the <see cref="AssetManager"/> and provides direct file
/// system access for asset loading.
/// </para>
/// <para>
/// This mount is automatically added by the <see cref="AssetManager"/> and
/// serves as the fallback when no other mounts contain the requested file.
/// </para>
/// <para>
/// <b>Path Security:</b>
/// This mount includes security checks to prevent directory traversal attacks.
/// All paths are validated to ensure they remain within the content root.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // The mount is automatically added by AssetManager
/// // No manual creation is required
/// 
/// // To use it directly (for custom mount implementations):
/// var vfs = new VirtualFileSystemMount();
/// if (vfs.HasFile("textures/player.png"))
/// {
///     byte[] data = vfs.ReadFile("textures/player.png");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe as it only reads from the file system.
/// </para>
/// </remarks>
public sealed class VirtualFileSystemMount : IMount
{
    /// <summary>
    /// Gets the name of the mount.
    /// </summary>
    public string Name => "Virtual File System";

    /// <summary>
    /// Determines whether a file exists at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public bool HasFile(string virtualPath)
    {
        string fullPath = GetFullPath(virtualPath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Reads the file at the specified virtual path and returns its contents as a byte array.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the path attempts to access files outside the content root.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
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