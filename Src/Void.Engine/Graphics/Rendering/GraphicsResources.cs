// ============================================================================
//  GraphicsResources.cs
// ============================================================================
//  Public contracts for renderer-owned GPU resources and draw submissions.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Defines the common lifetime contract for a resource owned by a renderer backend.
/// </summary>
/// <remarks>
/// Built-in backend implementations such as OpenGL resource classes remain internal.
/// Renderer plugins expose their resources through these renderer-neutral interfaces.
/// Resources should be disposed by the code that owns them and should not be mixed
/// between unrelated <see cref="IGraphicsDevice"/> instances.
/// </remarks>
public interface IGraphicsResource : IDisposable
{
    /// <summary>
    /// Gets whether the underlying backend resource is still valid for use.
    /// </summary>
    bool IsValid { get; }
}

/// <summary>
/// Represents a renderer-owned GPU buffer.
/// </summary>
public interface IGraphicsBuffer : IGraphicsResource
{
    /// <summary>Gets the description used to create the buffer.</summary>
    BufferDescription Description { get; }
}

/// <summary>
/// Represents a renderer-owned GPU texture.
/// </summary>
public interface IGraphicsTexture : IGraphicsResource
{
    /// <summary>Gets the description used to create the texture.</summary>
    TextureDescription Description { get; }
}

/// <summary>
/// Represents a renderer-owned off-screen render target.
/// </summary>
public interface IGraphicsRenderTarget : IGraphicsResource
{
    /// <summary>Gets the description used to create the render target.</summary>
    RenderTargetDescription Description { get; }

    /// <summary>
    /// Gets the color texture containing the render target's rendered output.
    /// </summary>
    /// <remarks>
    /// The render target owns this texture. Disposing the render target may invalidate
    /// the returned resource, so callers should not dispose the color texture separately
    /// unless a backend explicitly documents different ownership.
    /// </remarks>
    IGraphicsTexture ColorTexture { get; }
}

/// <summary>
/// Represents a renderer-owned GPU shader program.
/// </summary>
/// <remarks>
/// Named uniform methods preserve VOID's shader-facing model while allowing each
/// renderer backend to map names to its native binding system. Uniform names that are
/// missing, inactive, or optimized away should be treated as harmless no-ops so VOID can
/// apply its conventional per-draw uniforms to custom programs. Resource type mismatches
/// may still be rejected clearly by the backend.
/// </remarks>
public interface IGraphicsShaderProgram : IGraphicsResource
{
    /// <summary>Sets a floating-point uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, float value);

    /// <summary>Sets an integer uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, int value);

    /// <summary>Sets a two-component vector uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect2 value);

    /// <summary>Sets a three-component vector uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect3 value);

    /// <summary>Sets a four-component vector uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect4 value);

    /// <summary>Sets a color uniform.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Color value);

    /// <summary>
    /// Sets a VOID-owned 4x4 matrix uniform.
    /// </summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Matrix value);

    /// <summary>Binds or clears a renderer-owned texture for a named shader input.</summary>
    /// <param name="name">The shader texture or sampler name.</param>
    /// <param name="texture">The texture to bind, or <see langword="null"/> to clear the existing texture assignment.</param>
    /// <remarks>
    /// A non-null texture is borrowed for the binding and remains owned by its original owner.
    /// It should belong to the same graphics device as the shader program. Passing <see langword="null"/> must
    /// remove the previous texture assignment for that name so later draws cannot observe a
    /// stale texture. A missing or inactive texture name should be treated as a harmless no-op.
    /// </remarks>
    void SetTexture(string name, IGraphicsTexture texture);
}

/// <summary>
/// Describes one renderer-neutral draw submission produced by VOID's batching layer.
/// </summary>
/// <remarks>
/// Renderer backends receive this structure through <see cref="IGraphicsDevice.Draw"/>.
/// Resources in a command are borrowed for the duration of the call and remain owned
/// by their existing owners. Implementations should not retain the command or dispose
/// any resource referenced by it.
/// </remarks>
public readonly struct RenderCommand
{
    /// <summary>Gets the vertex buffer used by the draw.</summary>
    public IGraphicsBuffer VertexBuffer { get; }

    /// <summary>Gets the optional index buffer used by an indexed draw.</summary>
    public IGraphicsBuffer IndexBuffer { get; }

    /// <summary>Gets the optional primary texture used by the draw.</summary>
    public IGraphicsTexture Texture { get; }

    /// <summary>Gets the shader program used by the draw.</summary>
    public IGraphicsShaderProgram Shader { get; }

    /// <summary>Gets the blend mode applied by the draw.</summary>
    public IBlendMode BlendMode { get; }

    /// <summary>Gets the primitive topology used by the draw.</summary>
    public PrimitiveType PrimitiveType { get; }

    /// <summary>Gets the first vertex used by the draw.</summary>
    public int VertexStart { get; }

    /// <summary>Gets the number of vertices available to the draw.</summary>
    public int VertexCount { get; }

    /// <summary>Gets the first index used by an indexed draw.</summary>
    public int IndexStart { get; }

    /// <summary>Gets the number of indices used by an indexed draw.</summary>
    public int IndexCount { get; }

    /// <summary>
    /// Gets the optional scissor rectangle in render-target pixels using VOID's
    /// top-left coordinate convention.
    /// </summary>
    public Rect2? ScissorRectangle { get; }

    /// <summary>
    /// Gets whether this command contains both an index buffer and a positive index count.
    /// </summary>
    public bool IsIndexed => IndexBuffer != null && IndexCount > 0;

    /// <summary>
    /// Creates a renderer-neutral draw command.
    /// </summary>
    /// <param name="vertexBuffer">The required vertex buffer.</param>
    /// <param name="primitiveType">The primitive topology.</param>
    /// <param name="vertexStart">The non-negative first vertex.</param>
    /// <param name="vertexCount">The positive number of vertices available to the draw.</param>
    /// <param name="blendMode">The blend mode, or null to use alpha blending.</param>
    /// <param name="texture">Optional primary texture.</param>
    /// <param name="shader">Shader program used by the draw.</param>
    /// <param name="indexBuffer">Optional index buffer. Required when <paramref name="indexCount"/> is positive.</param>
    /// <param name="indexStart">The non-negative first index.</param>
    /// <param name="indexCount">The non-negative number of indices. A positive value requires <paramref name="indexBuffer"/>.</param>
    /// <param name="scissorRectangle">
    /// Optional scissor rectangle in render-target pixels using a top-left origin.
    /// A null value disables scissor clipping for the draw.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="vertexBuffer"/> is null, or when <paramref name="indexCount"/>
    /// is positive and <paramref name="indexBuffer"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a start offset is negative, <paramref name="vertexCount"/> is not
    /// positive, or <paramref name="indexCount"/> is negative.
    /// </exception>
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
        int indexCount = 0,
        Rect2? scissorRectangle = null)
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
        if (indexCount > 0 && indexBuffer == null)
        {
            throw new ArgumentNullException(
                nameof(indexBuffer),
                "An index buffer is required when indexCount is positive.");
        }

        PrimitiveType = primitiveType;
        VertexStart = vertexStart;
        VertexCount = vertexCount;
        BlendMode = blendMode ?? global::Void.Engine.Graphics.BlendMode.Alpha;
        Texture = texture;
        Shader = shader;
        IndexBuffer = indexBuffer;
        IndexStart = indexStart;
        IndexCount = indexCount;
        ScissorRectangle = scissorRectangle;
    }
}
