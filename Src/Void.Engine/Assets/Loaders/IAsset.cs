// ============================================================================
//  IAsset.cs
// ============================================================================
//  Shared asset lifecycle contract used by the asset manager and custom assets.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Identifies how an asset was created and how VOID manages its lifetime.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Indicates that the asset has no assigned lifecycle type.
    /// </summary>
    None,

    /// <summary>
    /// Represents an asset loaded from source data and managed by <see cref="AssetManager"/>.
    /// </summary>
    Normal,

    /// <summary>
    /// Represents an asset created directly by game or engine code rather than loaded by <see cref="AssetManager"/>.
    /// </summary>
    Instanced,

    /// <summary>
    /// Represents a texture-like asset backed by renderer-managed data such as an atlas page or render target.
    /// </summary>
    Atlas
}

/// <summary>
/// Defines the lifecycle contract for assets that can be loaded, cached, unloaded, and disposed by VOID.
/// </summary>
/// <remarks>
/// <para>
/// Custom asset types registered with <see cref="AssetManager.RegisterAssetType{T}"/>
/// implement this interface so the asset manager can track their source data, validity, and last access time.
/// </para>
/// <para>
/// <see cref="Load"/> should make the asset ready for use and refresh <see cref="LastAccessTime"/>.
/// <see cref="Unload"/> should release reloadable resources while preserving enough source data to load the asset again.
/// <see cref="IDisposable.Dispose"/> should perform final cleanup.
/// </para>
/// <code>
/// public sealed class DialogueAsset : IAsset
/// {
///     public uint Id { get; }
///     public string Tag { get; }
///     public byte[] Data { get; }
///     public bool IsValid { get; private set; }
///     public AssetType Type => AssetType.Normal;
///     public DateTime LastAccessTime { get; private set; }
///
///     public DialogueAsset(uint id, byte[] data, string tag)
///     {
///         Id = id;
///         Data = data;
///         Tag = tag;
///     }
///
///     public void Load()
///     {
///         LastAccessTime = DateTime.Now;
///         IsValid = true;
///     }
///
///     public void Unload() => IsValid = false;
///     public void Dispose() => Unload();
/// }
/// </code>
/// </remarks>
public interface IAsset : IDisposable
{
    /// <summary>
    /// Gets the identifier assigned to this asset instance.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets the asset tag, typically the normalized source path used to load it.
    /// </summary>
    string Tag { get; }

    /// <summary>
    /// Gets the source bytes retained by the asset for loading or recreation.
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// Gets whether the asset is currently loaded and ready for use.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the lifecycle type assigned to the asset.
    /// </summary>
    AssetType Type { get; }

    /// <summary>
    /// Gets the most recent time the asset was accessed or refreshed.
    /// </summary>
    /// <remarks>
    /// <see cref="AssetManager"/> uses this value when deciding which managed assets are eligible for eviction.
    /// </remarks>
    DateTime LastAccessTime { get; }

    /// <summary>
    /// Loads or refreshes the asset so it is ready for use.
    /// </summary>
    /// <remarks>
    /// Implementations should update <see cref="LastAccessTime"/> when the asset is accessed through this method.
    /// </remarks>
    void Load();

    /// <summary>
    /// Releases reloadable resources owned by the asset.
    /// </summary>
    /// <remarks>
    /// Managed assets should retain enough source data to support a later call to <see cref="Load"/>.
    /// </remarks>
    void Unload();
}
