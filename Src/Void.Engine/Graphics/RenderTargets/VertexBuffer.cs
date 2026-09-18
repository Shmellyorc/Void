// ============================================================================
//  VertexBuffer.cs
// ============================================================================
//  Internal renderer-neutral vertex buffer used by VOID's batching layer.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;
using RenderPrimitiveType = Void.Engine.Graphics.Rendering.PrimitiveType;
using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

namespace Void.Engine.Graphics.RenderTargets;

internal sealed class VertexBuffer : IVertexBuffer, IGraphicsBufferSource
{
    private readonly int _capacity;

    private IGraphicsDevice _graphicsDevice;
    private IGraphicsBuffer _graphicsBuffer;

    // Only allocated when Update is called before a renderer is available.
    // During normal rendering the batcher's own vertex array is uploaded directly.
    private RenderVertex[] _pendingVertices;
    private int _pendingStart = int.MaxValue;
    private int _pendingEnd;
    private bool _hasGpuData;

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
    }

    public void Update(ReadOnlySpan<RenderVertex> vertices, uint vertexCount, uint offset)
    {
        ThrowIfDisposed();

        if (vertexCount > (uint)vertices.Length)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if ((ulong)offset + vertexCount > (ulong)_capacity)
            throw new ArgumentOutOfRangeException(nameof(offset), "Vertex update exceeds the buffer capacity.");

        int count = checked((int)vertexCount);
        if (count == 0)
            return;

        int destinationOffset = checked((int)offset);
        ReadOnlySpan<RenderVertex> source = vertices[..count];

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice))
        {
            StagePendingUpdate(source, destinationOffset);
            return;
        }

        EnsureGraphicsBuffer(activeDevice);

        int byteOffset = checked(destinationOffset * RenderVertex.Layout.Stride);
        activeDevice.UpdateBuffer(_graphicsBuffer, source, byteOffset);
        _hasGpuData = true;
    }

    public bool TryGetGraphicsBuffer(out IGraphicsBuffer buffer)
    {
        ThrowIfDisposed();

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice))
        {
            buffer = null;
            return false;
        }

        EnsureGraphicsBuffer(activeDevice);

        if (!_hasGpuData)
        {
            buffer = null;
            return false;
        }

        buffer = _graphicsBuffer;
        return true;
    }

    public void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, BatchRenderState states)
    {
        DrawInternal(
            target,
            vertexStart,
            vertexCount,
            states,
            indexBuffer: null,
            indexStart: 0,
            indexCount: 0);
    }

    internal void DrawIndexed(
        IRenderTarget target,
        IndexBuffer indexBuffer,
        uint vertexStart,
        uint vertexCount,
        uint indexStart,
        uint indexCount,
        BatchRenderState states)
    {
        ArgumentNullException.ThrowIfNull(indexBuffer);

        if ((ulong)indexStart + indexCount > (ulong)indexBuffer.IndexCount)
            throw new ArgumentOutOfRangeException(nameof(indexCount), "Indexed draw exceeds the index buffer capacity.");

        if (!indexBuffer.TryGetGraphicsBuffer(out IGraphicsBuffer graphicsIndexBuffer))
            throw new InvalidOperationException("Unable to resolve the renderer-owned index buffer.");

        DrawInternal(
            target,
            vertexStart,
            vertexCount,
            states,
            graphicsIndexBuffer,
            indexStart,
            indexCount);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ReleaseGraphicsBuffer();
        _pendingVertices = null;
        _pendingStart = int.MaxValue;
        _pendingEnd = 0;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void DrawInternal(
        IRenderTarget target,
        uint vertexStart,
        uint vertexCount,
        BatchRenderState states,
        IGraphicsBuffer indexBuffer,
        uint indexStart,
        uint indexCount)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(states);

        if ((ulong)vertexStart + vertexCount > (ulong)_capacity)
            throw new ArgumentOutOfRangeException(nameof(vertexCount), "Draw exceeds the vertex buffer capacity.");

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice device))
            throw new InvalidOperationException("No active graphics device is available.");

        IGraphicsDevice targetDevice = target.GraphicsDevice;
        if (targetDevice == null)
        {
            throw new InvalidOperationException(
                $"Render target '{target.GetType().Name}' returned no graphics device.");
        }

        IGraphicsRenderTarget graphicsTarget = target.GraphicsRenderTarget;
        if (graphicsTarget == null)
        {
            throw new InvalidOperationException(
                $"Render target '{target.GetType().Name}' returned no graphics render target.");
        }

        if (!ReferenceEquals(device, targetDevice))
            throw new InvalidOperationException("Vertex buffer and render target belong to different graphics devices.");

        if (!graphicsTarget.IsValid)
        {
            throw new InvalidOperationException(
                $"Render target '{target.GetType().Name}' returned an invalid graphics render target.");
        }

        if (!TryGetGraphicsBuffer(out IGraphicsBuffer vertexBuffer))
            throw new InvalidOperationException("Unable to resolve the renderer-owned vertex buffer. Upload vertex data before drawing.");

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

        device.SetRenderTarget(graphicsTarget);

        var command = new RenderCommand(
            vertexBuffer,
            _primitiveType,
            checked((int)vertexStart),
            checked((int)vertexCount),
            states.BlendMode,
            texture,
            shader,
            indexBuffer,
            checked((int)indexStart),
            checked((int)indexCount),
            CreateTargetScissor(states.ScissorRectangle, target));

        device.Draw(command);
    }

    private static Rect2? CreateTargetScissor(Rect2? scissorRectangle, IRenderTarget target)
    {
        if (!scissorRectangle.HasValue)
            return null;

        Vect2 viewport = GameSettings.Instance.Viewport;
        if (viewport.X <= 0f || viewport.Y <= 0f)
            return scissorRectangle;

        Rect2 rectangle = scissorRectangle.Value;
        float scaleX = target.Width / viewport.X;
        float scaleY = target.Height / viewport.Y;

        return new Rect2(
            rectangle.X * scaleX,
            rectangle.Y * scaleY,
            rectangle.Width * scaleX,
            rectangle.Height * scaleY);
    }

    private void EnsureGraphicsBuffer(IGraphicsDevice activeDevice)
    {
        if (_graphicsBuffer != null && !ReferenceEquals(activeDevice, _graphicsDevice))
            ReleaseGraphicsBuffer();

        if (_graphicsBuffer != null)
            return;

        var description = new BufferDescription(
            checked(_capacity * RenderVertex.Layout.Stride),
            BufferType.Vertex,
            BufferUsage.Stream,
            RenderVertex.Layout);

        _graphicsDevice = activeDevice;
        _graphicsBuffer = activeDevice.CreateBuffer(description);
        _hasGpuData = false;

        UploadPendingData();
    }

    private void StagePendingUpdate(ReadOnlySpan<RenderVertex> source, int destinationOffset)
    {
        _pendingVertices ??= new RenderVertex[_capacity];
        source.CopyTo(_pendingVertices.AsSpan(destinationOffset, source.Length));

        _pendingStart = Math.Min(_pendingStart, destinationOffset);
        _pendingEnd = Math.Max(_pendingEnd, destinationOffset + source.Length);
    }

    private void UploadPendingData()
    {
        if (_pendingVertices == null || _pendingStart >= _pendingEnd)
            return;

        int count = _pendingEnd - _pendingStart;
        ReadOnlySpan<RenderVertex> pending = _pendingVertices.AsSpan(_pendingStart, count);
        int byteOffset = checked(_pendingStart * RenderVertex.Layout.Stride);

        _graphicsDevice.UpdateBuffer(_graphicsBuffer, pending, byteOffset);
        _hasGpuData = true;

        _pendingVertices = null;
        _pendingStart = int.MaxValue;
        _pendingEnd = 0;
    }

    private void ReleaseGraphicsBuffer()
    {
        _graphicsBuffer?.Dispose();
        _graphicsBuffer = null;
        _graphicsDevice = null;
        _hasGpuData = false;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
