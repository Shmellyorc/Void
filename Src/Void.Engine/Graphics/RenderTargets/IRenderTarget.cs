// ============================================================================
//  IRenderTarget.cs
// ============================================================================
//  Defines the contract for render targets that can be drawn to, cleared,
//  and displayed. Supports custom views, vertex buffer rendering, and
//  texture retrieval for post-processing and atlas management.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Defines the contract for render targets that can be drawn to, cleared,
/// and displayed.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IRenderTarget"/> interface provides a unified abstraction
/// for renderable surfaces including the main window, render textures, and
/// custom render targets. It supports:
/// <list type="bullet">
///   <item><description>Clearing with a specified color</description></item>
///   <item><description>Drawing vertex buffers with render states</description></item>
///   <item><description>Displaying the rendered content</description></item>
///   <item><description>View/camera management</description></item>
///   <item><description>Texture retrieval for post-processing and atlas management</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the main render target (game window)
/// IRenderTarget target = Game.Instance.Window;
/// 
/// // Clear and draw
/// target.Clear(Color.CornflowerBlue);
/// target.Draw(vertexBuffer, 0, 100, renderStates);
/// target.Display();
/// 
/// // Create a render texture for post-processing
/// var renderTexture = RenderTarget.Get(1920, 1080);
/// renderTexture.Clear(Color.Transparent);
/// // ... draw to it ...
/// var texture = renderTexture.GetTexture();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations are not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public interface IRenderTarget
{
    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear the render target with.</param>
    /// <remarks>
    /// This method fills the entire render target with the specified color,
    /// clearing any previously rendered content.
    /// </remarks>
    void Clear(Color color);

    /// <summary>
    /// Draws a vertex buffer to the render target.
    /// </summary>
    /// <param name="buffer">The vertex buffer to draw.</param>
    /// <param name="vertexStart">The starting vertex index in the buffer.</param>
    /// <param name="vertexCount">The number of vertices to draw.</param>
    /// <param name="states">The render states (blend mode, transform, shader, texture) to apply.</param>
    /// <remarks>
    /// <para>
    /// This method renders the specified range of vertices from the vertex buffer
    /// using the provided render states. The buffer's primitive type determines
    /// how the vertices are rendered (points, lines, triangles, etc.).
    /// </para>
    /// </remarks>
    void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, SFRenderStates states);

    /// <summary>
    /// Displays the rendered content to the target surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For render textures, this method updates the underlying texture with
    /// the rendered content. For the main window, it presents the frame to
    /// the display.
    /// </para>
    /// <para>
    /// This method must be called after drawing operations to make the
    /// rendered content visible.
    /// </para>
    /// </remarks>
    void Display();

    /// <summary>
    /// Sets the view (camera) for the render target.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    /// <remarks>
    /// <para>
    /// The view defines the coordinate system and projection for rendering.
    /// Setting a camera applies its view and projection matrices to the
    /// render target.
    /// </para>
    /// <para>
    /// This is used for 2D camera systems with scrolling, zooming, and rotation.
    /// </para>
    /// </remarks>
    void SetView(Camera camera);

    /// <summary>
    /// Gets the texture associated with this render target.
    /// </summary>
    /// <returns>The render target texture, or <see langword="null"/> if the target does not support texture retrieval.</returns>
    /// <remarks>
    /// <para>
    /// For render textures, this returns the underlying texture that contains
    /// the rendered content. For the main window, this may return <see langword="null"/>
    /// or the back buffer texture.
    /// </para>
    /// <para>
    /// The returned texture can be used for post-processing, effects, or
    /// as input to other rendering operations.
    /// </para>
    /// </remarks>
    Texture GetTexture();

    /// <summary>
    /// Gets the size of the render target in pixels.
    /// </summary>
    /// <value>A vector containing the width and height of the render target.</value>
    Vect2 Size { get; }

    /// <summary>
    /// Gets the width of the render target in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the render target in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets a value indicating whether the render target uses sRGB color space.
    /// </summary>
    /// <value><see langword="true"/> if the render target uses sRGB; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// sRGB render targets apply gamma correction to rendered content,
    /// providing more accurate color representation.
    /// </remarks>
    bool Srgb { get; }
}