namespace Void.Engine.Graphics.RenderTargets;

internal sealed class VertexBuffer : IVertexBuffer
{
    private readonly SFVertexBuffer _buffer;
    private SFPrimitiveType _primitiveType;
    private bool _disposed;

    internal SFVertexBuffer Buffer => _buffer;

    public SFPrimitiveType PrimitiveType
    {
        get => _primitiveType;
        set
        {
            _primitiveType = value;
            _buffer.PrimitiveType = value;
        }
    }

    public VertexBuffer(int vertexCount)
    {
        _buffer = new SFVertexBuffer(
            (uint)vertexCount,
            SFPrimitiveType.Triangles,
            SFUsageSpecifier.Stream
        );
        _primitiveType = SFPrimitiveType.Triangles;
    }

    public void Update(ReadOnlySpan<SFVertex> vertices, uint vertexCount, uint offset)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexBuffer));
        _buffer.Update(vertices.ToArray(), vertexCount, offset);
    }

    public void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, SFRenderStates states)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexBuffer));

        // Try to get the underlying SFML render target
        if (target is TextureRenderTarget textureTarget)
        {
            _buffer.Draw(textureTarget.RenderTexture, vertexStart, vertexCount, states);
        }
        // else if (target is WindowRenderTarget windowTarget)
        // {
        //     _buffer.Draw(windowTarget.RenderTexture, vertexStart, vertexCount, states);
        // }
        else
        {
            throw new InvalidOperationException($"Unsupported render target type: {target.GetType().Name}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _buffer?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}