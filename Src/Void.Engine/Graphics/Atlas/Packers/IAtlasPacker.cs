// ============================================================================
//  IAtlasPacker.cs
// ============================================================================
//  Contract for pluggable texture-atlas packing algorithms.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Defines the geometry contract used by VOID texture-atlas packers.
/// </summary>
/// <remarks>
/// <para>
/// A packer owns only allocation geometry. It does not upload pixels or manage
/// renderer resources. <see cref="AtlasManager"/> uses the rectangles returned by
/// this interface to update atlas pixels and renderer textures.
/// </para>
/// <para>
/// Custom packers selected through <see cref="GameSettings.SetAtlasPacker(Type)"/>
/// must provide a public constructor with the signature
/// <c>(int width, int height)</c>. The dimensions supplied to that constructor are
/// the atlas page dimensions in pixels.
/// </para>
/// <para>
/// All successful allocations must use integral pixel coordinates, remain fully
/// inside the configured page, preserve the requested width and height, and never
/// overlap another live allocation.
/// </para>
/// <code>
/// public sealed class MyAtlasPacker : IAtlasPacker
/// {
///     public MyAtlasPacker(int width, int height)
///     {
///         // Initialize one page of allocation state.
///     }
///
///     // Implement the allocation contract below.
/// }
/// </code>
/// </remarks>
public interface IAtlasPacker
{
    /// <summary>
    /// Attempts to reserve a rectangle of the requested size.
    /// </summary>
    /// <param name="width">The requested width in pixels.</param>
    /// <param name="height">The requested height in pixels.</param>
    /// <param name="packedRect">
    /// Receives the reserved rectangle when successful; otherwise,
    /// <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the rectangle was reserved; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// On success, the rectangle must have integral X and Y coordinates, exactly
    /// match the requested width and height, remain inside the page, and not
    /// intersect any other live allocation. Rotation is not part of the contract.
    /// </para>
    /// <para>
    /// The returned rectangle remains reserved until it is released through
    /// <see cref="Free"/>, removed by <see cref="Clear"/>, or relocated by a
    /// successful <see cref="Defrag"/> operation.
    /// </para>
    /// <para>
    /// A failed attempt must not mutate the existing allocation layout.
    /// </para>
    /// </remarks>
    bool TryPack(int width, int height, out Rect2 packedRect);

    /// <summary>
    /// Releases an exact live allocation.
    /// </summary>
    /// <param name="rect">A rectangle previously returned by a successful <see cref="TryPack"/> call.</param>
    /// <remarks>
    /// Releasing an exact live rectangle must make that allocation no longer live.
    /// Passing a rectangle that is not currently allocated must be a harmless no-op.
    /// Implementations may defer reuse of newly freed space when their packing model
    /// cannot safely represent that space without defragmentation.
    /// </remarks>
    void Free(Rect2 rect);

    /// <summary>
    /// Removes all live allocations and restores the initial empty-page state.
    /// </summary>
    /// <remarks>
    /// After this call, <see cref="UsedSpace"/> must be zero and the full page must
    /// be available for future allocations. This method manages geometry only and
    /// must not create, dispose, or update renderer resources.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Attempts to compact live allocations and reports every rectangle that moved.
    /// </summary>
    /// <returns>
    /// A list containing the exact old and new rectangle for each allocation whose
    /// position changed. Unchanged allocations are omitted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Defragmentation is transactional. If the packer cannot produce a complete
    /// valid replacement layout, it must leave the current layout unchanged and
    /// return an empty list.
    /// </para>
    /// <para>
    /// When moves are returned, the packer's live allocation state must already
    /// represent the new layout. Every new rectangle must preserve the old
    /// rectangle's dimensions, use integral pixel coordinates, remain inside the
    /// page, and not overlap another live allocation. Each moved old rectangle must
    /// appear exactly once in the returned list.
    /// </para>
    /// <para>
    /// Returning an empty list means that no allocation positions changed.
    /// </para>
    /// </remarks>
    List<(Rect2 OldRect, Rect2 NewRect)> Defrag();

    /// <summary>
    /// Gets a normalized estimate of external allocation fragmentation.
    /// </summary>
    /// <value>
    /// A value from 0 to 1, where 0 means the currently free area is fully useful
    /// for allocation and higher values indicate that more free area is split or
    /// trapped in a form that defragmentation may recover.
    /// </value>
    /// <remarks>
    /// <para>
    /// This value describes allocation fragmentation, not the percentage of the
    /// page that is unused. An empty page therefore reports 0 rather than 1.
    /// </para>
    /// <para>
    /// The built-in packers calculate this as
    /// <c>1 - (largest currently allocatable free rectangle area / total free area)</c>.
    /// Custom packers should use the same definition or a conservative equivalent
    /// based on space that can be allocated without moving live rectangles.
    /// A completely full page reports 0 because it has no free area to fragment.
    /// </para>
    /// </remarks>
    float Fragmentation { get; }

    /// <summary>
    /// Gets the total pixel area occupied by all live allocations.
    /// </summary>
    /// <remarks>
    /// This is the sum of <c>Width * Height</c> for the rectangles currently
    /// reserved by the packer. It is an area in pixels, not a byte count and not
    /// an allocator-specific envelope or bookkeeping estimate.
    /// </remarks>
    int UsedSpace { get; }

    /// <summary>
    /// Gets the total pixel area of the page managed by this packer.
    /// </summary>
    /// <remarks>
    /// For a page created with dimensions <c>width</c> and <c>height</c>, this value
    /// is <c>width * height</c>.
    /// </remarks>
    int TotalSpace { get; }
}
