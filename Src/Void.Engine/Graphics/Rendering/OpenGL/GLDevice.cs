// ============================================================================
//  GLDevice.cs
// ============================================================================
//  OpenGL implementation of VOID's renderer-neutral graphics device contract.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;
using Void.Engine.Graphics;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLDevice : IGraphicsDevice
{
    private GL _gl;
    private readonly GLStateCache _state;
    private GLRenderTarget _activeRenderTarget;
    private int _backbufferWidth;
    private int _backbufferHeight;

    private bool _hasClearColor;
    private Color _clearColor;

    private bool? _blendEnabled;
    private bool _hasBlendFunction;
    private BlendFactor _colorSrcFactor;
    private BlendFactor _colorDstFactor;
    private BlendFactor _alphaSrcFactor;
    private BlendFactor _alphaDstFactor;

    private bool _hasBlendEquation;
    private BlendEquation _colorEquation;
    private BlendEquation _alphaEquation;

    private bool _disposed;

    public RendererCapabilities Capabilities { get; private set; }

    internal GLDevice(GL gl, Vect2 initialSize, GraphicsVersion version)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        int maxTextureSize = QueryPositiveInteger(
            GLEnum.MaxTextureSize,
            "maximum texture size");
        int maxTextureUnits = QueryPositiveInteger(
            GLEnum.MaxCombinedTextureImageUnits,
            "combined texture image units");

        _state = new GLStateCache(_gl, maxTextureUnits);

        Capabilities = new RendererCapabilities(
            maxTextureSize,
            maxTextureUnits,
            maxRenderTargets: 1,
            maxSamples: 1,
            supportsInstancing: false,
            supportsGeometryShaders: version >= new GraphicsVersion(3, 2),
            supportsComputeShaders: false,
            supportsDebugOutput: false);

        Resize((int)initialSize.X, (int)initialSize.Y);
    }

    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        if (width <= 0 || height <= 0)
            return;

        bool sizeChanged = _backbufferWidth != width || _backbufferHeight != height;

        _backbufferWidth = width;
        _backbufferHeight = height;

        if (sizeChanged && _activeRenderTarget == null)
            _gl.Viewport(0, 0, (uint)width, (uint)height);
    }

    public void Clear(Color color)
    {
        ThrowIfDisposed();

        if (!_hasClearColor || !SameColor(_clearColor, color))
        {
            const float InvByte = 1f / 255f;
            _gl.ClearColor(color.R * InvByte, color.G * InvByte, color.B * InvByte, color.A * InvByte);
            _clearColor = color;
            _hasClearColor = true;
        }

        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public IGraphicsBuffer CreateBuffer(in BufferDescription description)
    {
        ThrowIfDisposed();
        return new GLBuffer(_gl, _state, description);
    }

    public IGraphicsTexture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default)
    {
        ThrowIfDisposed();
        return new GLTexture(_gl, _state, description, initialData);
    }

    public IGraphicsShaderProgram CreateShaderProgram(in ShaderProgramDescription description)
    {
        ThrowIfDisposed();
        return new GLShaderProgram(_gl, _state, description);
    }

    public IGraphicsRenderTarget CreateRenderTarget(in RenderTargetDescription description)
    {
        ThrowIfDisposed();

        GLRenderTarget target = new(_gl, _state, description);

        // GLRenderTarget creation temporarily binds its framebuffer and restores
        // framebuffer zero. Restore the device's logical target so creating an
        // off-screen target during rendering cannot desynchronize GLDevice state.
        RestoreActiveRenderTarget();

        return target;
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
            if (_activeRenderTarget == null)
                return;

            _activeRenderTarget = null;
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            if (_backbufferWidth > 0 && _backbufferHeight > 0)
                _gl.Viewport(0, 0, (uint)_backbufferWidth, (uint)_backbufferHeight);

            return;
        }

        if (target is not GLRenderTarget glTarget)
            throw new ArgumentException("The render target was not created by the VOID OpenGL backend.", nameof(target));

        if (ReferenceEquals(_activeRenderTarget, glTarget))
            return;

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
        _state.BindVertexArray(vao);

        GLEnum primitive = ToPrimitiveType(command.PrimitiveType);

        if (command.IsIndexed)
        {
            if (command.IndexBuffer is not GLBuffer indexBuffer || indexBuffer.Description.Type != BufferType.Index)
                throw new ArgumentException("The indexed draw command requires an OpenGL index buffer.", nameof(command));

            _state.BindElementArrayBuffer(indexBuffer.Handle);

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

        BlendFactor colorSrc = blendMode.ColorSrcFactor;
        BlendFactor colorDst = blendMode.ColorDstFactor;
        BlendEquation colorEquation = blendMode.ColorEquation;
        BlendFactor alphaSrc = blendMode.AlphaSrcFactor;
        BlendFactor alphaDst = blendMode.AlphaDstFactor;
        BlendEquation alphaEquation = blendMode.AlphaEquation;

        bool opaque =
            colorSrc == BlendFactor.One &&
            colorDst == BlendFactor.Zero &&
            colorEquation == BlendEquation.Add &&
            alphaSrc == BlendFactor.One &&
            alphaDst == BlendFactor.Zero &&
            alphaEquation == BlendEquation.Add;

        if (opaque)
        {
            if (_blendEnabled != false)
            {
                _gl.Disable(EnableCap.Blend);
                _blendEnabled = false;
            }

            return;
        }

        if (_blendEnabled != true)
        {
            _gl.Enable(EnableCap.Blend);
            _blendEnabled = true;
        }

        if (!_hasBlendFunction ||
            _colorSrcFactor != colorSrc ||
            _colorDstFactor != colorDst ||
            _alphaSrcFactor != alphaSrc ||
            _alphaDstFactor != alphaDst)
        {
            _gl.BlendFuncSeparate(
                ToBlendFactor(colorSrc),
                ToBlendFactor(colorDst),
                ToBlendFactor(alphaSrc),
                ToBlendFactor(alphaDst));

            _colorSrcFactor = colorSrc;
            _colorDstFactor = colorDst;
            _alphaSrcFactor = alphaSrc;
            _alphaDstFactor = alphaDst;
            _hasBlendFunction = true;
        }

        if (!_hasBlendEquation ||
            _colorEquation != colorEquation ||
            _alphaEquation != alphaEquation)
        {
            _gl.BlendEquationSeparate(
                ToBlendEquation(colorEquation),
                ToBlendEquation(alphaEquation));

            _colorEquation = colorEquation;
            _alphaEquation = alphaEquation;
            _hasBlendEquation = true;
        }
    }

    private void RestoreActiveRenderTarget()
    {
        if (_activeRenderTarget != null)
        {
            _activeRenderTarget.Bind();
            _gl.Viewport(
                0,
                0,
                (uint)_activeRenderTarget.Description.Width,
                (uint)_activeRenderTarget.Description.Height);
            return;
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        if (_backbufferWidth > 0 && _backbufferHeight > 0)
            _gl.Viewport(0, 0, (uint)_backbufferWidth, (uint)_backbufferHeight);
    }

    private static bool SameColor(in Color left, in Color right)
        => left.R == right.R &&
           left.G == right.G &&
           left.B == right.B &&
           left.A == right.A;

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

    private int QueryPositiveInteger(GLEnum parameter, string capabilityName)
    {
        _gl.GetInteger(parameter, out int value);
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"OpenGL reported an invalid {capabilityName} value: {value}.");
        }

        return value;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
