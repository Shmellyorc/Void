namespace Void.Engine.Assets.Loaders.Fonts;

public sealed class SpriteFont : Font, IAsset
{
    // Built-in character sets
    public const string CharsetFull = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
    public const string CharsetNumbers = "0123456789";
    public const string CharsetUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string CharsetLowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string CharsetLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string CharsetAlphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string CharsetHex = "0123456789ABCDEF";

    private readonly string _charset;

    public int LineSpacing { get; }
    public int Spacing { get; }

    internal SpriteFont(uint id, byte[] data, string tag, string charset = null, int lineSpacing = 1, int spacing = 0)
        : base(id, data, tag, AssetType.Normal)
    {
        _charset = charset ?? CharsetFull;
        LineSpacing = lineSpacing;
        Spacing = spacing;
        LastAccessTime = DateTime.Now;
    }

    public override void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        _image = new SFImage(Data);
        _texture = new SFTexture(_image);

        if (_glyphs.IsEmpty())
        {
            _glyphs = ExtractGlyphs();
            _firstCharacter = _charset[0];
            _characterCount = _glyphs.Length;
        }

        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    public override void Dispose()
    {
        _glyphs = Array.Empty<Glyph>();

        base.Dispose();
    }

    private Glyph[] ExtractGlyphs()
    {
        if (_image == null)
            throw new InvalidOperationException("Image not loaded.");

        uint width = _image.Size.X;
        uint height = _image.Size.Y;

        Color ignoreColor = _image.GetPixel(0, 0);

        bool[,] processed = new bool[width, height];
        var glyphs = new List<Glyph>();

        for (uint y = 0; y < height; y++)
        {
            for (uint x = 0; x < width; x++)
            {
                if (processed[x, y])
                    continue;

                Color pixel = _image.GetPixel(x, y);
                if (pixel == ignoreColor)
                    continue;

                // Found a character - floodfill to get bounds
                var (bounds, pixels) = FloodFill(x, y, ignoreColor, processed);

                // Create glyph from bounds
                var glyph = new Glyph
                {
                    Position = bounds.Position,
                    Size = bounds.Size,
                    Offset = Vect2.Zero,
                    Advance = (int)bounds.Width + Spacing
                };

                glyphs.Add(glyph);

                // Check if we exceeded charset length
                if (glyphs.Count > _charset.Length)
                {
                    throw new InvalidOperationException(
                        $"Font has more glyphs ({glyphs.Count}) than charset provides ({_charset.Length}). " +
                        $"Discovered character #{glyphs.Count - 1} at position ({bounds.X}, {bounds.Y})"
                    );
                }
            }
        }

        if (glyphs.Count < _charset.Length)
        {
            throw new InvalidOperationException(
                $"Font has fewer glyphs ({glyphs.Count}) than charset requires ({_charset.Length})."
            );
        }

        return glyphs.ToArray();
    }

    private (Rect2 bounds, List<Vect2> pixels) FloodFill(uint startX, uint startY, Color ignoreColor, bool[,] processed)
    {
        uint width = _image.Size.X;
        uint height = _image.Size.Y;

        uint minX = startX, maxX = startX;
        uint minY = startY, maxY = startY;
        var pixels = new List<Vect2>();
        var stack = new Stack<(uint x, uint y)>();
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (x >= width || y >= height)
                continue;
            if (processed[x, y])
                continue;

            Color pixel = _image.GetPixel(x, y);
            if (pixel == ignoreColor)
                continue;

            processed[x, y] = true;
            pixels.Add(new Vect2(x, y));

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

            // 4-directional floodfill
            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }

        return (
            new Rect2(minX, minY, maxX - minX + 1, maxY - minY + 1),
            pixels
        );
    }

    public override Glyph GetGlyph(char c)
    {
        int index = _charset.IndexOf(c);
        if (index < 0 || index >= _glyphs.Length)
            return _glyphs.Length > 0 ? _glyphs[0] : default;
        return _glyphs[index];
    }

    public static implicit operator SFTexture(SpriteFont font)
        => font._texture;
}