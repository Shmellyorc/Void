// ============================================================================
//  IBatcher.cs
// ============================================================================
//  Defines the contract for batch rendering implementations. Provides methods
//  for beginning and ending batches, flushing commands, and accessing
//  performance statistics.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Specifies texture flip effects.
/// </summary>
[Flags]
public enum TextureEffects
{
    /// <summary>No texture flipping.</summary>
    None = 0,
    /// <summary>Flip the texture horizontally.</summary>
    Horizontal = 1 << 0,
    /// <summary>Flip the texture vertically.</summary>
    Vertical = 1 << 1
}

/// <summary>
/// Specifies the sorting mode for batched commands.
/// </summary>
public enum SortMode
{
    /// <summary>Commands are drawn immediately without sorting.</summary>
    Immediate,
    /// <summary>Commands are sorted from back to front (painter's algorithm).</summary>
    BackToFront,
    /// <summary>Commands are sorted from front to back.</summary>
    FrontToBack,
    /// <summary>Commands are deferred for later processing.</summary>
    Deferred
}

/// <summary>
/// Defines the contract for batch rendering implementations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IBatcher"/> interface provides a unified abstraction for
/// batch rendering systems. It supports:
/// <list type="bullet">
///   <item><description>Beginning and ending batches with configurable sort modes</description></item>
///   <item><description>Flushing batched commands to the GPU</description></item>
///   <item><description>Blend mode and camera management</description></item>
///   <item><description>Render target switching</description></item>
///   <item><description>Performance statistics tracking</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// IBatcher batcher = new SpriteBatcher();
/// 
/// // Begin a batch with back-to-front sorting
/// batcher.Begin(SortMode.BackToFront, BlendMode.Alpha, camera, renderTarget);
/// 
/// // Draw commands...
/// 
/// // End the batch and flush to GPU
/// batcher.End();
/// 
/// // Check performance
/// Console.WriteLine($"Draw calls: {batcher.DrawCallCount}");
/// Console.WriteLine($"Vertices: {batcher.VertexCount}");
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations are not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public interface IBatcher : IDisposable
{
    /// <summary>
    /// Begins a new batch with the specified settings.
    /// </summary>
    /// <param name="sort">The sort mode to use. If <see langword="null"/>, the default is used.</param>
    /// <param name="blendMode">The blend mode to use. If <see langword="null"/>, the default is used.</param>
    /// <param name="camera">The camera to use for rendering. If <see langword="null"/>, no camera is applied.</param>
    /// <param name="renderTarget">The render target to draw to. If <see langword="null"/>, the default is used.</param>
    /// <remarks>
    /// <para>
    /// This method must be called before any draw operations. Once a batch has
    /// begun, draw commands are collected until <see cref="End"/> is called.
    /// </para>
    /// <para>
    /// Only one batch can be active at a time. Calling <see cref="Begin"/> again
    /// without calling <see cref="End"/> first will throw an exception.
    /// </para>
    /// </remarks>
    void Begin(SortMode? sort = null, IBlendMode blendMode = null, Camera camera = null, IRenderTarget renderTarget = null);

    /// <summary>
    /// Ends the current batch and flushes all commands to the GPU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method finalizes the batch, flushes all collected commands to the
    /// GPU, and resets the batch state. After calling <see cref="End"/>, a new
    /// batch can be started with <see cref="Begin"/>.
    /// </para>
    /// <para>
    /// If no commands were added to the batch, <see cref="End"/> does nothing.
    /// </para>
    /// </remarks>
    void End();

    /// <summary>
    /// Flushes all pending commands to the GPU without ending the batch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method forces all collected commands to be rendered immediately
    /// while keeping the batch active. After flushing, the batch can continue
    /// accepting new commands.
    /// </para>
    /// <para>
    /// This is useful for forcing rendering at specific points in the frame
    /// or for debugging purposes.
    /// </para>
    /// </remarks>
    void Flush();

    /// <summary>
    /// Gets a value indicating whether a batch is currently active.
    /// </summary>
    /// <value><see langword="true"/> if a batch is active; otherwise, <see langword="false"/>.</value>
    bool IsDrawing { get; }

    /// <summary>
    /// Gets the number of draw calls issued in the current batch.
    /// </summary>
    /// <value>The total number of GPU draw calls.</value>
    int DrawCallCount { get; }

    /// <summary>
    /// Gets the number of vertices processed in the current batch.
    /// </summary>
    /// <value>The total vertex count.</value>
    int VertexCount { get; }

    /// <summary>
    /// Gets the number of commands in the current batch.
    /// </summary>
    /// <value>The total command count.</value>
    int CommandCount { get; }

    /// <summary>
    /// Gets the name of the batcher.
    /// </summary>
    /// <value>The batcher name (e.g., "SpriteBatcher", "PrimitiveBatcher").</value>
    string Name { get; }

    /// <summary>
    /// Gets the performance statistics for the current batch.
    /// </summary>
    /// <value>A <see cref="BatchStats"/> structure containing performance metrics.</value>
    /// <remarks>
    /// <para>
    /// The statistics include draw calls, vertices, triangles, commands,
    /// texture and blend mode switches, and CPU/GPU timing data.
    /// </para>
    /// <para>
    /// These metrics are useful for profiling and optimizing rendering performance.
    /// </para>
    /// </remarks>
    BatchStats Stats { get; }
}