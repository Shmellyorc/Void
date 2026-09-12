// ============================================================================
//  AtlasMetrics.cs
// ============================================================================
//  Contains metrics and statistics about atlas usage, including page counts,
//  pixel-area utilization, texture count, and eviction history.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Contains a snapshot of texture atlas usage statistics.
/// </summary>
/// <remarks>
/// <para>
/// Metrics are obtained from <see cref="AtlasManager.GetMetrics"/>. Space values
/// describe two-dimensional pixel area, not memory usage in bytes.
/// </para>
/// <code>
/// AtlasMetrics metrics = AtlasManager.Instance.GetMetrics();
///
/// Console.WriteLine($"Atlas usage: {metrics.PercentageFull:F1}%");
/// Console.WriteLine($"Pixel area: {metrics.UsedPixelArea}/{metrics.TotalPixelArea}");
/// Console.WriteLine($"Pages used: {metrics.UsedPages}/{metrics.TotalPages}");
/// Console.WriteLine($"Evictions: {metrics.EvictionCount}");
/// </code>
/// </remarks>
public struct AtlasMetrics
{
    /// <summary>
    /// Gets the total number of atlas pages configured for the manager.
    /// </summary>
    public int TotalPages { get; internal set; }

    /// <summary>
    /// Gets the number of atlas pages that currently contain at least one packed region.
    /// </summary>
    public int UsedPages { get; internal set; }

    /// <summary>
    /// Gets the total two-dimensional pixel area available across all configured atlas pages.
    /// </summary>
    /// <remarks>
    /// For square pages this is the sum of <c>pageSize * pageSize</c> for every
    /// configured page, including pages whose graphics resources have not yet been created.
    /// </remarks>
    public int TotalPixelArea { get; internal set; }

    /// <summary>
    /// Gets the two-dimensional pixel area occupied by live packed regions.
    /// </summary>
    public int UsedPixelArea { get; internal set; }

    /// <summary>
    /// Gets the total atlas pixel area.
    /// </summary>
    /// <remarks>
    /// This compatibility property retains the original public name. Despite the
    /// <c>Bytes</c> suffix, the value is pixel area and is identical to
    /// <see cref="TotalPixelArea"/>. New code should prefer <see cref="TotalPixelArea"/>.
    /// </remarks>
    public int TotalSpaceBytes
    {
        get => TotalPixelArea;
        internal set => TotalPixelArea = value;
    }

    /// <summary>
    /// Gets the pixel area occupied by live packed regions.
    /// </summary>
    /// <remarks>
    /// This compatibility property retains the original public name. Despite the
    /// <c>Bytes</c> suffix, the value is pixel area and is identical to
    /// <see cref="UsedPixelArea"/>. New code should prefer <see cref="UsedPixelArea"/>.
    /// </remarks>
    public int UsedSpaceBytes
    {
        get => UsedPixelArea;
        internal set => UsedPixelArea = value;
    }

    /// <summary>
    /// Gets the percentage of the configured atlas pixel area occupied by live packed regions.
    /// </summary>
    /// <value>A value from 0 through 100.</value>
    public float PercentageFull { get; internal set; }

    /// <summary>
    /// Gets the number of live texture-region entries currently cached in the atlas.
    /// </summary>
    public int TextureCount { get; internal set; }

    /// <summary>
    /// Gets the number of atlas entries evicted since the manager was initialized or cleared.
    /// </summary>
    public int EvictionCount { get; internal set; }
}
