using System.Runtime.InteropServices;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Identifies the graphics API implemented by a renderer backend.
/// </summary>
public enum GraphicsApi
{
    Unknown,
    OpenGL,
    Vulkan,
    Direct3D,
    Metal,
    Custom
}

/// <summary>
/// Window capabilities a renderer needs before its graphics device can be created.
/// </summary>
[Flags]
public enum RendererWindowFlags
{
    None = 0,
    OpenGL = 1 << 0,
    Vulkan = 1 << 1,
    Metal = 1 << 2
}

/// <summary>
/// Primitive topology used by a draw call.
/// </summary>
public enum PrimitiveType
{
    Points,
    Lines,
    LineStrip,
    Triangles,
    TriangleStrip,
    TriangleFan
}

/// <summary>
/// Type of GPU buffer being created.
/// </summary>
public enum BufferType
{
    Vertex,
    Index,
    Uniform,
    Storage
}

/// <summary>
/// Expected GPU buffer update frequency.
/// </summary>
public enum BufferUsage
{
    Static,
    Dynamic,
    Stream
}

/// <summary>
/// Backend-neutral vertex element formats. The format includes component count
/// and normalization behavior so renderer plugins can map it to their own API.
/// </summary>
public enum VertexElementFormat
{
    Float,
    Float2,
    Float3,
    Float4,
    Byte4,
    Byte4Normalized,
    UByte4,
    UByte4Normalized
}

/// <summary>
/// Integer width stored in an index buffer.
/// </summary>
public enum IndexElementType
{
    UInt16,
    UInt32
}

/// <summary>
/// Texture pixel format understood by renderer backends.
/// </summary>
public enum TextureFormat
{
    R8,
    RG8,
    RGB8,
    RGBA8,
    SRgba8,
    Depth16,
    Depth24,
    Depth24Stencil8,
    Depth32Float
}

/// <summary>
/// Intended uses for a GPU texture.
/// </summary>
[Flags]
public enum TextureUsage
{
    None = 0,
    Sampled = 1 << 0,
    RenderTarget = 1 << 1,
    TransferSource = 1 << 2,
    TransferDestination = 1 << 3
}

public enum TextureFilter
{
    Nearest,
    Linear
}

public enum TextureWrap
{
    ClampToEdge,
    Repeat,
    MirroredRepeat
}

public enum ShaderStage
{
    Vertex,
    Fragment,
    Geometry,
    Compute
}

/// <summary>
/// Shader representation supplied to a backend. Backends decide which
/// languages or binary formats they support.
/// </summary>
public enum ShaderLanguage
{
    Unknown,
    Glsl,
    Hlsl,
    SpirV,
    Dxil,
    Msl,
    Custom
}

/// <summary>
/// Common native handles a custom renderer may request from the window system.
/// Unsupported handles simply return false from IRendererContext.TryGetNativeHandle.
/// </summary>
public enum NativeWindowHandleKind
{
    Window,
    Display,
    Instance,
    Surface,
    View
}

/// <summary>
/// Major/minor graphics API version.
/// </summary>
public readonly struct GraphicsVersion : IEquatable<GraphicsVersion>, IComparable<GraphicsVersion>
{
    public uint Major { get; }
    public uint Minor { get; }

    public GraphicsVersion(uint major, uint minor)
    {
        Major = major;
        Minor = minor;
    }

    public int CompareTo(GraphicsVersion other)
    {
        int major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor.CompareTo(other.Minor);
    }

    public bool Equals(GraphicsVersion other) => Major == other.Major && Minor == other.Minor;
    public override bool Equals(object obj) => obj is GraphicsVersion other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Major, Minor);
    public override string ToString() => $"{Major}.{Minor}";

    public static bool operator ==(GraphicsVersion left, GraphicsVersion right) => left.Equals(right);
    public static bool operator !=(GraphicsVersion left, GraphicsVersion right) => !left.Equals(right);
    public static bool operator <(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// Backend-neutral 2D vertex used by VOID batching and draw submission.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    /// <summary>
    /// Canonical input layout for VOID's built-in 2D vertex. Renderer backends
    /// consume this descriptor without depending on the CLR field offsets by assumption.
    /// </summary>
    public static VertexLayoutDescription Layout { get; } = new(
        Marshal.SizeOf<Vertex>(),
        new VertexAttributeDescription[]
        {
            new(0, VertexElementFormat.Float2, Marshal.OffsetOf<Vertex>(nameof(Position)).ToInt32()),
            new(1, VertexElementFormat.UByte4Normalized, Marshal.OffsetOf<Vertex>(nameof(Color)).ToInt32()),
            new(2, VertexElementFormat.Float2, Marshal.OffsetOf<Vertex>(nameof(TexCoord)).ToInt32())
        });

    public readonly Vect2 Position;
    public readonly Color Color;
    public readonly Vect2 TexCoord;

    public Vertex(Vect2 position, Color color)
        : this(position, color, Vect2.Zero)
    {
    }

    public Vertex(Vect2 position, Color color, Vect2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }
}
