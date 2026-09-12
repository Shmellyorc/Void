// ============================================================================
//  IRendererBackend.cs
// ============================================================================
//  Public extension contract for pluggable renderer backends.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Defines a pluggable renderer backend used by VOID.
/// </summary>
/// <remarks>
/// <para>
/// The built-in OpenGL renderer is one implementation of this contract. A plugin may
/// provide Vulkan, Direct3D, Metal, or another graphics API without modifying VOID.
/// <see cref="RequiredWindowFlags"/> and <see cref="Version"/> are read before the
/// native window is created. VOID then calls <see cref="Initialize"/> once with a
/// platform context for that window.
/// </para>
/// <para>
/// After successful initialization, <see cref="Device"/> and <see cref="Default2DShader"/>
/// must be ready for VOID's built-in rendering path. During each frame VOID calls
/// <see cref="BeginFrame"/>, performs off-screen rendering and final presentation work,
/// then calls <see cref="EndFrame"/>. <see cref="Resize"/> is called when the native
/// backbuffer size changes.
/// </para>
/// <para>
/// Renderer instances are expected to own their backend-specific device, swapchain or
/// presentation state, default 2D shader, and related GPU resources. Dispose releases
/// those resources and makes the backend unusable.
/// </para>
/// <code>
/// GameSettings.Instance
///     .SetRenderer(() =&gt; new MyRenderer());
/// </code>
/// </remarks>
public interface IRendererBackend : IDisposable
{
    /// <summary>Gets a human-readable renderer name used for diagnostics.</summary>
    string Name { get; }

    /// <summary>Gets the graphics API implemented by this backend.</summary>
    GraphicsApi Api { get; }

    /// <summary>
    /// Gets the graphics API version requested or implemented by this backend.
    /// </summary>
    GraphicsVersion Version { get; }

    /// <summary>
    /// Gets the native window features that must be enabled before initialization.
    /// </summary>
    RendererWindowFlags RequiredWindowFlags { get; }

    /// <summary>Gets the capabilities reported by the initialized backend.</summary>
    RendererCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the renderer-neutral graphics device owned by this backend.
    /// </summary>
    /// <remarks>
    /// This must be non-null and usable after <see cref="Initialize"/> succeeds and
    /// until the backend is disposed.
    /// </remarks>
    IGraphicsDevice Device { get; }

    /// <summary>
    /// Gets the renderer-owned shader program used by VOID's built-in 2D pipeline
    /// when game code has not supplied a custom shader.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The program must be valid after <see cref="Initialize"/> succeeds. It consumes
    /// <see cref="Vertex.Layout"/>: location 0 is position, location 1 is normalized
    /// vertex color, and location 2 is the texture coordinate. Texture coordinates used
    /// by VOID's built-in 2D path are expressed in texture-pixel units.
    /// </para>
    /// <para>
    /// Before drawing, VOID supplies <c>uViewProjection</c> as a 4x4 matrix,
    /// <c>uUseTexture</c> as zero or one, <c>uTextureSize</c> as the texture width and
    /// height when a texture is present, and <c>uTexture</c> as the primary texture.
    /// The backend may implement those bindings however its API requires internally,
    /// but the resulting 2D shader must preserve these semantics.
    /// </para>
    /// </remarks>
    IGraphicsShaderProgram Default2DShader { get; }

    /// <summary>Gets whether this backend has completed initialization.</summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the renderer after the native window has been created with
    /// <see cref="RequiredWindowFlags"/>.
    /// </summary>
    /// <param name="context">
    /// Window and platform services owned by VOID. The backend may retain this context
    /// for its lifetime but must not take ownership of the native window or borrowed handles.
    /// </param>
    /// <remarks>
    /// Implementations should create the graphics device, presentation resources,
    /// capability information, and <see cref="Default2DShader"/> before returning.
    /// Initialization is expected to occur once per backend instance.
    /// </remarks>
    void Initialize(IRendererContext context);

    /// <summary>Begins rendering a native window frame.</summary>
    /// <param name="clearColor">The color used to clear the native backbuffer.</param>
    /// <remarks>
    /// This is called before VOID renders its off-screen game surface and performs the
    /// final presentation draw. APIs with swapchains may acquire the current image here.
    /// </remarks>
    void BeginFrame(Color clearColor);

    /// <summary>Ends and presents the current native window frame.</summary>
    /// <remarks>
    /// APIs that require an explicit present or buffer swap should perform it here.
    /// </remarks>
    void EndFrame();

    /// <summary>Notifies the renderer that the native backbuffer size changed.</summary>
    /// <param name="width">The new positive backbuffer width in pixels.</param>
    /// <param name="height">The new positive backbuffer height in pixels.</param>
    /// <remarks>
    /// Swapchain-based backends may recreate presentation resources here. The engine's
    /// internal game render size is independent from the native window backbuffer size.
    /// </remarks>
    void Resize(int width, int height);
}
