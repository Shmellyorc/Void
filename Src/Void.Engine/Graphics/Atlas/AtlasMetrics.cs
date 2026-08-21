// ============================================================================
//  AtlasMetrics.cs
// ============================================================================
//  Contains metrics and statistics about atlas usage, including page counts,
//  space utilization, texture count, and eviction history.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Contains metrics and statistics about atlas usage.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AtlasMetrics"/> structure provides detailed information
/// about the current state of the atlas system. These metrics are useful
/// for monitoring atlas efficiency, diagnosing performance issues, and
/// optimizing texture packing.
/// </para>
/// <para>
/// Metrics are obtained by calling <see cref="AtlasManager.GetMetrics"/>.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var metrics = AtlasManager.Instance.GetMetrics();
/// 
/// Console.WriteLine($"Atlas usage: {metrics.PercentageFull:F1}%");
/// Console.WriteLine($"Textures packed: {metrics.TextureCount}");
/// Console.WriteLine($"Pages used: {metrics.UsedPages}/{metrics.TotalPages}");
/// Console.WriteLine($"Evictions: {metrics.EvictionCount}");
/// 
/// if (metrics.PercentageFull > 90f)
/// {
///     // Atlas is nearly full, consider increasing page count
/// }
/// </code>
/// </para>
/// </remarks>
public struct AtlasMetrics
{
    /// <summary>
    /// Gets the total number of atlas pages allocated.
    /// </summary>
    /// <value>The total page count configured for the atlas.</value>
    public int TotalPages { get; internal set; }

    /// <summary>
    /// Gets the number of atlas pages currently in use.
    /// </summary>
    /// <value>The number of pages that contain at least one packed texture.</value>
    public int UsedPages { get; internal set; }

    /// <summary>
    /// Gets the total available space across all atlas pages.
    /// </summary>
    /// <value>The total space in pixels (width × height × page count).</value>
    public int TotalSpaceBytes { get; internal set; }

    /// <summary>
    /// Gets the amount of space currently used by packed textures.
    /// </summary>
    /// <value>The used space in pixels.</value>
    public int UsedSpaceBytes { get; internal set; }

    /// <summary>
    /// Gets the percentage of atlas space currently occupied by packed textures.
    /// </summary>
    /// <value>A value between 0 and 100 representing the fullness percentage.</value>
    public float PercentageFull { get; internal set; }

    /// <summary>
    /// Gets the number of textures currently packed in the atlas.
    /// </summary>
    /// <value>The total number of packed texture entries.</value>
    public int TextureCount { get; internal set; }

    /// <summary>
    /// Gets the number of evictions performed by the atlas manager.
    /// </summary>
    /// <value>
    /// The total number of textures evicted from the atlas to make room
    /// for new textures.
    /// </value>
    /// <remarks>
    /// A high eviction count may indicate that the atlas is too small or
    /// that textures are being used inefficiently.
    /// </remarks>
    public int EvictionCount { get; internal set; }
}
