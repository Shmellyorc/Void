// ============================================================================
//  SpriteFont.cs
// ============================================================================
//  A bitmap font implementation that extracts glyphs from a texture atlas
//  using flood-fill character detection.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// A bitmap font implementation that extracts glyphs from a texture atlas
/// using flood-fill character detection.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SpriteFont"/> class implements a bitmap font system where
/// characters are extracted from a texture atlas using flood-fill to detect
/// individual glyphs. The font image should contain characters arranged in
/// order matching the provided charset.
/// </para>
/// <para>
/// <b>Character Sets:</b>
/// <list type="bullet">
///   <item><description><see cref="CharsetFull"/> - Complete ASCII printable characters</description></item>
///   <item><description><see cref="CharsetNumbers"/> - Digits 0-9</description></item>
///   <item><description><see cref="CharsetUppercase"/> - Uppercase letters A-Z</description></item>
///   <item><description><see cref="CharsetLowercase"/> - Lowercase letters a-z</description></item>
///   <item><description><see cref="CharsetLetters"/> - All letters A-Z and a-z</description></item>
///   <item><description><see cref="CharsetAlphanumeric"/> - Letters and numbers</description></item>
///   <item><description><see cref="CharsetHex"/> - Hexadecimal characters</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Loading Process:</b>
/// <list type="number">
///   <item><description>Image is loaded from raw data</description></item>
///   <item><description>The pixel at (0,0) is used as the ignore/background color</description></item>
///   <item><description>Flood-fill scans the image to find each glyph</description></item>
///   <item><description>Each glyph's bounding box is extracted</description></item>
///   <item><description>Glyphs are stored in the order they appear in the charset</description></item>
///   <item><description>Validation ensures the font has exactly the expected number of glyphs</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a font with default full charset
/// var font = AssetManager.Instance.Load&lt;SpriteFont&gt;("fonts/arial.png");
/// 
/// // Load a font with a custom charset (fewer glyphs)
/// var font = AssetManager.Instance.LoadSpriteFont(
///     "fonts/numbers.png",
///     spacing: 0f,
///     lineSpacing: 0f,
///     charset: SpriteFont.CharsetNumbers
/// );
/// 
/// // Measure text
/// Vect2 size = font.Measure("Hello World");
/// 
/// // Get glyph data
/// Glyph glyph = font.GetGlyph('A');
/// 
/// // Use in rendering
/// texture = font; // Implicit conversion
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class SpriteFont : Font, IAsset
{
    // Built-in character sets
    /// <summary>
    /// Complete ASCII printable character set.
    /// </summary>
    public const string CharsetFull = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    /// <summary>
    /// Numeric character set (0-9).
    /// </summary>
    public const string CharsetNumbers = "0123456789";

    /// <summary>
    /// Uppercase letter character set (A-Z).
    /// </summary>
    public const string CharsetUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>
    /// Lowercase letter character set (a-z).
    /// </summary>
    public const string CharsetLowercase = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// All letters character set (A-Z, a-z).
    /// </summary>
    public const string CharsetLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Alphanumeric character set (0-9, A-Z, a-z).
    /// </summary>
    public const string CharsetAlphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Hexadecimal character set (0-9, A-F).
    /// </summary>
    public const string CharsetHex = "0123456789ABCDEF";

    private readonly string _charset;

    /// <summary>
    /// Gets the line height of the font in pixels.
    /// </summary>
    public override float LineHeight => GetActualLineHeight();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteFont"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the asset.</param>
    /// <param name="data">The raw font data bytes.</param>
    /// <param name="tag">The normalized path or tag used to identify the asset.</param>
    /// <param name="charset">The character set for glyph ordering. Defaults to <see cref="CharsetFull"/>.</param>
    /// <param name="lineSpacing">Additional space between lines.</param>
    /// <param name="spacing">Additional space between characters.</param>
    internal SpriteFont(uint id, byte[] data, string tag, string charset = null, float lineSpacing = 0f, float spacing = 0f)
        : base(id, data, tag, AssetType.Normal)
    {
        _charset = charset ?? CharsetFull;
        LineSpacing = lineSpacing;
        Spacing = spacing;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Loads the font data by parsing the bitmap image and extracting glyphs.
    /// </summary>
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

    /// <summary>
    /// Disposes the font and releases all resources.
    /// </summary>
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

        Color ignoreColor = _image.GetPixel(new(0, 0));

        bool[,] processed = new bool[width, height];
        var glyphs = new List<Glyph>();

        for (uint y = 0; y < height; y++)
        {
            for (uint x = 0; x < width; x++)
            {
                if (processed[x, y])
                    continue;

                Color pixel = _image.GetPixel(new(x, y));
                if (pixel == ignoreColor)
                    continue;

                var (bounds, pixels) = FloodFill(x, y, ignoreColor, processed);

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

            Color pixel = _image.GetPixel(new(x, y));
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
    /// Gets the glyph data for a specific character.
    /// </summary>
    /// <param name="c">The character to get the glyph for.</param>
    /// <returns>The <see cref="Glyph"/> data for the specified character, or the first glyph if the character is not found.</returns>
    public override Glyph GetGlyph(char c)
    {
        int index = _charset.IndexOf(c);
        if (index < 0 || index >= _glyphs.Length)
            return _glyphs.Length > 0 ? _glyphs[0] : default;
        return _glyphs[index];
    }

    /// <summary>
    /// Implicitly converts a sprite font to an SFML texture.
    /// </summary>
    public static implicit operator SFTexture(SpriteFont font)
        => font._texture;

    private float GetActualLineHeight()
    {
        float maxHeight = 0;
        foreach (var glyph in _glyphs)
        {
            if (glyph.Size.Y > maxHeight)
                maxHeight = glyph.Size.Y;
        }
        return maxHeight > 0 ? maxHeight : (_image?.Size.Y ?? 0);
    }
}