// ============================================================================
//  IMount.cs
// ============================================================================
//  Interface for virtual asset sources used by the asset manager.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Defines a virtual asset source that can locate and read files by path.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssetManager"/> searches its mounts in order and uses the first
/// mount that reports a requested path through <see cref="HasFile"/>.
/// Implementations can provide assets from files, archives, memory, remote
/// storage, or another source without changing the asset loading API.
/// </para>
/// <para>
/// Add higher-priority mounts with <see cref="AssetManager.AddMountToStart"/>
/// and fallback mounts with <see cref="AssetManager.AddMountToEnd"/>.
/// </para>
/// <code>
/// IMount mount = new MyMount();
/// AssetManager.Instance.AddMountToStart(mount);
///
/// Texture texture = AssetManager.Instance.Load&lt;Texture&gt;("ui/icon.png");
/// </code>
/// </remarks>
public interface IMount
{
    /// <summary>
    /// Gets the display name used to identify the mount.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether the mount contains a file at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the path can be read from this mount;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool HasFile(string virtualPath);

    /// <summary>
    /// Reads the complete contents of a file from the mount.
    /// </summary>
    /// <param name="virtualPath">The virtual asset path to read.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">
    /// The requested path does not exist in the mount.
    /// </exception>
    byte[] ReadFile(string virtualPath);
}
