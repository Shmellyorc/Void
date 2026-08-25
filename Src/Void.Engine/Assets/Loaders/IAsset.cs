// ============================================================================
//  IAsset.cs
// ============================================================================
//  Core interface for all asset types in the asset management system.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Defines the type of an asset for management and lifecycle purposes.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// The asset type is unknown or not set.
    /// </summary>
    None,

    /// <summary>
    /// A standard asset loaded from file data and managed by the <see cref="AssetManager"/>.
    /// </summary>
    Normal,

    /// <summary>
    /// A programmatically created asset that is not managed by the <see cref="AssetManager"/>.
    /// </summary>
    Instanced,

    /// <summary>
    /// An asset created from a render target, typically used for render textures.
    /// </summary>
    Atlas
}

/// <summary>
/// Defines the contract for all asset types in the asset management system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAsset"/> interface provides the core functionality for
/// assets managed by the <see cref="AssetManager"/>. All asset types must
/// implement this interface to be loaded, cached, and evicted properly.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Unique identifier for asset tracking</description></item>
///   <item><description>Tag for identification and logging</description></item>
///   <item><description>Raw data storage for reloading</description></item>
///   <item><description>Load/Unload lifecycle management</description></item>
///   <item><description>Access tick for LRU eviction</description></item>
///   <item><description>Asset type classification</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Asset is created with raw data and tag</description></item>
///   <item><description><see cref="Load"/> is called to create the underlying resource</description></item>
///   <item><description>Asset is used and access tick is updated</description></item>
///   <item><description>Asset may be <see cref="Unload"/>ed to free resources</description></item>
///   <item><description>Asset can be reloaded if accessed again</description></item>
///   <item><description>Asset is <see cref="IDisposable.Dispose"/>d when no longer needed</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Creating a custom asset type
/// public class CustomAsset : IAsset
/// {
///     public uint Id { get; }
///     public string Tag { get; }
///     public byte[] Data { get; }
///     public bool IsValid { get; private set; }
///     public AssetType Type => AssetType.Normal;
///     public ushort LastAccessTick { get; set; }
/// 
///     public CustomAsset(uint id, byte[] data, string tag)
///     {
///         Id = id;
///         Data = data;
///         Tag = tag;
///     }
/// 
///     public void Load()
///     {
///         // Create underlying resource from Data
///         IsValid = true;
///     }
/// 
///     public void Unload()
///     {
///         // Free underlying resource
///         IsValid = false;
///     }
/// 
///     public void Dispose()
///     {
///         // Clean up resources
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations should handle their own thread safety if accessed
/// from multiple threads.
/// </para>
/// </remarks>
public interface IAsset : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this asset.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets the tag or path used to identify this asset.
    /// </summary>
    string Tag { get; }

    /// <summary>
    /// Gets the raw data bytes of the asset.
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// Gets a value indicating whether the asset is loaded and ready for use.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the type of the asset for management purposes.
    /// </summary>
    AssetType Type { get; }

    /// <summary>
    /// Gets the last time this asset was accessed, used for LRU eviction.
    /// </summary>
    DateTime LastAccessTime { get; }

    /// <summary>
    /// Loads the asset data into memory.
    /// </summary>
    /// <remarks>
    /// This method should create the underlying resource from the raw data.
    /// If the asset is already loaded, it should update the last access time.
    /// </remarks>
    void Load();

    /// <summary>
    /// Unloads the asset data from memory.
    /// </summary>
    /// <remarks>
    /// This method should free the underlying resource while keeping the
    /// raw data available for reloading.
    /// </remarks>
    void Unload();
}