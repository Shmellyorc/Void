// ============================================================================
//  OpenGLRenderer.cs
// ============================================================================
//  Built-in OpenGL renderer wired to VOID's public renderer contracts.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;
using Void.Engine.Resources;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class OpenGLRenderer : RendererBackend
{
    private readonly GraphicsVersion _requestedVersion;
    private IRendererContext _context;
    private GLDevice _device;
    private IGraphicsShaderProgram _default2DShader;

    public OpenGLRenderer(GraphicsVersion requestedVersion)
    {
        _requestedVersion = requestedVersion;
    }

    public override string Name => "VOID OpenGL";
    public override GraphicsApi Api => GraphicsApi.OpenGL;
    public override GraphicsVersion Version => _requestedVersion;
    public override RendererWindowFlags RequiredWindowFlags => RendererWindowFlags.OpenGL;
    public override RendererCapabilities Capabilities => _device?.Capabilities ?? default;
    public override IGraphicsDevice Device => _device;
    public override IGraphicsShaderProgram Default2DShader => _default2DShader;

    protected override void OnInitialize(IRendererContext context)
    {
        _context = context;

        GL gl = GL.GetApi(context.GetProcAddress);
        _device = new GLDevice(gl, context.WindowSize, _requestedVersion);
        _default2DShader = CreateDefault2DShader(_device);

        if (!context.TrySetSwapInterval(context.Settings.VSync ? 1 : 0))
            throw new InvalidOperationException("Unable to configure the OpenGL swap interval.");
    }

    public override void BeginFrame(Color clearColor)
    {
        ObjectDisposedException.ThrowIf(!IsInitialized, this);
        _device.SetRenderTarget(null);
        _device.Clear(clearColor);
    }

    public override void EndFrame()
    {
        ObjectDisposedException.ThrowIf(!IsInitialized, this);
        _context.SwapBuffers();
    }

    public override void Resize(int width, int height)
    {
        if (!IsInitialized || width <= 0 || height <= 0)
            return;

        _device.Resize(width, height);
    }

    protected override void OnDispose()
    {
        _default2DShader?.Dispose();
        _default2DShader = null;

        _device?.Dispose();
        _device = null;
        _context = null;
    }

    private static IGraphicsShaderProgram CreateDefault2DShader(IGraphicsDevice device)
    {
        byte[] vertexSource = EmbeddedResources.ReadAllBytes("Data/Shaders/Default2D.vert.glsl");
        byte[] fragmentSource = EmbeddedResources.ReadAllBytes("Data/Shaders/Default2D.frag.glsl");

        ShaderSource[] sources =
        [
            new ShaderSource(ShaderStage.Vertex, ShaderLanguage.Glsl, vertexSource),
            new ShaderSource(ShaderStage.Fragment, ShaderLanguage.Glsl, fragmentSource)
        ];

        IGraphicsShaderProgram shader = device.CreateShaderProgram(new ShaderProgramDescription(sources));
        shader.SetUniform("uUseTexture", 0);
        return shader;
    }
}
