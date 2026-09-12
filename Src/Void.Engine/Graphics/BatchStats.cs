// ============================================================================
//  BatchStats.cs
// ============================================================================
//  Statistics reported by VOID batchers for their most recent flush.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Contains rendering statistics reported by a batcher.
/// </summary>
/// <remarks>
/// <para>
/// Built-in batchers reset these values when a batch begins and update them when
/// a non-empty flush completes. The values describe the most recently reported
/// flush rather than a cumulative lifetime total.
/// </para>
/// <para><code>
/// BatchStats stats = batcher.Stats;
/// Console.WriteLine($"{stats.DrawCalls} draw calls, {stats.Vertices} vertices");
/// </code></para>
/// </remarks>
public struct BatchStats
{
    /// <summary>Gets or sets the number of draw submissions issued by the flush.</summary>
    public int DrawCalls;

    /// <summary>Gets or sets the number of vertices reported by the flush.</summary>
    public int Vertices;

    /// <summary>Gets or sets the number of triangles reported by the batcher.</summary>
    /// <remarks>Concrete batchers may report zero when triangle accounting is not provided.</remarks>
    public int Triangles;

    /// <summary>Gets or sets the number of commands processed by the flush.</summary>
    public int Commands;

    /// <summary>Gets or sets the number of texture switches reported by the batcher.</summary>
    public int TextureSwitches;

    /// <summary>Gets or sets the number of blend-mode switches reported by the batcher.</summary>
    public int BlendModeSwitches;

    /// <summary>Gets or sets CPU-side flush time in milliseconds.</summary>
    /// <remarks>
    /// Built-in batchers measure calling-thread wall-clock time spent sorting,
    /// preparing or uploading geometry, and issuing draw submissions. This is not GPU time.
    /// </remarks>
    public float CPUTime;

    /// <summary>Gets or sets backend-reported GPU execution time in milliseconds.</summary>
    /// <remarks>
    /// VOID's built-in batchers do not currently issue GPU timer queries, so this
    /// value remains zero unless a future or custom implementation supplies it.
    /// </remarks>
    public float GPUTime;

    /// <summary>Resets every statistic to zero.</summary>
    public void Reset()
    {
        DrawCalls = 0;
        Vertices = 0;
        Triangles = 0;
        Commands = 0;
        TextureSwitches = 0;
        BlendModeSwitches = 0;
        CPUTime = 0;
        GPUTime = 0;
    }

    /// <summary>Returns the primary counters in a compact diagnostic string.</summary>
    /// <returns>A string containing draw-call, vertex, triangle, and command counts.</returns>
    public override string ToString()
        => $"DrawCalls: {DrawCalls}, Vertices: {Vertices}, Triangles: {Triangles}, Commands: {Commands}";
}
