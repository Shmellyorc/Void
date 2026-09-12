using RenderPrimitiveType = Void.Engine.Graphics.Rendering.PrimitiveType;
using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

// ============================================================================
//  IVertexBuffer.cs
// ============================================================================
//  Defines the renderer-neutral contract for vertex storage and submission.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Represents renderer-neutral vertex storage that can be updated and submitted
/// to a render target.
/// </summary>
/// <remarks>
/// <para>
/// VOID's built-in batchers use an internal implementation of this interface.
/// Custom batchers can provide another implementation through the protected
/// <see cref="BaseBatcher.VertexBuffer"/> extension point.
/// </para>
/// <para>
/// Buffer implementations own the details of GPU allocation, deferred uploads,
/// and device binding. VOID's built-in buffer submits through the renderer-neutral
/// resources exposed by <see cref="IRenderTarget.GraphicsDevice"/> and
/// <see cref="IRenderTarget.GraphicsRenderTarget"/>, so custom render-target
/// implementations do not need to inherit from a VOID concrete target type.
/// </para>
/// <code>
/// buffer.PrimitiveType = RenderPrimitiveType.Triangles;
/// buffer.Update(vertices, (uint)vertices.Length, 0);
/// buffer.Draw(target, 0, (uint)vertices.Length, states);
/// </code>
/// </remarks>
public interface IVertexBuffer
{
    /// <summary>
    /// Gets or sets how submitted vertices are interpreted.
    /// </summary>
    RenderPrimitiveType PrimitiveType { get; set; }

    /// <summary>
    /// Updates a range of vertices in the buffer.
    /// </summary>
    /// <param name="vertices">The source vertices.</param>
    /// <param name="vertexCount">
    /// The number of vertices to copy from <paramref name="vertices"/>.
    /// </param>
    /// <param name="offset">The destination offset measured in vertices.</param>
    /// <remarks>
    /// Implementations may upload immediately or stage the data until a graphics
    /// device becomes available. The requested range must fit both the supplied
    /// span and the buffer's capacity.
    /// </remarks>
    void Update(ReadOnlySpan<RenderVertex> vertices, uint vertexCount, uint offset);

    /// <summary>
    /// Submits a range of vertices to a render target.
    /// </summary>
    /// <param name="target">The render target that receives the submission.</param>
    /// <param name="vertexStart">The zero-based first vertex to draw.</param>
    /// <param name="vertexCount">The number of vertices to draw.</param>
    /// <param name="states">The renderer-neutral state used by the draw.</param>
    /// <remarks>
    /// The requested vertex range must fit the buffer. The target's graphics device
    /// and render-target resource must be compatible with the resources owned by the
    /// buffer implementation.
    /// </remarks>
    void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, BatchRenderState states);

    /// <summary>
    /// Releases resources owned by the vertex buffer.
    /// </summary>
    void Dispose();
}
