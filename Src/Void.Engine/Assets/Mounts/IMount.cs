// ============================================================================
//  IMount.cs
// ============================================================================
//  Interface for virtual file system mounts that provide asset access.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Mounts;

/// <summary>
/// Defines the contract for virtual file system mounts that provide asset access.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IMount"/> interface represents a virtual file system that
/// can be searched by the <see cref="AssetManager"/> to locate and load assets.
/// Mounts are searched in priority order (first added = highest priority).
/// </para>
/// <para>
/// <b>Built-in Mount Implementations:</b>
/// <list type="bullet">
///   <item><description><see cref="VirtualFileSystemMount"/> - Direct file system access to the content root</description></item>
///   <item><description><see cref="MacOsMount"/> - macOS application bundle resource access</description></item>
///   <item><description><see cref="PackMount"/> - Encrypted and/or compressed asset pack archives</description></item>
///   <item><description><see cref="MacOsPackMount"/> - macOS-specific pack mount for bundle resources</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create and add a custom mount
/// var mount = new MyCustomMount();
/// AssetManager.Instance.AddMountToStart(mount);
/// 
/// // Or add to the end of the search order
/// AssetManager.Instance.AddMountToEnd(mount);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations should be thread-safe as they may be accessed concurrently
/// by the asset manager.
/// </para>
/// </remarks>
public interface IMount
{
    /// <summary>
    /// Gets the name of the mount for identification and logging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether a file exists at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    bool HasFile(string virtualPath);

    /// <summary>
    /// Reads the file at the specified virtual path and returns its contents as a byte array.
    /// </summary>
    /// <param name="virtualPath">The virtual path to the file.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist in the mount.</exception>
    byte[] ReadFile(string virtualPath);
}