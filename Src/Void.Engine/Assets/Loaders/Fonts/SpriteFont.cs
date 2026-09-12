// ============================================================================
//  SpriteFont.cs
// ============================================================================
//  Built-in bitmap font backed by a renderer-neutral RGBA glyph atlas.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Provides VOID's bitmap sprite-font implementation.
/// </summary>
/// <remarks>
/// <para>
/// A sprite font treats the color of the source image's top-left pixel as the
/// background color. Connected non-background regions are discovered as glyphs
/// and mapped to characters in charset order.
/// </para>
/// <para>
/// Use <see cref="AssetManager.LoadSpriteFont"/> when custom character sets,
/// glyph spacing, or line spacing are required.
/// </para>
/// <code>
/// SpriteFont font = AssetManager.Instance.LoadSpriteFont(
///     "Fonts/ui.png",
///     spacing: 1f,
///     lineSpacing: 2f,
///     charset: SpriteFont.CharsetFull);
///
/// Vect2 size = font.Measure("Score: 100");
/// </code>
/// </remarks>
public sealed class SpriteFont : Font, IAsset
{
    /// <summary>
    /// Contains printable ASCII characters from space through tilde.
    /// </summary>
    public const string CharsetFull = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    /// <summary>
    /// Contains the decimal digits 0 through 9.
    /// </summary>
    public const string CharsetNumbers = "0123456789";

    /// <summary>
    /// Contains uppercase English letters A through Z.
    /// </summary>
    public const string CharsetUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Contains lowercase English letters a through z.
    /// </summary>
    public const string CharsetLowercase = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Contains uppercase followed by lowercase English letters.
    /// </summary>
    public const string CharsetLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Contains decimal digits followed by uppercase and lowercase English letters.
    /// </summary>
    public const string CharsetAlphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Contains uppercase hexadecimal characters 0 through 9 and A through F.
    /// </summary>
    public const string CharsetHex = "0123456789ABCDEF";

    private readonly string _charset;

    /// <summary>
    /// Gets the height of the tallest extracted glyph, or the atlas height when no glyph height is available.
    /// </summary>
    public override float LineHeight => GetActualLineHeight();

    internal SpriteFont(uint id, byte[] data, string tag, string charset = null, float lineSpacing = 0f, float spacing = 0f)
        : base(id, data, tag, AssetType.Normal)
    {
        _charset = charset ?? CharsetFull;
        LineSpacing = lineSpacing;
        Spacing = spacing;
    }

    /// <summary>
    /// Loads the bitmap atlas and extracts glyph regions when needed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the number of discovered glyph regions does not match the configured charset.
    /// </exception>
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

    /// <summary>
    /// Releases the font and clears its extracted glyph data.
    /// </summary>
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

    /// <summary>
    /// Gets the glyph assigned to the specified character.
    /// </summary>
    /// <param name="c">The character to resolve.</param>
    /// <returns>
    /// The matching glyph, or the first glyph when the character is not present.
    /// Returns an empty glyph when no glyphs have been extracted.
    /// </returns>
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
