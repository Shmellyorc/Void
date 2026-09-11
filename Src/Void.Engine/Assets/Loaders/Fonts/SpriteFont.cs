// ============================================================================
//  SpriteFont.cs
// ============================================================================
//  Built-in bitmap font. Glyph extraction and GPU backing are fully renderer-neutral.
// ============================================================================

namespace Void.Engine.Assets.Loaders.Fonts;

public sealed class SpriteFont : Font, IAsset
{
    public const string CharsetFull = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
    public const string CharsetNumbers = "0123456789";
    public const string CharsetUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string CharsetLowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string CharsetLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string CharsetAlphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string CharsetHex = "0123456789ABCDEF";

    private readonly string _charset;

    public override float LineHeight => GetActualLineHeight();

    internal SpriteFont(uint id, byte[] data, string tag, string charset = null, float lineSpacing = 0f, float spacing = 0f)
        : base(id, data, tag, AssetType.Normal)
    {
        _charset = charset ?? CharsetFull;
        LineSpacing = lineSpacing;
        Spacing = spacing;
    }

    public override void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        // Decode once into renderer-neutral RGBA data. This is the data future
        // OpenGL/Vulkan/Direct3D backends consume.
        EnsureTexturePixels();

        if (_glyphs.IsEmpty())
        {
            _glyphs = ExtractGlyphs();
            _firstCharacter = _charset[0];
            _characterCount = _glyphs.Length;
        }

        IsValid = true;
        base.Load();
    }

    public override void Dispose()
    {
        _glyphs = Array.Empty<Glyph>();
        base.Dispose();
    }

    private Glyph[] ExtractGlyphs()
    {
        EnsureTexturePixels();

        int width = TexturePixelWidth;
        int height = TexturePixelHeight;
        Color ignoreColor = GetTexturePixel(0, 0);

        bool[,] processed = new bool[width, height];
        var glyphs = new List<Glyph>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (processed[x, y])
                    continue;

                Color pixel = GetTexturePixel(x, y);
                if (pixel == ignoreColor)
                    continue;

                var (bounds, _) = FloodFill(x, y, ignoreColor, processed);

                var glyph = new Glyph
                {
                    Position = bounds.Position,
                    Size = bounds.Size,
                    Offset = Vect2.Zero,
                    Advance = bounds.Width + Spacing
                };

                glyphs.Add(glyph);

                if (glyphs.Count > _charset.Length)
                {
                    throw new InvalidOperationException(
                        $"Font has more glyphs ({glyphs.Count}) than charset provides ({_charset.Length}). " +
                        $"Discovered character #{glyphs.Count - 1} at position ({bounds.X}, {bounds.Y})");
                }
            }
        }

        if (glyphs.Count < _charset.Length)
        {
            throw new InvalidOperationException(
                $"Font has fewer glyphs ({glyphs.Count}) than charset requires ({_charset.Length}).");
        }

        return glyphs.ToArray();
    }

    private (Rect2 bounds, List<Vect2> pixels) FloodFill(int startX, int startY, Color ignoreColor, bool[,] processed)
    {
        int width = TexturePixelWidth;
        int height = TexturePixelHeight;

        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;
        var pixels = new List<Vect2>();
        var stack = new Stack<(int x, int y)>();
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (x < 0 || y < 0 || x >= width || y >= height)
                continue;
            if (processed[x, y])
                continue;

            Color pixel = GetTexturePixel(x, y);
            if (pixel == ignoreColor)
                continue;

            processed[x, y] = true;
            pixels.Add(new Vect2(x, y));

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

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

    private float GetActualLineHeight()
    {
        float maxHeight = 0;
        foreach (var glyph in _glyphs)
        {
            if (glyph.Size.Y > maxHeight)
                maxHeight = glyph.Size.Y;
        }

        return maxHeight > 0 ? maxHeight : TexturePixelHeight;
    }
}
