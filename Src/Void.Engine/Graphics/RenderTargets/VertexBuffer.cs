using Void.Engine.Graphics.Rendering;
using RenderPrimitiveType = Void.Engine.Graphics.Rendering.PrimitiveType;
using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>Renderer-neutral vertex buffer used by VOID's batching layer.</summary>
internal sealed class VertexBuffer : IVertexBuffer, IGraphicsBufferSource
{
    private readonly RenderVertex[] _vertices;
    private readonly int _capacity;

    private IGraphicsDevice _graphicsDevice;
    private IGraphicsBuffer _graphicsBuffer;
    private RenderPrimitiveType _primitiveType = RenderPrimitiveType.Triangles;
    private bool _disposed;

    public RenderPrimitiveType PrimitiveType
    {
        get => _primitiveType;
        set
        {
            ThrowIfDisposed();
            _primitiveType = value;
        }
    }

    public VertexBuffer(int vertexCount)
    {
        if (vertexCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));

        _capacity = vertexCount;
        _vertices = new RenderVertex[vertexCount];
    }

    public void Update(ReadOnlySpan<RenderVertex> vertices, uint vertexCount, uint offset)
    {
        ThrowIfDisposed();

        if (vertexCount > (uint)vertices.Length)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if ((ulong)offset + vertexCount > (ulong)_capacity)
            throw new ArgumentOutOfRangeException(nameof(offset), "Vertex update exceeds the buffer capacity.");

        int count = checked((int)vertexCount);
        int destinationOffset = checked((int)offset);
        ReadOnlySpan<RenderVertex> source = vertices[..count];
        source.CopyTo(_vertices.AsSpan(destinationOffset, count));

        if (_graphicsBuffer != null)
        {
            if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice) ||
                !ReferenceEquals(activeDevice, _graphicsDevice))
            {
                ReleaseGraphicsBuffer();
            }
            else
            {
                int byteOffset = checked(destinationOffset * RenderVertex.Layout.Stride);
                _graphicsDevice.UpdateBuffer(_graphicsBuffer, source, byteOffset);
            }
        }
    }

    public bool TryGetGraphicsBuffer(out IGraphicsBuffer buffer)
    {
        ThrowIfDisposed();

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice))
        {
            buffer = null;
            return false;
        }

        if (_graphicsBuffer != null && !ReferenceEquals(activeDevice, _graphicsDevice))
            ReleaseGraphicsBuffer();

        if (_graphicsBuffer == null)
        {
            var description = new BufferDescription(
                checked(_capacity * RenderVertex.Layout.Stride),
                BufferType.Vertex,
                BufferUsage.Stream,
                RenderVertex.Layout);

            _graphicsDevice = activeDevice;
            _graphicsBuffer = activeDevice.CreateBuffer(description);
            activeDevice.UpdateBuffer(_graphicsBuffer, _vertices, 0);
        }

        buffer = _graphicsBuffer;
        return true;
    }

    public void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, BatchRenderState states)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(states);

        if (target is not TextureRenderTarget textureTarget)
            throw new InvalidOperationException($"Unsupported render target type: {target.GetType().Name}");

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice device))
            throw new InvalidOperationException("No active graphics device is available.");

        if (!ReferenceEquals(device, textureTarget.GraphicsDevice))
            throw new InvalidOperationException("Vertex buffer and render target belong to different graphics devices.");

        if (!TryGetGraphicsBuffer(out IGraphicsBuffer vertexBuffer))
            throw new InvalidOperationException("Unable to resolve the renderer-owned vertex buffer.");

        if (!Renderer2DState.TryPrepareShader(states, out IGraphicsShaderProgram shader, out IGraphicsTexture texture))
        {
            if (states.Shader != null)
            {
                throw new InvalidOperationException(
                    "The selected custom shader did not expose a valid renderer-neutral ShaderProgram. " +
                    "Custom IShader implementations should return a ShaderProgram from IShader.Program.");
            }

            throw new InvalidOperationException("The active renderer did not provide a usable 2D shader.");
        }

        device.SetRenderTarget(textureTarget.GraphicsRenderTarget);

        var command = new RenderCommand(
            vertexBuffer,
            _primitiveType,
            checked((int)vertexStart),
            checked((int)vertexCount),
            states.BlendMode,
            texture,
            shader);

        device.Draw(command);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ReleaseGraphicsBuffer();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ReleaseGraphicsBuffer()
    {
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
        _graphicsDevice = null;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
