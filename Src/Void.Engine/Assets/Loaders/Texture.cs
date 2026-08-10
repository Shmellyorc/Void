namespace Void.Engine.Assets.Loaders;

public sealed class Texture : IAsset, IEquatable<Texture>
{
    private SFImage _image;
    private SFTexture _texture;
    private readonly bool _repeated, _smooth;

    // NOTE: Don't rely off SFML Texture incase its invalid,
    // always set these on Load()
    public Vect2 Size { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool Repeated => _repeated;
    public bool Smooth => _smooth;

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public bool IsValid { get; private set; }
    public Rect2 Bounds => new(Vect2.Zero, Size);
    public DateTime LastAccessTime { get; private set; }
    public AssetType Type { get; }

    internal uint NativeHandle => _texture.NativeHandle;

    internal Texture(uint id, byte[] data, string tag, bool repeated, bool smooth)
    {
        Id = id;
        Data = data;
        Tag = tag;
        _repeated = repeated;
        _smooth = smooth;

        LastAccessTime = DateTime.Now;
        Type = AssetType.Normal;
    }

    public Texture(Vect2 size, Color color)
    {
        _image = new SFImage((uint)size.X, (uint)size.Y, color);
        _texture = new SFTexture(_image)
        {
            Repeated = _repeated,
            Smooth = _smooth
        };

        Id = AssetManager._id++;
        Size = size;
        Width = (int)size.X;
        Height = (int)size.Y;

        LastAccessTime = DateTime.Now;
        Type = AssetType.Instanced;
        IsValid = true;
    }
    public Texture(Vect2 size) : this(size, Color.White) { }

    internal Texture(SFRenderTexture renderTexture)
    {
        _texture = renderTexture.Texture;

        Id = AssetManager._id++;
        Size = _texture.Size;
        Width = (int)Size.X;
        Height = (int)Size.Y;

        LastAccessTime = DateTime.Now;
        Type = AssetType.Atlas;
        IsValid = true;
    }

    



    ~Texture() => Dispose();






    public void Load()
    {
        if (Type == AssetType.None)
            throw new InvalidOperationException($"Texture type is '{Type}'.");

        if (Type == AssetType.Instanced || Type == AssetType.Atlas || IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        _image = new SFImage(Data);
        _texture = new SFTexture(_image);

        Size = _texture.Size;
        Width = (int)_texture.Size.X;
        Height = (int)_texture.Size.Y;

        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    public void Unload()
    {
        // Instance textures cannot be unloaded by
        // the asset manager

        if (Type == AssetType.Instanced || Type == AssetType.Atlas || !IsValid)
            return;

        _image?.Dispose();
        _texture?.Dispose();
    }

    public void Dispose()
    {
        _image?.Dispose();
        _texture?.Dispose();

        GC.SuppressFinalize(this);
    }

    public void CopyTo(SFRenderTexture target, Rect2 srcRect, Vect2 destination)
    {
        if (!IsValid || target == null)
            return;

        var subImage = new SFImage(
            (uint)srcRect.Width,
            (uint)srcRect.Height,
            Color.Transparent
        );

        for (int y = 0; y < srcRect.Height; y++)
        {
            for (int x = 0; x < srcRect.Width; x++)
            {
                int px = (int)(srcRect.Left + x);
                int py = (int)(srcRect.Top + y);
                var color = _image.GetPixel((uint)px, (uint)py);
                subImage.SetPixel((uint)x, (uint)y, color);
            }
        }

        target.Texture.Update(subImage, (uint)destination.X, (uint)destination.Y);
    }



    public static bool operator ==(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return false;
        if (a is null)
            return !(b is null);

        return a.Equals(b);
    }

    public static bool operator !=(in Texture a, in Texture b)
    {
        if (ReferenceEquals(a, b))
            return false;
        if (a is null)
            return !(b is null);

        return !a.Equals(b);
    }

    public bool Equals(Texture other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Id == other?.Id;
    }

    public override bool Equals(object obj)
        => obj is Texture other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Id);

    public override string ToString()
        => $"Texture({Id}, {Data.Length}, {Tag}, {IsValid})";


    public static implicit operator SFTexture(Texture v)
    {
        v.LastAccessTime = DateTime.Now;

        return v._texture;
    }
}
