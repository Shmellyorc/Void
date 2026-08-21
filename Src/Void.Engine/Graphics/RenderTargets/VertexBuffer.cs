// ============================================================================
//  VertexBuffer.cs
// ============================================================================
//  Internal implementation of IVertexBuffer that wraps SFML's vertex buffer
//  for GPU-accelerated vertex data storage and rendering.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

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

        if (target is TextureRenderTarget textureTarget)
        {
            var renderTexture = textureTarget.RenderTexture;
            var previousShader = ShaderState.GetCurrent();

            if (states.Shader != null && !states.Shader.IsInvalid)
            {
                ShaderState.Bind(states.Shader);

                if (states.Texture != null && !states.Texture.IsInvalid)
                {
                    states.Shader.SetUniform("uTexture", states.Texture);
                }
            }
            else
            {
                ShaderState.Bind(null);
            }

            _buffer.Draw(renderTexture, vertexStart, vertexCount, states);

            ShaderState.Bind(previousShader);
        }
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