// ============================================================================
//  IRenderTarget.cs
// ============================================================================
//  Defines the renderer-neutral contract for drawing into a render surface.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Represents a renderer-neutral destination that can receive vertex-buffer
/// submissions and optionally expose its rendered color texture.
/// </summary>
/// <remarks>
/// <para>
/// VOID's built-in off-screen targets are obtained from <see cref="RenderTarget"/>.
/// Custom render-target implementations participate in the same batching path by
/// exposing the renderer-neutral graphics device and graphics render target that
/// back the surface through <see cref="GraphicsDevice"/> and
/// <see cref="GraphicsRenderTarget"/>.
/// </para>
/// <para>
/// The graphics resources returned by those properties must belong to the same
/// active renderer backend. This lets VOID's built-in vertex-buffer path submit
/// to third-party render surfaces without depending on a concrete VOID target type.
/// </para>
/// <para>
/// Camera transforms used by VOID's built-in rendering path are carried through
/// <see cref="BatchRenderState"/>. <see cref="SetView"/> remains part of the target
/// contract so an implementation can react to camera changes when it needs to.
/// </para>
/// <code>
/// IRenderTarget target = RenderTarget.Get(320, 180);
/// target.Clear(Color.Transparent);
/// // Submit drawing commands to target.
/// target.Display();
/// Texture texture = target.GetTexture();
/// RenderTarget.Return(target);
/// </code>
/// </remarks>
public interface IRenderTarget
{
    /// <summary>
    /// Gets the renderer-neutral graphics device that owns this render target.
    /// </summary>
    /// <remarks>
    /// The returned device must be the active device used to create
    /// <see cref="GraphicsRenderTarget"/> and any graphics resources submitted to it.
    /// </remarks>
    IGraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// Gets the renderer-owned render-target resource used for draw submission.
    /// </summary>
    /// <remarks>
    /// The resource must belong to <see cref="GraphicsDevice"/> and remain valid
    /// while the target is available for drawing.
    /// </remarks>
    IGraphicsRenderTarget GraphicsRenderTarget { get; }

    /// <summary>
    /// Clears the render target to the specified color.
    /// </summary>
    /// <param name="color">The color written across the target.</param>
    void Clear(Color color);

    /// <summary>
    /// Draws a range of vertices from a vertex buffer.
    /// </summary>
    /// <param name="buffer">The vertex buffer that owns the geometry.</param>
    /// <param name="vertexStart">The zero-based first vertex to submit.</param>
    /// <param name="vertexCount">The number of vertices to submit.</param>
    /// <param name="states">The renderer-neutral state used for the submission.</param>
    /// <remarks>
    /// Implementations using VOID's normal batching path can delegate the submission
    /// to <paramref name="buffer"/>. The buffer uses this target's public graphics
    /// resources rather than requiring a specific concrete target implementation.
    /// </remarks>
    void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, BatchRenderState states);

    /// <summary>
    /// Completes target-specific work required after drawing.
    /// </summary>
    /// <remarks>
    /// VOID's built-in texture render target exposes rendered pixels immediately,
    /// so this operation is a no-op for that implementation. Other targets may use
    /// it to resolve, present, or otherwise finalize their rendered content.
    /// </remarks>
    void Display();

    /// <summary>
    /// Notifies the render target that the active camera has changed.
    /// </summary>
    /// <param name="camera">The camera selected for rendering.</param>
    /// <remarks>
    /// VOID's built-in target does not store a separate camera view because its
    /// view-projection matrix is supplied through <see cref="BatchRenderState"/>.
    /// </remarks>
    void SetView(Camera camera);

    /// <summary>
    /// Gets the texture containing this target's rendered color output, when available.
    /// </summary>
    /// <returns>
    /// The target's color texture, or <see langword="null"/> when the implementation
    /// does not expose one.
    /// </returns>
    /// <remarks>
    /// The returned texture remains owned by the render target. Do not dispose it
    /// independently unless the implementation explicitly documents different ownership.
    /// </remarks>
    Texture GetTexture();

    /// <summary>
    /// Gets the render target size in pixels.
    /// </summary>
    Vect2 Size { get; }

    /// <summary>
    /// Gets the render target width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the render target height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets whether the target was created for sRGB color output.
    /// </summary>
    bool Srgb { get; }
}
