namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Describes one attribute inside a vertex record.
/// Locations are renderer-neutral shader input locations, not OpenGL object IDs.
/// </summary>
public readonly struct VertexAttributeDescription
{
    public int Location { get; }
    public VertexElementFormat Format { get; }
    public int Offset { get; }

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
public readonly struct VertexLayoutDescription
{
    public int Stride { get; }
    public ReadOnlyMemory<VertexAttributeDescription> Attributes { get; }

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
            if (attribute.Offset >= stride)
                throw new ArgumentException("A vertex attribute offset must be smaller than the vertex stride.", nameof(attributes));

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
}

/// <summary>
/// Describes a GPU buffer without exposing backend-specific enums.
/// </summary>
public readonly struct BufferDescription
{
    public int SizeInBytes { get; }
    public BufferType Type { get; }
    public BufferUsage Usage { get; }
    public VertexLayoutDescription? VertexLayout { get; }
    public IndexElementType IndexElementType { get; }

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
/// Describes a GPU texture. This is intentionally separate from the game-facing Texture asset.
/// </summary>
public readonly struct TextureDescription
{
    public int Width { get; }
    public int Height { get; }
    public TextureFormat Format { get; }
    public TextureUsage Usage { get; }
    public TextureFilter MinFilter { get; }
    public TextureFilter MagFilter { get; }
    public TextureWrap WrapU { get; }
    public TextureWrap WrapV { get; }
    public int SampleCount { get; }
    public bool GenerateMipmaps { get; }

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
public readonly struct RenderTargetDescription
{
    public int Width { get; }
    public int Height { get; }
    public TextureFormat ColorFormat { get; }
    public TextureFormat? DepthFormat { get; }
    public int SampleCount { get; }

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
/// Backend-neutral shader source. Text and binary shader formats are both represented as bytes.
/// </summary>
public readonly struct ShaderSource
{
    public ShaderStage Stage { get; }
    public ShaderLanguage Language { get; }
    public ReadOnlyMemory<byte> Data { get; }
    public string EntryPoint { get; }

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
/// Immutable set of shader stages used to create a GPU shader program.
/// </summary>
public readonly struct ShaderProgramDescription
{
    public ReadOnlyMemory<ShaderSource> Sources { get; }

    public ShaderProgramDescription(ReadOnlyMemory<ShaderSource> sources)
    {
        if (sources.IsEmpty)
            throw new ArgumentException("At least one shader source is required.", nameof(sources));

        Sources = sources;
    }
}
