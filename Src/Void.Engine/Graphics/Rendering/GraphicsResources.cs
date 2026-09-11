using Void.Engine.Graphics;
using System.Numerics;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Base contract for a backend-owned GPU resource. Built-in OpenGL resources
/// such as GLTexture remain internal while implementing these public contracts.
/// </summary>
public interface IGraphicsResource : IDisposable
{
    bool IsValid { get; }
}

public interface IGraphicsBuffer : IGraphicsResource
{
    BufferDescription Description { get; }
}

public interface IGraphicsTexture : IGraphicsResource
{
    TextureDescription Description { get; }
}

public interface IGraphicsRenderTarget : IGraphicsResource
{
    RenderTargetDescription Description { get; }
    IGraphicsTexture ColorTexture { get; }
}

/// <summary>
/// GPU shader program implemented by each renderer backend.
/// Named uniform methods preserve VOID's current shader-facing API while allowing
/// Vulkan/Direct3D backends to map them to their own binding model internally.
/// </summary>
public interface IGraphicsShaderProgram : IGraphicsResource
{
    void SetUniform(string name, float value);
    void SetUniform(string name, int value);
    void SetUniform(string name, Vect2 value);
    void SetUniform(string name, Vect3 value);
    void SetUniform(string name, Vect4 value);
    void SetUniform(string name, Color value);
    void SetUniform(string name, Matrix4x4 value);
    void SetTexture(string name, IGraphicsTexture texture);
}

/// <summary>
/// Backend-neutral draw submission produced by VOID's batching layer.
/// </summary>
public readonly struct RenderCommand
{
    public IGraphicsBuffer VertexBuffer { get; }
    public IGraphicsBuffer IndexBuffer { get; }
    public IGraphicsTexture Texture { get; }
    public IGraphicsShaderProgram Shader { get; }
    public IBlendMode BlendMode { get; }
    public PrimitiveType PrimitiveType { get; }
    public int VertexStart { get; }
    public int VertexCount { get; }
    public int IndexStart { get; }
    public int IndexCount { get; }

    public bool IsIndexed => IndexBuffer != null && IndexCount > 0;

    public RenderCommand(
        IGraphicsBuffer vertexBuffer,
        PrimitiveType primitiveType,
        int vertexStart,
        int vertexCount,
        IBlendMode blendMode = null,
        IGraphicsTexture texture = null,
        IGraphicsShaderProgram shader = null,
        IGraphicsBuffer indexBuffer = null,
        int indexStart = 0,
        int indexCount = 0)
    {
        VertexBuffer = vertexBuffer ?? throw new ArgumentNullException(nameof(vertexBuffer));

        if (vertexStart < 0)
            throw new ArgumentOutOfRangeException(nameof(vertexStart));
        if (vertexCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if (indexStart < 0)
            throw new ArgumentOutOfRangeException(nameof(indexStart));
        if (indexCount < 0)
            throw new ArgumentOutOfRangeException(nameof(indexCount));

        PrimitiveType = primitiveType;
        VertexStart = vertexStart;
        VertexCount = vertexCount;
        BlendMode = blendMode ?? global::Void.Engine.Graphics.BlendMode.Alpha;
        Texture = texture;
        Shader = shader;
        IndexBuffer = indexBuffer;
        IndexStart = indexStart;
        IndexCount = indexCount;
    }
}
