// ============================================================================
//  GraphicsTypes.cs
// ============================================================================
//  Renderer-neutral enums and value types shared by VOID and renderer plugins.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Runtime.InteropServices;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Identifies the graphics API implemented by a renderer backend.
/// </summary>
public enum GraphicsApi
{
    /// <summary>The backend does not identify a known graphics API.</summary>
    Unknown,

    /// <summary>OpenGL.</summary>
    OpenGL,

    /// <summary>Vulkan.</summary>
    Vulkan,

    /// <summary>Direct3D.</summary>
    Direct3D,

    /// <summary>Metal.</summary>
    Metal,

    /// <summary>A renderer-specific API not represented by another value.</summary>
    Custom
}

/// <summary>
/// Describes native window features that must be enabled before a renderer is initialized.
/// </summary>
[Flags]
public enum RendererWindowFlags
{
    /// <summary>No graphics-specific window feature is required.</summary>
    None = 0,

    /// <summary>The window must be created with OpenGL support.</summary>
    OpenGL = 1 << 0,

    /// <summary>The window must be created with Vulkan support.</summary>
    Vulkan = 1 << 1,

    /// <summary>The window must be created with Metal support.</summary>
    Metal = 1 << 2
}

/// <summary>
/// Describes how vertices are assembled into primitives for a draw command.
/// </summary>
public enum PrimitiveType
{
    /// <summary>Each vertex produces one point.</summary>
    Points,

    /// <summary>Each pair of vertices produces one independent line segment.</summary>
    Lines,

    /// <summary>Vertices form one connected line strip.</summary>
    LineStrip,

    /// <summary>Each group of three vertices produces one independent triangle.</summary>
    Triangles,

    /// <summary>Vertices form a connected triangle strip.</summary>
    TriangleStrip,

    /// <summary>Vertices form a triangle fan around the first vertex.</summary>
    TriangleFan
}

/// <summary>
/// Identifies the role of a GPU buffer.
/// </summary>
public enum BufferType
{
    /// <summary>Vertex data consumed by a draw command.</summary>
    Vertex,

    /// <summary>Index data consumed by an indexed draw command.</summary>
    Index,

    /// <summary>Uniform or constant-buffer data.</summary>
    Uniform,

    /// <summary>General shader storage data.</summary>
    Storage
}

/// <summary>
/// Describes the expected update frequency of a GPU buffer.
/// </summary>
public enum BufferUsage
{
    /// <summary>The buffer is expected to change rarely after creation.</summary>
    Static,

    /// <summary>The buffer is expected to change periodically.</summary>
    Dynamic,

    /// <summary>The buffer is expected to be replaced or updated frequently.</summary>
    Stream
}

/// <summary>
/// Describes the component layout and normalization behavior of one vertex attribute.
/// </summary>
public enum VertexElementFormat
{
    /// <summary>One 32-bit floating-point component.</summary>
    Float,

    /// <summary>Two 32-bit floating-point components.</summary>
    Float2,

    /// <summary>Three 32-bit floating-point components.</summary>
    Float3,

    /// <summary>Four 32-bit floating-point components.</summary>
    Float4,

    /// <summary>Four signed 8-bit integer components.</summary>
    Byte4,

    /// <summary>Four signed 8-bit components normalized by the backend.</summary>
    Byte4Normalized,

    /// <summary>Four unsigned 8-bit integer components.</summary>
    UByte4,

    /// <summary>Four unsigned 8-bit components normalized by the backend.</summary>
    UByte4Normalized
}

/// <summary>
/// Identifies the integer width stored in an index buffer.
/// </summary>
public enum IndexElementType
{
    /// <summary>Unsigned 16-bit indices.</summary>
    UInt16,

    /// <summary>Unsigned 32-bit indices.</summary>
    UInt32
}

/// <summary>
/// Identifies a renderer-neutral texture pixel format.
/// </summary>
/// <remarks>
/// A backend may support only a subset of these formats. Renderer authors should
/// report relevant limits through <see cref="RendererCapabilities"/> and reject
/// unsupported resource descriptions clearly.
/// </remarks>
public enum TextureFormat
{
    /// <summary>One unsigned 8-bit red component.</summary>
    R8,

    /// <summary>Unsigned 8-bit red and green components.</summary>
    RG8,

    /// <summary>Unsigned 8-bit red, green, and blue components.</summary>
    RGB8,

    /// <summary>Unsigned 8-bit linear red, green, blue, and alpha components.</summary>
    RGBA8,

    /// <summary>sRGB red, green, and blue with an unsigned 8-bit alpha component.</summary>
    SRgba8,

    /// <summary>16-bit depth data.</summary>
    Depth16,

    /// <summary>24-bit depth data.</summary>
    Depth24,

    /// <summary>24-bit depth data with 8-bit stencil data.</summary>
    Depth24Stencil8,

    /// <summary>32-bit floating-point depth data.</summary>
    Depth32Float
}

/// <summary>
/// Describes the intended uses of a GPU texture.
/// </summary>
[Flags]
public enum TextureUsage
{
    /// <summary>No usage has been declared.</summary>
    None = 0,

    /// <summary>The texture may be sampled by shaders.</summary>
    Sampled = 1 << 0,

    /// <summary>The texture may be attached to a render target.</summary>
    RenderTarget = 1 << 1,

    /// <summary>The texture may be used as a transfer or copy source.</summary>
    TransferSource = 1 << 2,

    /// <summary>The texture may receive transfer, copy, or upload data.</summary>
    TransferDestination = 1 << 3
}

/// <summary>
/// Describes texture sampling between neighboring texels.
/// </summary>
public enum TextureFilter
{
    /// <summary>Use the nearest texel.</summary>
    Nearest,

    /// <summary>Linearly interpolate neighboring texels.</summary>
    Linear
}

/// <summary>
/// Describes how texture coordinates outside the normal texture range are handled.
/// </summary>
public enum TextureWrap
{
    /// <summary>Clamp coordinates to the texture edge.</summary>
    ClampToEdge,

    /// <summary>Repeat the texture.</summary>
    Repeat,

    /// <summary>Repeat the texture while mirroring every other repetition.</summary>
    MirroredRepeat
}

/// <summary>
/// Identifies a programmable shader stage.
/// </summary>
public enum ShaderStage
{
    /// <summary>Vertex processing stage.</summary>
    Vertex,

    /// <summary>Fragment or pixel processing stage.</summary>
    Fragment,

    /// <summary>Geometry processing stage.</summary>
    Geometry,

    /// <summary>Compute processing stage.</summary>
    Compute
}

/// <summary>
/// Identifies the source or binary representation supplied for a shader stage.
/// </summary>
/// <remarks>
/// Backends decide which languages and binary formats they support. For example,
/// VOID's built-in OpenGL renderer consumes GLSL source while another renderer may
/// consume HLSL, SPIR-V, DXIL, MSL, or a custom representation.
/// </remarks>
public enum ShaderLanguage
{
    /// <summary>The shader representation is unknown.</summary>
    Unknown,

    /// <summary>OpenGL Shading Language source.</summary>
    Glsl,

    /// <summary>High-Level Shading Language source.</summary>
    Hlsl,

    /// <summary>SPIR-V binary data.</summary>
    SpirV,

    /// <summary>DirectX Intermediate Language binary data.</summary>
    Dxil,

    /// <summary>Metal Shading Language source.</summary>
    Msl,

    /// <summary>A backend-specific representation not covered by another value.</summary>
    Custom
}

/// <summary>
/// Identifies a platform-native handle that a renderer may request from
/// <see cref="IRendererContext"/>.
/// </summary>
public enum NativeWindowHandleKind
{
    /// <summary>The native window handle.</summary>
    Window,

    /// <summary>The native display or connection handle.</summary>
    Display,

    /// <summary>A native application or graphics instance handle.</summary>
    Instance,

    /// <summary>A native presentation surface handle.</summary>
    Surface,

    /// <summary>A native platform view handle.</summary>
    View
}

/// <summary>
/// Represents a graphics API version using major and minor components.
/// </summary>
public readonly struct GraphicsVersion : IEquatable<GraphicsVersion>, IComparable<GraphicsVersion>
{
    /// <summary>Gets the major version component.</summary>
    public uint Major { get; }

    /// <summary>Gets the minor version component.</summary>
    public uint Minor { get; }

    /// <summary>
    /// Creates a graphics API version.
    /// </summary>
    /// <param name="major">The major version component.</param>
    /// <param name="minor">The minor version component.</param>
    public GraphicsVersion(uint major, uint minor)
    {
        Major = major;
        Minor = minor;
    }

    /// <summary>Compares this version with another version.</summary>
    /// <param name="other">The version to compare with.</param>
    /// <returns>
    /// A negative value when this version is older, zero when the versions are equal,
    /// or a positive value when this version is newer.
    /// </returns>
    public int CompareTo(GraphicsVersion other)
    {
        int major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor.CompareTo(other.Minor);
    }

    /// <summary>Determines whether this value equals another graphics version.</summary>
    /// <param name="other">The version to compare with.</param>
    /// <returns><see langword="true"/> when both version components are equal.</returns>
    public bool Equals(GraphicsVersion other) => Major == other.Major && Minor == other.Minor;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is GraphicsVersion other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Major, Minor);

    /// <summary>Returns the version in <c>major.minor</c> form.</summary>
    /// <returns>The formatted graphics version.</returns>
    public override string ToString() => $"{Major}.{Minor}";

    /// <summary>Determines whether two versions are equal.</summary>
    public static bool operator ==(GraphicsVersion left, GraphicsVersion right) => left.Equals(right);

    /// <summary>Determines whether two versions are different.</summary>
    public static bool operator !=(GraphicsVersion left, GraphicsVersion right) => !left.Equals(right);

    /// <summary>Determines whether the left version is older than the right version.</summary>
    public static bool operator <(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) < 0;

    /// <summary>Determines whether the left version is newer than the right version.</summary>
    public static bool operator >(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) > 0;

    /// <summary>Determines whether the left version is older than or equal to the right version.</summary>
    public static bool operator <=(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) <= 0;

    /// <summary>Determines whether the left version is newer than or equal to the right version.</summary>
    public static bool operator >=(GraphicsVersion left, GraphicsVersion right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// Represents VOID's canonical renderer-neutral 2D vertex.
/// </summary>
/// <remarks>
/// <see cref="Layout"/> defines the exact attribute locations and byte offsets used
/// by VOID's built-in batching path. Renderer backends should consume that descriptor
/// instead of assuming a backend-specific vertex layout.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    /// <summary>
    /// Gets the canonical input layout for VOID's built-in 2D vertex.
    /// </summary>
    public static VertexLayoutDescription Layout { get; } = new(
        Marshal.SizeOf<Vertex>(),
        new VertexAttributeDescription[]
        {
            new(0, VertexElementFormat.Float2, Marshal.OffsetOf<Vertex>(nameof(Position)).ToInt32()),
            new(1, VertexElementFormat.UByte4Normalized, Marshal.OffsetOf<Vertex>(nameof(Color)).ToInt32()),
            new(2, VertexElementFormat.Float2, Marshal.OffsetOf<Vertex>(nameof(TexCoord)).ToInt32())
        });

    /// <summary>Vertex position in VOID render coordinates.</summary>
    public readonly Vect2 Position;

    /// <summary>Vertex color.</summary>
    public readonly Color Color;

    /// <summary>Texture coordinate in VOID's texture-coordinate convention.</summary>
    public readonly Vect2 TexCoord;

    /// <summary>
    /// Creates an untextured vertex.
    /// </summary>
    /// <param name="position">Vertex position.</param>
    /// <param name="color">Vertex color.</param>
    public Vertex(Vect2 position, Color color)
        : this(position, color, Vect2.Zero)
    {
    }

    /// <summary>
    /// Creates a vertex with position, color, and texture coordinates.
    /// </summary>
    /// <param name="position">Vertex position.</param>
    /// <param name="color">Vertex color.</param>
    /// <param name="texCoord">Texture coordinate.</param>
    public Vertex(Vect2 position, Color color, Vect2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }
}
