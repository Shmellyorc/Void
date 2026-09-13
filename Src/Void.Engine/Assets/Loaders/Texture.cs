// ============================================================================
//  Texture.cs
// ============================================================================
//  Game-facing texture asset backed by renderer-neutral graphics resources.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Represents a game-facing texture that can be loaded from encoded image data or created in memory.
/// </summary>
/// <remarks>
/// <para>
/// File-backed textures retain their encoded source bytes and create renderer-owned GPU resources lazily when needed.
/// This keeps the public texture API independent from the active graphics backend.
/// </para>
/// <para>
/// Textures created with the public constructors are instanced textures and are not evicted by <see cref="AssetManager"/>.
/// </para>
/// <code>
/// Texture player = AssetManager.Instance.Load&lt;Texture&gt;("Textures/player.png");
/// Texture whitePixel = new Texture(new Vect2(1, 1), Color.White);
/// </code>
/// </remarks>
public sealed class Texture : IAsset, IEquatable<Texture>
{
    private readonly bool _repeated;
    private readonly bool _smooth;

    private byte[] _decodedPixels;
    private IGraphicsDevice _graphicsDevice;
    private IGraphicsTexture _graphicsTexture;
    private bool _ownsGraphicsTexture = true;

    /// <summary>
    /// Gets the texture size in pixels.
    /// </summary>
    public Vect2 Size { get; private set; }

    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets whether texture coordinates outside the texture bounds repeat the image.
    /// </summary>
    public bool Repeated => _repeated;

    /// <summary>
    /// Gets whether linear filtering is requested for this texture.
    /// </summary>
    public bool Smooth => _smooth;

    /// <summary>
    /// Gets the identifier assigned to this texture.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized asset tag or source path, when one exists.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the original encoded source bytes retained for texture recreation.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets whether the texture has valid source or renderer-backed data available for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets a rectangle covering the full texture in pixel coordinates.
    /// </summary>
    public Rect2 Bounds => new(Vect2.Zero, Size);

    /// <summary>
    /// Gets the most recent time this texture was accessed by the asset or rendering systems.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Gets the lifecycle type assigned to this texture.
    /// </summary>
    public AssetType Type { get; }

    internal Texture(uint id, byte[] data, string tag, bool repeated, bool smooth)
    {
        Id = id;
        Data = data ?? Array.Empty<byte>();
        Tag = tag;
        _repeated = repeated;
        _smooth = smooth;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Creates an in-memory solid-color texture.
    /// </summary>
    /// <param name="size">The texture size in pixels.</param>
    /// <param name="color">The color used to fill every pixel.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting width or height is not greater than zero.</exception>
    public Texture(Vect2 size, Color color)
    {
        int width = checked((int)size.X);
        int height = checked((int)size.Y);
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        Data = Array.Empty<byte>();
        Id = AssetManager.GetNextId();
        Size = size;
        Width = width;
        Height = height;
        Type = AssetType.Instanced;
        LastAccessTime = DateTime.Now;
        _decodedPixels = CreateSolidPixels(width, height, color);
        IsValid = true;
    }

    /// <summary>
    /// Creates an in-memory white texture.
    /// </summary>
    /// <param name="size">The texture size in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting width or height is not greater than zero.</exception>
    public Texture(Vect2 size) : this(size, Color.White) { }

    // Wraps a renderer-owned texture without taking ownership. Render targets
    // and atlas pages use this path to expose their texture through the game API.
    internal Texture(IGraphicsDevice device, IGraphicsTexture graphicsTexture, AssetType type = AssetType.Atlas)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(graphicsTexture);

        Data = Array.Empty<byte>();
        Id = AssetManager.GetNextId();
        Width = graphicsTexture.Description.Width;
        Height = graphicsTexture.Description.Height;
        Size = new Vect2(Width, Height);
        Type = type;
        LastAccessTime = DateTime.Now;
        IsValid = graphicsTexture.IsValid;

        _graphicsDevice = device;
        _graphicsTexture = graphicsTexture;
        _ownsGraphicsTexture = false;
    }

    /// <summary>
    /// Ensures a normal file-backed texture has decoded pixel data available for use.
    /// </summary>
    /// <remarks>
    /// Instanced and renderer-backed textures are already prepared by their creation path and do not require decoding.
    /// </remarks>
    public void Load()
    {
        if (Type == AssetType.None)
            throw new InvalidOperationException($"Texture type is '{Type}'.");

        if (Type == AssetType.Instanced || Type == AssetType.Atlas || IsValid)
            return;

        if (!EnsureDecodedPixels())
            throw new InvalidOperationException($"Texture '{Tag ?? Id.ToString()}' has no decodable source data.");

        Size = new Vect2(Width, Height);
        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    /// <summary>
    /// Releases reloadable resources owned by a normal file-backed texture.
    /// </summary>
    /// <remarks>
    /// Instanced and renderer-backed atlas textures are not unloaded through this method.
    /// </remarks>
    public void Unload()
    {
        if (Type == AssetType.Instanced || Type == AssetType.Atlas)
            return;

        ReleaseGraphicsTexture();
        _decodedPixels = null;
        IsValid = false;
    }

    /// <summary>
    /// Releases resources owned by this texture.
    /// </summary>
    public void Dispose()
    {
        ReleaseGraphicsTexture();
        _decodedPixels = null;
        IsValid = false;
    }

    internal bool TryCopyPixelRegion(Rect2 sourceRect, out byte[] pixels, out int width, out int height)
    {
        LastAccessTime = DateTime.Now;

        if (Type == AssetType.Normal && !IsValid)
            Load();

        if (!EnsureDecodedPixels())
        {
            pixels = null;
            width = 0;
            height = 0;
            return false;
        }

        return RgbaPixelBuffer.TryCopyRegion(
            _decodedPixels,
            Width,
            Height,
            sourceRect,
            out pixels,
            out width,
            out height);
    }

    internal bool TryGetGraphicsTexture(out IGraphicsTexture texture)
    {
        LastAccessTime = DateTime.Now;

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice))
        {
            texture = null;
            return false;
        }

        if (_graphicsTexture != null && !ReferenceEquals(activeDevice, _graphicsDevice))
            ReleaseGraphicsTexture();

        if (_graphicsTexture == null)
        {
            if (!EnsureDecodedPixels())
            {
                texture = null;
                return false;
            }

            var description = new TextureDescription(
                Width,
                Height,
                TextureFormat.RGBA8,
                TextureUsage.Sampled | TextureUsage.TransferDestination,
                _smooth ? TextureFilter.Linear : TextureFilter.Nearest,
                _smooth ? TextureFilter.Linear : TextureFilter.Nearest,
                _repeated ? TextureWrap.Repeat : TextureWrap.ClampToEdge,
                _repeated ? TextureWrap.Repeat : TextureWrap.ClampToEdge);

            _graphicsDevice = activeDevice;
            _graphicsTexture = activeDevice.CreateTexture(description, _decodedPixels);
            _ownsGraphicsTexture = true;
        }

        texture = _graphicsTexture;
        return texture?.IsValid == true;
    }

    private bool EnsureDecodedPixels()
    {
        if (_decodedPixels is { Length: > 0 })
            return true;

        if (Data == null || Data.Length == 0)
            return false;

        DecodedImage decoded = ImageDecoder.DecodeRgba(Data);
        _decodedPixels = decoded.Pixels;
        Width = decoded.Width;
        Height = decoded.Height;
        Size = new Vect2(Width, Height);
        return true;
    }

    private void ReleaseGraphicsTexture()
    {
        if (_ownsGraphicsTexture)
            _graphicsTexture?.Dispose();

        _graphicsTexture = null;
        _graphicsDevice = null;
        _ownsGraphicsTexture = true;
    }

    private static byte[] CreateSolidPixels(int width, int height, Color color)
    {
        byte[] pixels = new byte[checked(width * height * 4)];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.R;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.B;
            pixels[i + 3] = color.A;
        }
        return pixels;
    }

    /// <summary>
    /// Determines whether two texture references identify the same texture asset.
    /// </summary>
    /// <param name="a">The first texture.</param>
    /// <param name="b">The second texture.</param>
    /// <returns><see langword="true"/> when both references are null, reference the same object, or have the same asset identifier.</returns>
    public static bool operator ==(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two texture references identify different texture assets.
    /// </summary>
    /// <param name="a">The first texture.</param>
    /// <param name="b">The second texture.</param>
    /// <returns><see langword="true"/> when the textures are not equal.</returns>
    public static bool operator !=(in Texture a, in Texture b) => !(a == b);

    /// <summary>
    /// Determines whether this texture has the same asset identifier as another texture.
    /// </summary>
    /// <param name="other">The texture to compare with this instance.</param>
    /// <returns><see langword="true"/> when both textures have the same identifier.</returns>
    public bool Equals(Texture other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id == other.Id;
    }

    /// <summary>
    /// Determines whether an object represents the same texture asset as this instance.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a texture with the same identifier.</returns>
    public override bool Equals(object obj) => obj is Texture other && Equals(other);

    /// <summary>
    /// Returns a hash code based on the texture asset identifier.
    /// </summary>
    /// <returns>A hash code for this texture.</returns>
    public override int GetHashCode() => HashCode.Combine(Id);

    /// <summary>
    /// Returns a diagnostic string describing this texture asset.
    /// </summary>
    /// <returns>A string containing the identifier, source byte count, tag, and validity.</returns>
    public override string ToString() => $"Texture({Id}, {Data?.Length ?? 0}, {Tag}, {IsValid})";
}
