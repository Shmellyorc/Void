// ============================================================================
//  Font.cs
// ============================================================================
//  Abstract base class for font assets providing glyph access and text measurement.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Abstract base class for font assets providing glyph access and text measurement.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Font"/> class implements <see cref="IAsset"/> and provides
/// the core functionality for font assets including glyph retrieval and text
/// measurement. Derived classes implement specific font formats and rendering.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Glyph access via <see cref="GetGlyph"/></description></item>
///   <item><description>Text measurement with <see cref="Measure"/></description></item>
///   <item><description>Line height, line spacing, and character spacing control</description></item>
///   <item><description>Asset lifecycle management (load, unload, dispose)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a font (specific implementation)
/// var font = AssetManager.Instance.Load&lt;SpriteFont&gt;("fonts/arial.png");
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
public abstract class Font : IAsset
{
    /// <summary>
    /// Array of glyphs for the font's character set.
    /// </summary>
    protected Glyph[] _glyphs = Array.Empty<Glyph>();

    /// <summary>
    /// The ASCII code of the first character in the font's character set.
    /// </summary>
    /// <remarks>
    /// Default is 32 (space). This determines the starting index for the glyph array.
    /// </remarks>
    protected int _firstCharacter = 32;

    /// <summary>
    /// The total number of characters in the font's character set.
    /// </summary>
    protected int _characterCount = 0;

    /// <summary>
    /// The texture containing all glyphs for the font.
    /// </summary>
    protected SFTexture _texture;

    /// <summary>
    /// The image used to build the font texture.
    /// </summary>
    protected SFImage _image;

    /// <summary>
    /// Gets the unique identifier of the font.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the font.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the raw font data bytes.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets a value indicating whether the font is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; protected set; }

    /// <summary>
    /// Gets the last access time of the font for eviction tracking.
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets the asset type of the font.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets the line height of the font in pixels.
    /// </summary>
    public abstract float LineHeight { get; }

    /// <summary>
    /// Gets or sets the line spacing (additional space between lines).
    /// </summary>
    public float LineSpacing { get; protected set; }

    /// <summary>
    /// Gets or sets the character spacing (additional space between characters).
    /// </summary>
    public float Spacing { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Font"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the asset.</param>
    /// <param name="data">The raw font data bytes.</param>
    /// <param name="tag">The normalized path or tag used to identify the asset.</param>
    /// <param name="type">The asset type.</param>
    protected Font(uint id, byte[] data, string tag, AssetType type = AssetType.Normal)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = type;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Loads the font data into memory.
    /// </summary>
    public virtual void Load() => LastAccessTime = DateTime.Now;

    /// <summary>
    /// Unloads the font data from memory.
    /// </summary>
    public virtual void Unload()
    {
        if (!IsValid)
            return;

        _image?.Dispose();
        _texture?.Dispose();

        IsValid = false;
    }

    /// <summary>
    /// Disposes the font and releases all resources.
    /// </summary>
    public virtual void Dispose()
    {
        Unload();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the glyph data for a specific character.
    /// </summary>
    /// <param name="c">The character to get the glyph for.</param>
    /// <returns>The <see cref="Glyph"/> data for the specified character.</returns>
    public abstract Glyph GetGlyph(char c);

    /// <summary>
    /// Measures the dimensions of the specified text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>A <see cref="Vect2"/> containing the width and height of the text.</returns>
    public Vect2 Measure(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vect2.Zero;

        float maxLineWidth = 0;
        float currentLineWidth = 0;
        int lineCount = 1;
        bool hasCharOnLine = false;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                lineCount++;

                if (hasCharOnLine)
                    currentLineWidth -= Spacing;

                if (currentLineWidth > maxLineWidth)
                    maxLineWidth = currentLineWidth;

                currentLineWidth = 0;
                hasCharOnLine = false;
                continue;
            }

            if (c == '\r')
                continue;

            var glyph = GetGlyph(c);
            currentLineWidth += glyph.Advance;
            hasCharOnLine = true;
        }

        if (hasCharOnLine)
            currentLineWidth -= Spacing;

        if (currentLineWidth > maxLineWidth)
            maxLineWidth = currentLineWidth;

        float lineHeight = LineHeight + LineSpacing;
        return new Vect2(maxLineWidth, lineCount * lineHeight);
    }

    /// <summary>
    /// Calculates the line height from the glyph data.
    /// </summary>
    /// <returns>The maximum glyph height or a default value.</returns>
    protected virtual float GetLineHeight()
    {
        float maxHeight = 0;
        foreach (var glyph in _glyphs)
        {
            if (glyph.Size.Y > maxHeight)
                maxHeight = glyph.Size.Y;
        }
        return maxHeight > 0 ? maxHeight : 16f;
    }

    /// <summary>
    /// Implicitly converts a font to an SFML texture.
    /// </summary>
    public static implicit operator SFTexture(Font v)
    {
        v.LastAccessTime = DateTime.Now;

        if (v.Type == AssetType.Normal)
        {
            if (v._texture == null || !v.IsValid || v._texture.IsInvalid)
                v.Load();
        }

        return v._texture;
    }
}