// ============================================================================
//  IAtlasPacker.cs
// ============================================================================
//  Defines the contract for texture atlas packing algorithms.
//  Provides methods for packing, freeing, defragmenting, and tracking
//  atlas space usage.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Defines the contract for texture atlas packing algorithms.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IAtlasPacker"/> interface provides methods for packing
/// rectangular textures into a larger atlas texture. Different implementations
/// use different algorithms (e.g., Guillotine, Skyline) with varying trade-offs
/// between packing efficiency, speed, and fragmentation.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Pack textures of varying sizes into a fixed-size atlas</description></item>
///   <item><description>Free individual textures when no longer needed</description></item>
///   <item><description>Defragment to recover wasted space</description></item>
///   <item><description>Track used space and fragmentation metrics</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var packer = new SkylinePacker(2048, 2048);
/// 
/// if (packer.TryPack(128, 128, out var packedRect))
/// {
///     // Texture was packed at packedRect.X, packedRect.Y
///     // Copy texture data to the atlas at this position
/// }
/// 
/// // Check fragmentation
/// float frag = packer.Fragmentation;
/// if (frag > 0.3f) // 30% wasted space
///     packer.Defrag();
/// 
/// // Free a texture when no longer needed
/// packer.Free(packedRect);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations are not guaranteed to be thread-safe. Use a single packer
/// per atlas and access it from the main thread.
/// </para>
/// </remarks>
public interface IAtlasPacker
{
    /// <summary>
    /// Attempts to pack a rectangle of the specified size into the atlas.
    /// </summary>
    /// <param name="width">The width of the rectangle to pack.</param>
    /// <param name="height">The height of the rectangle to pack.</param>
    /// <param name="packedRect">When this method returns, contains the packed position and size if successful; otherwise, <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if the rectangle was successfully packed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method searches for the best available free space in the atlas
    /// that can accommodate the requested size. The exact placement strategy
    /// depends on the specific packer implementation.
    /// </para>
    /// <para>
    /// If successful, the returned <paramref name="packedRect"/> contains
    /// the position (X, Y) and size (Width, Height) where the texture should
    /// be placed in the atlas.
    /// </para>
    /// </remarks>
    bool TryPack(int width, int height, out Rect2 packedRect);

    /// <summary>
    /// Clears all packed rectangles from the atlas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resets the packer to its initial state, removing all
    /// packed rectangles and resetting used space to zero. The atlas space
    /// becomes fully available for new textures.
    /// </para>
    /// <para>
    /// This does not dispose or release any underlying GPU resources.
    /// It only resets the packer's internal tracking state.
    /// </para>
    /// </remarks>
    void Clear();

    /// <summary>
    /// Defragments the atlas to reduce fragmentation and recover wasted space.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Over time, packing and freeing textures can leave fragmented free space
    /// that cannot be used for larger textures. Defragmentation rearranges
    /// packed textures to consolidate free space into larger contiguous blocks.
    /// </para>
    /// <para>
    /// Defragmentation is typically an expensive operation that should be
    /// performed sparingly, such as when <see cref="Fragmentation"/> exceeds
    /// a certain threshold.
    /// </para>
    /// <para>
    /// After defragmentation, existing references to packed rectangles become
    /// invalid and must be updated. The <see cref="AtlasManager"/> handles
    /// this automatically.
    /// </para>
    /// </remarks>
    void Defrag();

    /// <summary>
    /// Frees a previously packed rectangle, making its space available for reuse.
    /// </summary>
    /// <param name="rect">The rectangle to free, as returned from <see cref="TryPack"/>.</param>
    /// <remarks>
    /// <para>
    /// This method marks the specified rectangle as free space that can be
    /// used for future packing operations. The packer may merge adjacent
    /// free rectangles to reduce fragmentation.
    /// </para>
    /// <para>
    /// The rectangle must match exactly the rectangle that was returned from
    /// a previous successful call to <see cref="TryPack"/>.
    /// </para>
    /// </remarks>
    void Free(Rect2 rect);

    /// <summary>
    /// Gets the fragmentation percentage of the atlas.
    /// </summary>
    /// <value>
    /// A value between 0 and 1 representing the percentage of wasted space
    /// due to fragmentation. Higher values indicate more wasted space.
    /// </value>
    /// <remarks>
    /// <para>
    /// Fragmentation is calculated as: <c>1 - (UsedSpace / TotalSpace)</c>
    /// </para>
    /// <para>
    /// A fragmentation value of 0 means all used space is contiguous and
    /// efficiently packed. Higher values indicate that free space is fragmented
    /// into smaller blocks that cannot be used for larger textures.
    /// </para>
    /// </remarks>
    float Fragmentation { get; }

    /// <summary>
    /// Gets the total amount of space currently used by packed rectangles.
    /// </summary>
    /// <value>The total area (in pixels) occupied by packed textures.</value>
    /// <remarks>
    /// This value represents the sum of the areas of all packed rectangles.
    /// It does not include wasted space due to fragmentation.
    /// </remarks>
    int UsedSpace { get; }

    /// <summary>
    /// Gets the total available space in the atlas.
    /// </summary>
    /// <value>The total area (in pixels) of the atlas.</value>
    /// <remarks>
    /// This is the product of the atlas width and height, representing the
    /// maximum possible space available for packing textures.
    /// </remarks>
    int TotalSpace { get; }
}