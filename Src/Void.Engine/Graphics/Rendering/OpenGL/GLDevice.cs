using Silk.NET.OpenGL;
using Void.Engine.Graphics;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLDevice : IGraphicsDevice
{
    private GL _gl;
    private GLRenderTarget _activeRenderTarget;
    private int _backbufferWidth;
    private int _backbufferHeight;
    private bool _disposed;

    public RendererCapabilities Capabilities { get; private set; }

    internal GLDevice(GL gl, Vect2 initialSize)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        Capabilities = default;
        Resize((int)initialSize.X, (int)initialSize.Y);
    }

    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        if (width <= 0 || height <= 0)
            return;

        _backbufferWidth = width;
        _backbufferHeight = height;

        if (_activeRenderTarget == null)
            _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void Clear(Color color)
    {
        ThrowIfDisposed();

        const float InvByte = 1f / 255f;
        _gl.ClearColor(color.R * InvByte, color.G * InvByte, color.B * InvByte, color.A * InvByte);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public IGraphicsBuffer CreateBuffer(in BufferDescription description)
    {
        ThrowIfDisposed();
        return new GLBuffer(_gl, description);
    }

    public IGraphicsTexture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default)
    {
        ThrowIfDisposed();
        return new GLTexture(_gl, description, initialData);
    }

    public IGraphicsShaderProgram CreateShaderProgram(in ShaderProgramDescription description)
    {
        ThrowIfDisposed();
        return new GLShaderProgram(_gl, description);
    }

    public IGraphicsRenderTarget CreateRenderTarget(in RenderTargetDescription description)
    {
        ThrowIfDisposed();
        return new GLRenderTarget(_gl, description);
    }

    public void UpdateBuffer<T>(IGraphicsBuffer buffer, ReadOnlySpan<T> data, int byteOffset = 0)
        where T : unmanaged
    {
        ThrowIfDisposed();

        if (buffer is not GLBuffer glBuffer)
            throw new ArgumentException("The buffer was not created by the VOID OpenGL backend.", nameof(buffer));

        glBuffer.Update(data, byteOffset);
    }

    public void UpdateTexture(
        IGraphicsTexture texture,
        int x,
        int y,
        int width,
        int height,
        TextureFormat sourceFormat,
        ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (texture is not GLTexture glTexture)
            throw new ArgumentException("The texture was not created by the VOID OpenGL backend.", nameof(texture));

        glTexture.Update(x, y, width, height, sourceFormat, data);
    }

    public void SetRenderTarget(IGraphicsRenderTarget target)
    {
        ThrowIfDisposed();

        if (target == null)
        {
            _activeRenderTarget = null;
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            if (_backbufferWidth > 0 && _backbufferHeight > 0)
                _gl.Viewport(0, 0, (uint)_backbufferWidth, (uint)_backbufferHeight);

            return;
        }

        if (target is not GLRenderTarget glTarget)
            throw new ArgumentException("The render target was not created by the VOID OpenGL backend.", nameof(target));

        glTarget.Bind();
        _activeRenderTarget = glTarget;
        _gl.Viewport(0, 0, (uint)glTarget.Description.Width, (uint)glTarget.Description.Height);
    }

    public unsafe void Draw(in RenderCommand command)
    {
        ThrowIfDisposed();

        if (command.VertexBuffer is not GLBuffer vertexBuffer || vertexBuffer.Description.Type != BufferType.Vertex)
            throw new ArgumentException("The draw command requires an OpenGL vertex buffer.", nameof(command));

        if (command.Shader is not GLShaderProgram shader)
            throw new InvalidOperationException("OpenGL draw submission requires a shader program created by this backend.");

        shader.Use();
        ApplyBlendMode(command.BlendMode);

        if (command.Texture != null)
        {
            if (command.Texture is not GLTexture texture)
                throw new ArgumentException("The draw command texture was not created by the VOID OpenGL backend.", nameof(command));

            // Unit zero is the primary draw texture. Shader.SetTexture may bind named
            // samplers explicitly when a shader uses multiple textures.
            texture.Bind(0);
        }

        uint vao = vertexBuffer.GetOrCreateVertexArray();
        _gl.BindVertexArray(vao);

        GLEnum primitive = ToPrimitiveType(command.PrimitiveType);

        if (command.IsIndexed)
        {
            if (command.IndexBuffer is not GLBuffer indexBuffer || indexBuffer.Description.Type != BufferType.Index)
                throw new ArgumentException("The indexed draw command requires an OpenGL index buffer.", nameof(command));

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer.Handle);

            int indexSize = indexBuffer.Description.IndexElementType == IndexElementType.UInt16 ? sizeof(ushort) : sizeof(uint);
            int byteOffset = checked(command.IndexStart * indexSize);

            _gl.DrawElements(
                primitive,
                checked((uint)command.IndexCount),
                ToIndexType(indexBuffer.Description.IndexElementType),
                (void*)(nint)byteOffset);
        }
        else
        {
            _gl.DrawArrays(
                primitive,
                command.VertexStart,
                checked((uint)command.VertexCount));
        }

        _gl.BindVertexArray(0);
    }

    public void WaitIdle()
    {
        ThrowIfDisposed();
        _gl.Finish();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activeRenderTarget = null;
        _gl?.Dispose();
        _gl = null;
        _disposed = true;
    }

    private void ApplyBlendMode(IBlendMode blendMode)
    {
        blendMode ??= global::Void.Engine.Graphics.BlendMode.Alpha;

        bool opaque =
            blendMode.ColorSrcFactor == BlendFactor.One &&
            blendMode.ColorDstFactor == BlendFactor.Zero &&
            blendMode.ColorEquation == BlendEquation.Add &&
            blendMode.AlphaSrcFactor == BlendFactor.One &&
            blendMode.AlphaDstFactor == BlendFactor.Zero &&
            blendMode.AlphaEquation == BlendEquation.Add;

        if (opaque)
        {
            _gl.Disable(EnableCap.Blend);
            return;
        }

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFuncSeparate(
            ToBlendFactor(blendMode.ColorSrcFactor),
            ToBlendFactor(blendMode.ColorDstFactor),
            ToBlendFactor(blendMode.AlphaSrcFactor),
            ToBlendFactor(blendMode.AlphaDstFactor));

        _gl.BlendEquationSeparate(
            ToBlendEquation(blendMode.ColorEquation),
            ToBlendEquation(blendMode.AlphaEquation));
    }

    private static GLEnum ToPrimitiveType(global::Void.Engine.Graphics.Rendering.PrimitiveType primitiveType)
        => primitiveType switch
        {
            global::Void.Engine.Graphics.Rendering.PrimitiveType.Points => GLEnum.Points,
            global::Void.Engine.Graphics.Rendering.PrimitiveType.Lines => GLEnum.Lines,
            global::Void.Engine.Graphics.Rendering.PrimitiveType.LineStrip => GLEnum.LineStrip,
            global::Void.Engine.Graphics.Rendering.PrimitiveType.Triangles => GLEnum.Triangles,
            global::Void.Engine.Graphics.Rendering.PrimitiveType.TriangleStrip => GLEnum.TriangleStrip,
            global::Void.Engine.Graphics.Rendering.PrimitiveType.TriangleFan => GLEnum.TriangleFan,
            _ => throw new ArgumentOutOfRangeException(nameof(primitiveType))
        };

    private static GLEnum ToIndexType(IndexElementType type)
        => type switch
        {
            IndexElementType.UInt16 => GLEnum.UnsignedShort,
            IndexElementType.UInt32 => GLEnum.UnsignedInt,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static BlendingFactor ToBlendFactor(BlendFactor factor)
        => factor switch
        {
            BlendFactor.Zero => BlendingFactor.Zero,
            BlendFactor.One => BlendingFactor.One,
            BlendFactor.SrcColor => BlendingFactor.SrcColor,
            BlendFactor.OneMinusSrcColor => BlendingFactor.OneMinusSrcColor,
            BlendFactor.DstColor => BlendingFactor.DstColor,
            BlendFactor.OneMinusDstColor => BlendingFactor.OneMinusDstColor,
            BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
            BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor))
        };

    private static BlendEquationModeEXT ToBlendEquation(BlendEquation equation)
        => equation switch
        {
            BlendEquation.Add => BlendEquationModeEXT.FuncAdd,
            BlendEquation.Subtract => BlendEquationModeEXT.FuncSubtract,
            BlendEquation.ReverseSubtract => BlendEquationModeEXT.FuncReverseSubtract,
            BlendEquation.Min => BlendEquationModeEXT.Min,
            BlendEquation.Max => BlendEquationModeEXT.Max,
            _ => throw new ArgumentOutOfRangeException(nameof(equation))
        };

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
