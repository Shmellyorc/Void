namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Public extension point for pluggable VOID renderers. The built-in OpenGL
/// renderer is one implementation; Vulkan, Direct3D, Metal, or custom renderers
/// can implement the same contract without modifying VOID source.
/// </summary>
public interface IRendererBackend : IDisposable
{
    string Name { get; }
    GraphicsApi Api { get; }
    GraphicsVersion Version { get; }
    RendererWindowFlags RequiredWindowFlags { get; }
    RendererCapabilities Capabilities { get; }
    IGraphicsDevice Device { get; }

    /// <summary>
    /// Renderer-owned program used by VOID's built-in 2D batching path when the
    /// game has not supplied a custom shader. Each backend provides its native
    /// equivalent; batchers never depend on GLSL/HLSL/SPIR-V directly.
    /// </summary>
    IGraphicsShaderProgram Default2DShader { get; }

    bool IsInitialized { get; }

    /// <summary>
    /// Called after the window has been created with this renderer's required flags.
    /// </summary>
    void Initialize(IRendererContext context);

    void BeginFrame(Color clearColor);
    void EndFrame();
    void Resize(int width, int height);
}
