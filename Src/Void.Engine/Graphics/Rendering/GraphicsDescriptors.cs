// ============================================================================
//  GraphicsDescriptors.cs
// ============================================================================
//  Renderer-neutral descriptions used to create graphics resources.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Describes one attribute inside a vertex record.
/// </summary>
/// <remarks>
/// <see cref="Location"/> is a renderer-neutral shader input location, not a
/// backend object identifier. <see cref="Offset"/> is measured in bytes from the
/// beginning of one vertex record.
/// </remarks>
public readonly struct VertexAttributeDescription
{
    /// <summary>Gets the shader input location used by this attribute.</summary>
    public int Location { get; }

    /// <summary>Gets the component format of this attribute.</summary>
    public VertexElementFormat Format { get; }

    /// <summary>Gets the byte offset of this attribute within one vertex record.</summary>
    public int Offset { get; }

    /// <summary>
    /// Creates a vertex attribute description.
    /// </summary>
    /// <param name="location">The non-negative shader input location.</param>
    /// <param name="format">The attribute component format.</param>
    /// <param name="offset">The non-negative byte offset within the vertex record.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="location"/> or <paramref name="offset"/> is negative.
    /// </exception>
    public VertexAttributeDescription(int location, VertexElementFormat format, int offset)
    {
        if (location < 0)
            throw new ArgumentOutOfRangeException(nameof(location));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        Location = location;
        Format = format;
        Offset = offset;
    }
}

/// <summary>
/// Describes the in-memory layout of one vertex buffer binding.
/// </summary>
/// <remarks>
/// Attribute locations must be unique. Attribute offsets are relative to the
/// beginning of each vertex, and each attribute must fit completely within
/// <see cref="Stride"/>.
/// Renderer implementations may reject layouts that their API cannot represent.
/// </remarks>
public readonly struct VertexLayoutDescription
{
    /// <summary>Gets the size of one vertex record in bytes.</summary>
    public int Stride { get; }

    /// <summary>Gets the attributes contained in one vertex record.</summary>
    public ReadOnlyMemory<VertexAttributeDescription> Attributes { get; }

    /// <summary>
    /// Creates a vertex layout description.
    /// </summary>
    /// <param name="stride">The positive size of one vertex record in bytes.</param>
    /// <param name="attributes">The non-empty set of vertex attributes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="stride"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when no attributes are supplied, an attribute does not fit completely
    /// within the vertex stride, or an input location is declared more than once.
    /// </exception>
    public VertexLayoutDescription(int stride, ReadOnlyMemory<VertexAttributeDescription> attributes)
    {
        if (stride <= 0)
            throw new ArgumentOutOfRangeException(nameof(stride));
        if (attributes.IsEmpty)
            throw new ArgumentException("A vertex layout requires at least one attribute.", nameof(attributes));

        Span<int> locations = attributes.Length <= 32
            ? stackalloc int[attributes.Length]
            : new int[attributes.Length];

        int count = 0;
        foreach (VertexAttributeDescription attribute in attributes.Span)
        {
            int elementSize = GetElementSizeInBytes(attribute.Format);
            if (attribute.Offset > stride - elementSize)
            {
                throw new ArgumentException(
                    $"Vertex attribute at location {attribute.Location} with format {attribute.Format} exceeds the vertex stride.",
                    nameof(attributes));
            }

            for (int i = 0; i < count; i++)
            {
                if (locations[i] == attribute.Location)
                    throw new ArgumentException($"Vertex location {attribute.Location} is declared more than once.", nameof(attributes));
            }

            locations[count++] = attribute.Location;
        }

        Stride = stride;
        Attributes = attributes;
    }

    private static int GetElementSizeInBytes(VertexElementFormat format)
        => format switch
        {
            VertexElementFormat.Float => sizeof(float),
            VertexElementFormat.Float2 => sizeof(float) * 2,
            VertexElementFormat.Float3 => sizeof(float) * 3,
            VertexElementFormat.Float4 => sizeof(float) * 4,
            VertexElementFormat.Byte4 or
            VertexElementFormat.Byte4Normalized or
            VertexElementFormat.UByte4 or
            VertexElementFormat.UByte4Normalized => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown vertex element format.")
        };
}

/// <summary>
/// Describes a GPU buffer without exposing backend-specific resource enums.
/// </summary>
public readonly struct BufferDescription
{
    /// <summary>Gets the requested buffer capacity in bytes.</summary>
    public int SizeInBytes { get; }

    /// <summary>Gets the role of the buffer.</summary>
    public BufferType Type { get; }

    /// <summary>Gets the expected update frequency of the buffer.</summary>
    public BufferUsage Usage { get; }

    /// <summary>
    /// Gets the vertex layout when this is a vertex buffer, or <see langword="null"/>
    /// when no layout was supplied.
    /// </summary>
    public VertexLayoutDescription? VertexLayout { get; }

    /// <summary>
    /// Gets the integer width used when this is an index buffer.
    /// </summary>
    public IndexElementType IndexElementType { get; }

    /// <summary>
    /// Creates a GPU buffer description.
    /// </summary>
    /// <param name="sizeInBytes">The positive buffer capacity in bytes.</param>
    /// <param name="type">The role of the buffer.</param>
    /// <param name="usage">The expected update frequency.</param>
    /// <param name="vertexLayout">Optional layout for a vertex buffer.</param>
    /// <param name="indexElementType">Integer width used by an index buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizeInBytes"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a vertex layout is supplied for a non-vertex buffer.
    /// </exception>
    public BufferDescription(
        int sizeInBytes,
        BufferType type,
        BufferUsage usage = BufferUsage.Static,
        VertexLayoutDescription? vertexLayout = null,
        IndexElementType indexElementType = IndexElementType.UInt32)
    {
        if (sizeInBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes));

        if (type != BufferType.Vertex && vertexLayout.HasValue)
            throw new ArgumentException("Only vertex buffers can declare a vertex layout.", nameof(vertexLayout));

        SizeInBytes = sizeInBytes;
        Type = type;
        Usage = usage;
        VertexLayout = vertexLayout;
        IndexElementType = indexElementType;
    }
}

/// <summary>
/// Describes a renderer-owned GPU texture.
/// </summary>
/// <remarks>
/// This type describes backend resources and is intentionally separate from the
/// game-facing <see cref="Void.Engine.Assets.Loaders.Texture"/> asset. A renderer may reject formats, sample
/// counts, or usage combinations it does not support.
/// </remarks>
public readonly struct TextureDescription
{
    /// <summary>Gets the texture width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the texture height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the texture pixel format.</summary>
    public TextureFormat Format { get; }

    /// <summary>Gets the declared texture usages.</summary>
    public TextureUsage Usage { get; }

    /// <summary>Gets the minification filter.</summary>
    public TextureFilter MinFilter { get; }

    /// <summary>Gets the magnification filter.</summary>
    public TextureFilter MagFilter { get; }

    /// <summary>Gets the horizontal wrap mode.</summary>
    public TextureWrap WrapU { get; }

    /// <summary>Gets the vertical wrap mode.</summary>
    public TextureWrap WrapV { get; }

    /// <summary>Gets the requested sample count.</summary>
    public int SampleCount { get; }

    /// <summary>Gets whether mipmaps should be generated for the texture.</summary>
    public bool GenerateMipmaps { get; }

    /// <summary>
    /// Creates a GPU texture description.
    /// </summary>
    /// <param name="width">The positive texture width in pixels.</param>
    /// <param name="height">The positive texture height in pixels.</param>
    /// <param name="format">The texture pixel format.</param>
    /// <param name="usage">The intended texture usages.</param>
    /// <param name="minFilter">The minification filter.</param>
    /// <param name="magFilter">The magnification filter.</param>
    /// <param name="wrapU">The horizontal wrap mode.</param>
    /// <param name="wrapV">The vertical wrap mode.</param>
    /// <param name="sampleCount">The positive sample count.</param>
    /// <param name="generateMipmaps">Whether mipmaps should be generated.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a dimension or <paramref name="sampleCount"/> is not positive.
    /// </exception>
    public TextureDescription(
        int width,
        int height,
        TextureFormat format = TextureFormat.RGBA8,
        TextureUsage usage = TextureUsage.Sampled | TextureUsage.TransferDestination,
        TextureFilter minFilter = TextureFilter.Nearest,
        TextureFilter magFilter = TextureFilter.Nearest,
        TextureWrap wrapU = TextureWrap.ClampToEdge,
        TextureWrap wrapV = TextureWrap.ClampToEdge,
        int sampleCount = 1,
        bool generateMipmaps = false)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (sampleCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleCount));

        Width = width;
        Height = height;
        Format = format;
        Usage = usage;
        MinFilter = minFilter;
        MagFilter = magFilter;
        WrapU = wrapU;
        WrapV = wrapV;
        SampleCount = sampleCount;
        GenerateMipmaps = generateMipmaps;
    }
}

/// <summary>
/// Describes an off-screen render target.
/// </summary>
/// <remarks>
/// Renderer backends may support only a subset of color formats, depth formats,
/// and sample counts. Unsupported descriptions should fail clearly during resource
/// creation rather than being silently changed.
/// </remarks>
public readonly struct RenderTargetDescription
{
    /// <summary>Gets the render-target width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the render-target height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the color attachment format.</summary>
    public TextureFormat ColorFormat { get; }

    /// <summary>Gets the optional depth or depth-stencil attachment format.</summary>
    public TextureFormat? DepthFormat { get; }

    /// <summary>Gets the requested sample count.</summary>
    public int SampleCount { get; }

    /// <summary>
    /// Creates an off-screen render-target description.
    /// </summary>
    /// <param name="width">The positive width in pixels.</param>
    /// <param name="height">The positive height in pixels.</param>
    /// <param name="colorFormat">The color attachment format.</param>
    /// <param name="depthFormat">Optional depth or depth-stencil attachment format.</param>
    /// <param name="sampleCount">The positive sample count.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a dimension or <paramref name="sampleCount"/> is not positive.
    /// </exception>
    public RenderTargetDescription(
        int width,
        int height,
        TextureFormat colorFormat = TextureFormat.RGBA8,
        TextureFormat? depthFormat = null,
        int sampleCount = 1)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (sampleCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleCount));

        Width = width;
        Height = height;
        ColorFormat = colorFormat;
        DepthFormat = depthFormat;
        SampleCount = sampleCount;
    }
}

/// <summary>
/// Describes one backend-neutral shader stage.
/// </summary>
/// <remarks>
/// <see cref="Data"/> contains either source text or binary shader data according
/// to <see cref="Language"/>. Textual shader languages should be encoded as UTF-8.
/// Backends decide which shader languages and stages they support.
/// </remarks>
public readonly struct ShaderSource
{
    /// <summary>Gets the programmable stage represented by this source.</summary>
    public ShaderStage Stage { get; }

    /// <summary>Gets the source or binary representation.</summary>
    public ShaderLanguage Language { get; }

    /// <summary>Gets the shader source text or binary payload.</summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>Gets the shader entry-point name.</summary>
    public string EntryPoint { get; }

    /// <summary>
    /// Creates a shader-stage description.
    /// </summary>
    /// <param name="stage">The programmable shader stage.</param>
    /// <param name="language">The representation used by <paramref name="data"/>.</param>
    /// <param name="data">The non-empty shader source or binary payload.</param>
    /// <param name="entryPoint">
    /// The entry-point name. Null, empty, or whitespace uses <c>main</c>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
    public ShaderSource(
        ShaderStage stage,
        ShaderLanguage language,
        ReadOnlyMemory<byte> data,
        string entryPoint = "main")
    {
        if (data.IsEmpty)
            throw new ArgumentException("Shader source cannot be empty.", nameof(data));

        Stage = stage;
        Language = language;
        Data = data;
        EntryPoint = string.IsNullOrWhiteSpace(entryPoint) ? "main" : entryPoint;
    }
}

/// <summary>
/// Describes the shader stages used to create a renderer-owned shader program.
/// </summary>
/// <remarks>
/// The memory supplied through <see cref="Sources"/> is retained by this value.
/// Callers should treat the underlying source collection as immutable after the
/// description is created.
/// </remarks>
public readonly struct ShaderProgramDescription
{
    /// <summary>Gets the shader stages used by the program.</summary>
    public ReadOnlyMemory<ShaderSource> Sources { get; }

    /// <summary>
    /// Creates a shader-program description.
    /// </summary>
    /// <param name="sources">The non-empty set of shader stages.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sources"/> is empty.</exception>
    public ShaderProgramDescription(ReadOnlyMemory<ShaderSource> sources)
    {
        if (sources.IsEmpty)
            throw new ArgumentException("At least one shader source is required.", nameof(sources));

        Sources = sources;
    }
}
