// ============================================================================
//  Texture.cs
// ============================================================================
//  Game-facing renderer-neutral texture asset. Encoded source bytes remain in
//  Data; decoded RGBA pixels can recreate backend-owned GPU resources.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Assets.Loaders;

public sealed class Texture : IAsset, IEquatable<Texture>
{
    private readonly bool _repeated;
    private readonly bool _smooth;

    private byte[] _decodedPixels;
    private IGraphicsDevice _graphicsDevice;
    private IGraphicsTexture _graphicsTexture;
    private bool _ownsGraphicsTexture = true;

    public Vect2 Size { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool Repeated => _repeated;
    public bool Smooth => _smooth;
    public uint Id { get; }
    public string Tag { get; }

    /// <summary>Original encoded source bytes retained for GPU recreation.</summary>
    public byte[] Data { get; }

    public bool IsValid { get; private set; }
    public Rect2 Bounds => new(Vect2.Zero, Size);
    public DateTime LastAccessTime { get; private set; }
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

    public Texture(Vect2 size) : this(size, Color.White) { }

    /// <summary>
    /// Wraps a renderer-owned texture without taking ownership. Used by render
    /// targets and atlas pages.
    /// </summary>
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

    ~Texture() => Dispose();

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

    public void Unload()
    {
        if (Type == AssetType.Instanced || Type == AssetType.Atlas)
            return;

        ReleaseGraphicsTexture();
        _decodedPixels = null;
        IsValid = false;
    }

    public void Dispose()
    {
        ReleaseGraphicsTexture();
        _decodedPixels = null;
        IsValid = false;
        GC.SuppressFinalize(this);
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

    public static bool operator ==(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        return a.Equals(b);
    }

    public static bool operator !=(in Texture a, in Texture b) => !(a == b);

    public bool Equals(Texture other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => obj is Texture other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Id);
    public override string ToString() => $"Texture({Id}, {Data?.Length ?? 0}, {Tag}, {IsValid})";
}
