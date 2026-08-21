// ============================================================================
//  BatchStats.cs
// ============================================================================
//  Contains performance statistics for batch rendering, including draw calls,
//  vertex counts, texture and blend mode switches, and CPU/GPU timing data.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Contains performance statistics for batch rendering operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BatchStats"/> structure tracks key performance metrics
/// for batched rendering, including:
/// <list type="bullet">
///   <item><description>Draw calls and vertex counts</description></item>
///   <item><description>Texture and blend mode switches</description></item>
///   <item><description>CPU and GPU timing data</description></item>
///   <item><description>Command counts for batched operations</description></item>
/// </list>
/// </para>
/// <para>
/// These statistics are useful for profiling and optimizing rendering performance.
/// They help identify bottlenecks such as excessive draw calls or texture switches.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var stats = batcher.Stats;
/// 
/// Console.WriteLine($"Draw Calls: {stats.DrawCalls}");
/// Console.WriteLine($"Vertices: {stats.Vertices}");
/// Console.WriteLine($"GPU Time: {stats.GPUTime:F2}ms");
/// 
/// if (stats.DrawCalls > 100)
/// {
///     // Consider optimizing batching or texture atlasing
/// }
/// </code>
/// </para>
/// </remarks>
public struct BatchStats
{
    /// <summary>
    /// Gets or sets the number of draw calls issued during the batch.
    /// </summary>
    /// <value>The total number of GPU draw calls.</value>
    /// <remarks>
    /// Lower draw call counts generally indicate better batching efficiency.
    /// </remarks>
    public int DrawCalls;

    /// <summary>
    /// Gets or sets the total number of vertices rendered during the batch.
    /// </summary>
    /// <value>The total vertex count.</value>
    public int Vertices;

    /// <summary>
    /// Gets or sets the total number of triangles rendered during the batch.
    /// </summary>
    /// <value>The total triangle count (vertices / 3 for triangle primitives).</value>
    public int Triangles;

    /// <summary>
    /// Gets or sets the number of draw commands processed during the batch.
    /// </summary>
    /// <value>The total number of batched commands.</value>
    public int Commands;

    /// <summary>
    /// Gets or sets the number of texture switches that occurred during the batch.
    /// </summary>
    /// <value>The total number of texture changes.</value>
    /// <remarks>
    /// High texture switch counts can indicate that textures are not being
    /// properly batched or that atlas packing could be improved.
    /// </remarks>
    public int TextureSwitches;

    /// <summary>
    /// Gets or sets the number of blend mode switches that occurred during the batch.
    /// </summary>
    /// <value>The total number of blend mode changes.</value>
    /// <remarks>
    /// High blend mode switch counts can impact rendering performance.
    /// </remarks>
    public int BlendModeSwitches;

    /// <summary>
    /// Gets or sets the CPU time spent processing the batch.
    /// </summary>
    /// <value>The CPU time in milliseconds.</value>
    public float CPUTime;

    /// <summary>
    /// Gets or sets the GPU time spent rendering the batch.
    /// </summary>
    /// <value>The GPU time in milliseconds.</value>
    public float GPUTime;

    /// <summary>
    /// Resets all statistics to their default (zero) values.
    /// </summary>
    /// <remarks>
    /// This method is called automatically when beginning a new batch
    /// and can be used to manually reset statistics if needed.
    /// </remarks>
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

    /// <summary>
    /// Returns a string representation of the batch statistics.
    /// </summary>
    /// <returns>A formatted string containing key statistics.</returns>
    public override string ToString()
        => $"DrawCalls: {DrawCalls}, Vertices: {Vertices}, Triangles: {Triangles}, Commands: {Commands}";
}