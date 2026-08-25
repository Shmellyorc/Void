// ============================================================================
//  Texture.cs
// ============================================================================
//  Texture asset that wraps SFML texture data with loading, unloading,
//  and management capabilities.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// A texture asset that handles texture data with loading, unloading,
/// and management capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Texture"/> class implements <see cref="IAsset"/> and manages
/// the underlying graphics texture resource. It supports loading from file data,
/// creating textures programmatically, and wrapping existing textures.
/// </para>
/// <para>
/// Textures can be created in several ways:
/// <list type="bullet">
///   <item><description><b>Loaded from Data:</b> Loaded by <see cref="AssetManager"/> from file data</description></item>
///   <item><description><b>Programmatic:</b> Created via constructor with size and optional color</description></item>
///   <item><description><b>Wrapped:</b> Wraps an existing texture or render target texture</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Texture Types:</b>
/// <list type="bullet">
///   <item><description><see cref="AssetType.Normal"/> - Standard loaded texture (managed by AssetManager)</description></item>
///   <item><description><see cref="AssetType.Instanced"/> - Programmatically created texture (not managed)</description></item>
///   <item><description><see cref="AssetType.Atlas"/> - Texture from a render target (not managed)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a texture through AssetManager
/// var texture = AssetManager.Instance.Load&lt;Texture&gt;("textures/player.png");
/// 
/// // Create a texture programmatically
/// var whiteTexture = new Texture(new Vect2(32, 32), Color.White);
/// 
/// // Use the texture in rendering
/// sprite.Texture = texture;
/// 
/// // Check texture properties
/// Vect2 size = texture.Size;
/// int width = texture.Width;
/// int height = texture.Height;
/// Rect2 bounds = texture.Bounds;
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class Texture : IAsset, IEquatable<Texture>
{
    private SFImage _image;
    private SFTexture _texture;
    private readonly bool _repeated, _smooth;

    /// <summary>
    /// Gets the size of the texture in pixels.
    /// </summary>
    public Vect2 Size { get; private set; }

    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the texture repeats when sampled beyond its bounds.
    /// </summary>
    public bool Repeated => _repeated;

    /// <summary>
    /// Gets a value indicating whether the texture is smoothly interpolated.
    /// </summary>
    public bool Smooth => _smooth;

    /// <summary>
    /// Gets the unique identifier of the texture.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the texture.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the raw texture data bytes.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets a value indicating whether the texture is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the bounding rectangle of the texture.
    /// </summary>
    public Rect2 Bounds => new(Vect2.Zero, Size);

    /// <summary>
    /// Gets the last access time of the texture for eviction tracking.
    /// </summary>
    public ushort LastAccessTick { get; set; }

    /// <summary>
    /// Gets the asset type of the texture.
    /// </summary>
    public AssetType Type { get; }

    internal Texture(uint id, byte[] data, string tag, bool repeated, bool smooth)
    {
        Id = id;
        Data = data;
        Tag = tag;
        _repeated = repeated;
        _smooth = smooth;

        Type = AssetType.Normal;
    }

    /// <summary>
    /// Creates a new texture with the specified size and color.
    /// </summary>
    /// <param name="size">The size of the texture in pixels.</param>
    /// <param name="color">The fill color of the texture.</param>
    public Texture(Vect2 size, Color color)
    {
        _image = new SFImage(new((uint)size.X, (uint)size.Y), color);
        _texture = new SFTexture(_image)
        {
            Repeated = _repeated,
            Smooth = _smooth
        };

        Id = AssetManager.GetNextId();
        Size = size;
        Width = (int)size.X;
        Height = (int)size.Y;

        Type = AssetType.Instanced;
        IsValid = true;
    }

    /// <summary>
    /// Creates a new white texture with the specified size.
    /// </summary>
    /// <param name="size">The size of the texture in pixels.</param>
    public Texture(Vect2 size) : this(size, Color.White) { }

    internal Texture(SFRenderTexture renderTexture)
    {
        _texture = renderTexture.Texture;

        Id = AssetManager.GetNextId();
        Size = _texture.Size;
        Width = (int)Size.X;
        Height = (int)Size.Y;

        Type = AssetType.Atlas;
        IsValid = true;
    }

    internal Texture(SFTexture renderTexture)
    {
        _texture = renderTexture;

        Id = AssetManager.GetNextId();
        Size = _texture.Size;
        Width = (int)Size.X;
        Height = (int)Size.Y;

        Type = AssetType.Instanced;
        IsValid = true;
    }

    /// <summary>
    /// Finalizer that ensures resources are cleaned up if <see cref="Dispose"/> wasn't called.
    /// </summary>
    ~Texture() => Dispose();

    /// <summary>
    /// Updates this texture from another texture source.
    /// </summary>
    /// <param name="sourceTexture">The source texture to copy from.</param>
    internal void UpdateFrom(SFTexture sourceTexture)
    {
        if (sourceTexture == null || sourceTexture.IsInvalid)
            return;

        var image = sourceTexture.CopyToImage();
        _texture?.Dispose();
        _texture = new SFTexture(image);
        Size = _texture.Size;
        Width = (int)Size.X;
        Height = (int)Size.Y;
        image.Dispose();

        IsValid = true;
    }

    /// <summary>
    /// Loads the texture data into memory.
    /// </summary>
    public void Load()
    {
        if (Type == AssetType.None)
            throw new InvalidOperationException($"Texture type is '{Type}'.");

        if (Type == AssetType.Instanced || Type == AssetType.Atlas || IsValid)
        {
            return;
        }

        _image = new SFImage(Data);
        _texture = new SFTexture(_image);

        Size = _texture.Size;
        Width = (int)_texture.Size.X;
        Height = (int)_texture.Size.Y;

        IsValid = true;
    }

    /// <summary>
    /// Unloads the texture data from memory.
    /// </summary>
    public void Unload()
    {
        if (Type == AssetType.Instanced || Type == AssetType.Atlas || !IsValid)
            return;

        _image?.Dispose();
        _texture?.Dispose();

        IsValid = false;
    }

    /// <summary>
    /// Disposes the texture and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _image?.Dispose();
        _texture?.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Determines whether two textures are equal.
    /// </summary>
    public static bool operator ==(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return false;
        if (a is null)
            return !(b is null);

        return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two textures are not equal.
    /// </summary>
    public static bool operator !=(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return false;
        if (a is null)
            return !(b is null);

        return !a.Equals(b);
    }

    /// <summary>
    /// Determines whether the current texture is equal to another texture.
    /// </summary>
    public bool Equals(Texture other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id == other?.Id;
    }

    /// <summary>
    /// Determines whether the current texture is equal to the specified object.
    /// </summary>
    public override bool Equals(object obj)
        => obj is Texture other && Equals(other);

    /// <summary>
    /// Returns the hash code for the current texture.
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(Id);

    /// <summary>
    /// Returns a string representation of the current texture.
    /// </summary>
    public override string ToString()
        => $"Texture({Id}, {Data.Length}, {Tag}, {IsValid})";

    /// <summary>
    /// Implicitly converts a texture to an SFML texture.
    /// </summary>
    public static implicit operator SFTexture(Texture v)
    {
        AssetManager.Instance.Touch(v);

        if (v.Type == AssetType.Normal)
        {
            if (v._texture == null || !v.IsValid || v._texture.IsInvalid)
                v.Load();
        }

        return v._texture;
    }
}