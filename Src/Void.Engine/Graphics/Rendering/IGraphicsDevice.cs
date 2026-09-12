// ============================================================================
//  IGraphicsDevice.cs
// ============================================================================
//  Renderer-neutral GPU device contract implemented by renderer backends.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Defines the renderer-neutral GPU device used by VOID's rendering systems.
/// </summary>
/// <remarks>
/// <para>
/// A custom renderer exposes one active device through <see cref="IRendererBackend.Device"/>.
/// Resources created by a device belong to that device and should not be passed to a
/// different device. VOID may create buffers, textures, render targets, and shader
/// programs through this interface without knowing the backend API.
/// </para>
/// <para>
/// Unless a renderer explicitly provides stronger guarantees, device access should be
/// treated as render-thread only. Span data passed to upload methods is borrowed only
/// for the duration of the call and must not be retained by an implementation.
/// </para>
/// </remarks>
public interface IGraphicsDevice : IDisposable
{
    /// <summary>Gets the capabilities reported by this device.</summary>
    RendererCapabilities Capabilities { get; }

    /// <summary>Creates a GPU buffer owned by this device.</summary>
    /// <param name="description">The buffer description.</param>
    /// <returns>A renderer-owned buffer resource.</returns>
    /// <remarks>The caller owns the returned resource and is responsible for disposing it.</remarks>
    IGraphicsBuffer CreateBuffer(in BufferDescription description);

    /// <summary>Creates a GPU texture owned by this device.</summary>
    /// <param name="description">The texture description.</param>
    /// <param name="initialData">
    /// Optional tightly packed initial pixel data matching the texture dimensions and
    /// format. An empty span creates the resource without an initial pixel upload.
    /// </param>
    /// <returns>A renderer-owned texture resource.</returns>
    /// <remarks>
    /// The caller owns the returned resource and is responsible for disposing it.
    /// <paramref name="initialData"/> is valid only for the duration of this call.
    /// </remarks>
    IGraphicsTexture CreateTexture(
        in TextureDescription description,
        ReadOnlySpan<byte> initialData = default);

    /// <summary>Creates a GPU shader program owned by this device.</summary>
    /// <param name="description">The shader stages and representations to create.</param>
    /// <returns>A renderer-owned shader program.</returns>
    /// <remarks>
    /// The backend may reject shader languages, stages, or combinations it does not support.
    /// The caller owns the returned resource and is responsible for disposing it.
    /// </remarks>
    IGraphicsShaderProgram CreateShaderProgram(in ShaderProgramDescription description);

    /// <summary>Creates an off-screen render target owned by this device.</summary>
    /// <param name="description">The render-target description.</param>
    /// <returns>A renderer-owned render target.</returns>
    /// <remarks>
    /// The caller owns the returned resource and is responsible for disposing it.
    /// Unsupported formats, attachments, or sample counts should fail clearly.
    /// </remarks>
    IGraphicsRenderTarget CreateRenderTarget(in RenderTargetDescription description);

    /// <summary>Uploads unmanaged data into an existing GPU buffer.</summary>
    /// <typeparam name="T">The unmanaged element type contained in <paramref name="data"/>.</typeparam>
    /// <param name="buffer">A buffer created by this device.</param>
    /// <param name="data">The data to upload.</param>
    /// <param name="byteOffset">The non-negative destination offset in bytes.</param>
    /// <remarks>
    /// Implementations must consume or copy <paramref name="data"/> before returning.
    /// Updates must preserve buffer bytes outside the written range.
    /// </remarks>
    void UpdateBuffer<T>(
        IGraphicsBuffer buffer,
        ReadOnlySpan<T> data,
        int byteOffset = 0)
        where T : unmanaged;

    /// <summary>Uploads tightly packed pixel data into a texture region.</summary>
    /// <param name="texture">A texture created by this device.</param>
    /// <param name="x">The destination X coordinate in pixels.</param>
    /// <param name="y">The destination Y coordinate in pixels.</param>
    /// <param name="width">The positive update width in pixels.</param>
    /// <param name="height">The positive update height in pixels.</param>
    /// <param name="sourceFormat">The pixel format of <paramref name="data"/>.</param>
    /// <param name="data">Tightly packed pixel data for the requested region.</param>
    /// <remarks>
    /// Implementations must consume or copy <paramref name="data"/> before returning.
    /// The update must stay within the destination texture bounds. A backend may reject
    /// source formats or conversions it does not support.
    /// </remarks>
    void UpdateTexture(
        IGraphicsTexture texture,
        int x,
        int y,
        int width,
        int height,
        TextureFormat sourceFormat,
        ReadOnlySpan<byte> data);

    /// <summary>Selects the render target that receives subsequent clear and draw operations.</summary>
    /// <param name="target">
    /// A render target created by this device, or <see langword="null"/> to select the
    /// native window backbuffer.
    /// </param>
    void SetRenderTarget(IGraphicsRenderTarget target);

    /// <summary>Clears the currently selected render target or backbuffer.</summary>
    /// <param name="color">The clear color.</param>
    void Clear(Color color);

    /// <summary>Submits one draw command to the currently selected render target.</summary>
    /// <param name="command">The renderer-neutral draw command.</param>
    /// <remarks>
    /// All resources referenced by <paramref name="command"/> should have been created
    /// by this device. The command is borrowed only for this call and must not be retained.
    /// </remarks>
    void Draw(in RenderCommand command);

    /// <summary>
    /// Waits until previously submitted GPU work is complete.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for teardown, device recreation, or explicit
    /// backend synchronization points and should not be required for normal draws.
    /// </remarks>
    void WaitIdle();
}
