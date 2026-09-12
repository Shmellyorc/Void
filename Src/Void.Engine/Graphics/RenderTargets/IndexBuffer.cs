// ============================================================================
//  IndexBuffer.cs
// ============================================================================
//  Internal renderer-neutral index buffer used for batched quad submission.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.RenderTargets;

internal sealed class IndexBuffer : IDisposable
{
    private const int IndicesPerQuad = 6;
    private const int VerticesPerQuad = 4;

    private readonly uint[] _indices;

    private IGraphicsDevice _graphicsDevice;
    private IGraphicsBuffer _graphicsBuffer;
    private bool _disposed;

    public int IndexCount => _indices.Length;

    public IndexBuffer(int quadCapacity)
    {
        if (quadCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quadCapacity));

        _indices = new uint[checked(quadCapacity * IndicesPerQuad)];
        BuildQuadIndices(_indices);
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
                checked(_indices.Length * sizeof(uint)),
                BufferType.Index,
                BufferUsage.Static,
                indexElementType: IndexElementType.UInt32);

            _graphicsDevice = activeDevice;
            _graphicsBuffer = activeDevice.CreateBuffer(description);
            activeDevice.UpdateBuffer(_graphicsBuffer, _indices, 0);
        }

        buffer = _graphicsBuffer;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ReleaseGraphicsBuffer();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static void BuildQuadIndices(Span<uint> indices)
    {
        int quadCount = indices.Length / IndicesPerQuad;

        for (int quad = 0; quad < quadCount; quad++)
        {
            uint vertexBase = checked((uint)(quad * VerticesPerQuad));
            int indexBase = quad * IndicesPerQuad;

            indices[indexBase] = vertexBase;
            indices[indexBase + 1] = vertexBase + 1;
            indices[indexBase + 2] = vertexBase + 2;
            indices[indexBase + 3] = vertexBase + 1;
            indices[indexBase + 4] = vertexBase + 3;
            indices[indexBase + 5] = vertexBase + 2;
        }
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
