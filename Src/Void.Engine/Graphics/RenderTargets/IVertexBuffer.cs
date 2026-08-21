// ============================================================================
//  IVertexBuffer.cs
// ============================================================================
//  Defines the contract for vertex buffer management, including updating
//  vertex data, setting primitive types, and drawing to render targets.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Defines the contract for vertex buffer management and rendering.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IVertexBuffer"/> interface provides a unified abstraction
/// for vertex data storage and rendering. It supports:
/// <list type="bullet">
///   <item><description>Updating vertex data from spans</description></item>
///   <item><description>Setting primitive types (points, lines, triangles)</description></item>
///   <item><description>Drawing to render targets with render states</description></item>
///   <item><description>Resource disposal</description></item>
/// </list>
/// </para>
/// <para>
/// Vertex buffers are used by batchers to efficiently render large numbers
/// of vertices with minimal draw calls.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a vertex buffer
/// var buffer = new VertexBuffer(1024);
/// buffer.PrimitiveType = SFPrimitiveType.Triangles;
/// 
/// // Update with vertex data
/// var vertices = new SFVertex[] { ... };
/// buffer.Update(vertices, vertices.Length, 0);
/// 
/// // Draw to render target
/// buffer.Draw(renderTarget, 0, vertices.Length, renderStates);
/// 
/// // Clean up
/// buffer.Dispose();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations are not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public interface IVertexBuffer
{
    /// <summary>
    /// Gets or sets the primitive type used for rendering.
    /// </summary>
    /// <value>The primitive type (points, lines, triangles, etc.).</value>
    /// <remarks>
    /// <para>
    /// The primitive type determines how vertices are interpreted:
    /// <list type="bullet">
    ///   <item><description><see cref="SFPrimitiveType.Points"/> - Each vertex is a point</description></item>
    ///   <item><description><see cref="SFPrimitiveType.Lines"/> - Vertices form line segments</description></item>
    ///   <item><description><see cref="SFPrimitiveType.LineStrip"/> - Vertices form a continuous line</description></item>
    ///   <item><description><see cref="SFPrimitiveType.Triangles"/> - Vertices form triangles</description></item>
    ///   <item><description><see cref="SFPrimitiveType.TriangleStrip"/> - Vertices form a triangle strip</description></item>
    ///   <item><description><see cref="SFPrimitiveType.TriangleFan"/> - Vertices form a triangle fan</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    SFPrimitiveType PrimitiveType { get; set; }

    /// <summary>
    /// Updates the vertex buffer with new vertex data.
    /// </summary>
    /// <param name="vertices">The vertex data to upload.</param>
    /// <param name="vertexCount">The number of vertices to upload.</param>
    /// <param name="offset">The starting offset in the buffer.</param>
    /// <remarks>
    /// <para>
    /// This method uploads vertex data to the GPU. The buffer must have
    /// sufficient capacity to store the data at the specified offset.
    /// </para>
    /// <para>
    /// The <paramref name="offset"/> parameter allows partial updates to
    /// the buffer without re-uploading all data.
    /// </para>
    /// </remarks>
    void Update(ReadOnlySpan<SFVertex> vertices, uint vertexCount, uint offset);

    /// <summary>
    /// Draws the vertex buffer to the specified render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    /// <param name="vertexStart">The starting vertex index in the buffer.</param>
    /// <param name="vertexCount">The number of vertices to draw.</param>
    /// <param name="states">The render states (blend mode, transform, shader, texture) to apply.</param>
    /// <remarks>
    /// <para>
    /// This method renders the specified range of vertices to the render target
    /// using the current primitive type and render states.
    /// </para>
    /// <para>
    /// The render states control how the vertices are rendered, including
    /// blending, transformation, shaders, and textures.
    /// </para>
    /// </remarks>
    void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, SFRenderStates states);

    /// <summary>
    /// Disposes the vertex buffer and releases all GPU resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method releases the GPU memory used by the vertex buffer.
    /// After disposal, the buffer should not be used.
    /// </para>
    /// <para>
    /// It is recommended to call this method when the buffer is no longer needed
    /// to free GPU resources.
    /// </para>
    /// </remarks>
    void Dispose();
}