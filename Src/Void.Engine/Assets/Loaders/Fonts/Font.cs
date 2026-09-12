// ============================================================================
//  Font.cs
// ============================================================================
//  Abstract font asset base for renderer-neutral glyph metrics and atlas data.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Provides the base contract for font assets that expose glyph metrics and a
/// renderer-neutral RGBA glyph atlas.
/// </summary>
/// <remarks>
/// <para>
/// Derived font types provide glyph lookup through <see cref="GetGlyph"/> and
/// report their line height through <see cref="LineHeight"/>. Bitmap-like fonts
/// can decode their source image with <see cref="EnsureTexturePixels"/>, while
/// generated font implementations can provide their own atlas with
/// <see cref="SetTexturePixels"/>.
/// </para>
/// <para>
/// The font owns renderer-neutral atlas pixels. VOID creates backend-specific
/// texture resources from those pixels when a renderer needs them.
/// </para>
/// <code>
/// AssetManager.Instance.RegisterAssetType&lt;MyFont&gt;(
///     [".myfont"],
///     (id, data, tag) =&gt; new MyFont(id, data, tag));
/// </code>
/// </remarks>
public abstract class Font : IAsset
{
    /// <summary>
    /// Stores the glyphs exposed by the font.
    /// </summary>
    protected Glyph[] _glyphs = Array.Empty<Glyph>();

    /// <summary>
    /// Stores the first character represented by sequential glyph data.
    /// </summary>
    protected int _firstCharacter = 32;

    /// <summary>
    /// Stores the number of characters represented by the font.
    /// </summary>
    protected int _characterCount;

    private byte[] _texturePixels;
    private int _texturePixelWidth;
    private int _texturePixelHeight;
    private IGraphicsDevice _graphicsDevice;
    private IGraphicsTexture _graphicsTexture;

    /// <summary>
    /// Gets the unique asset identifier.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the asset tag or source path.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the original encoded font or source bytes retained for reloading.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets whether the font is currently loaded and ready for use.
    /// </summary>
    public bool IsValid { get; protected set; }

    /// <summary>
    /// Gets or sets the last time the font was accessed.
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets the asset classification for this font.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets the base height of one line of text, in pixels.
    /// </summary>
    public abstract float LineHeight { get; }

    /// <summary>
    /// Gets the additional vertical spacing applied between measured lines.
    /// </summary>
    public float LineSpacing { get; protected internal set; }

    /// <summary>
    /// Gets the additional horizontal spacing used by font implementations when
    /// calculating glyph advance.
    /// </summary>
    public float Spacing { get; protected internal set; }

    /// <summary>
    /// Gets the width of the renderer-neutral glyph atlas, in pixels.
    /// </summary>
    protected int TexturePixelWidth => _texturePixelWidth;

    /// <summary>
    /// Gets the height of the renderer-neutral glyph atlas, in pixels.
    /// </summary>
    protected int TexturePixelHeight => _texturePixelHeight;

    /// <summary>
    /// Gets the RGBA8 pixels that back the renderer-neutral glyph atlas.
    /// </summary>
    protected ReadOnlySpan<byte> TexturePixels => _texturePixels;

    /// <summary>
    /// Gets whether renderer-neutral glyph atlas pixels have been initialized.
    /// </summary>
    protected bool HasTexturePixels => _texturePixels is { Length: > 0 };

    /// <summary>
    /// Initializes a font asset.
    /// </summary>
    /// <param name="id">The unique asset identifier.</param>
    /// <param name="data">The encoded source bytes retained by the asset.</param>
    /// <param name="tag">The asset tag or source path.</param>
    /// <param name="type">The asset classification.</param>
    protected Font(uint id, byte[] data, string tag, AssetType type = AssetType.Normal)
    {
        Id = id;
        Data = data ?? Array.Empty<byte>();
        Tag = tag;
        Type = type;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Marks the font as recently accessed.
    /// </summary>
    /// <remarks>
    /// Derived implementations should create their glyph and atlas data before
    /// calling the base implementation when loading is required.
    /// </remarks>
    public virtual void Load() => LastAccessTime = DateTime.Now;

    /// <summary>
    /// Releases renderer resources and renderer-neutral atlas pixels owned by the font.
    /// </summary>
    public virtual void Unload()
    {
        if (!IsValid && _graphicsTexture == null)
            return;

        ReleaseGraphicsTexture();
        ClearTexturePixels();

        IsValid = false;
    }

    /// <summary>
    /// Releases resources owned by the font.
    /// </summary>
    public virtual void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the glyph used to render the specified character.
    /// </summary>
    /// <param name="c">The character to resolve.</param>
    /// <returns>The glyph associated with <paramref name="c"/>.</returns>
    public abstract Glyph GetGlyph(char c);

    /// <summary>
    /// Measures the width and height required to render the specified text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>
    /// The measured text size. Empty or null text returns <see cref="Vect2.Zero"/>.
    /// </returns>
    /// <remarks>
    /// Carriage returns are ignored. Newline characters start a new line, and
    /// each measured line uses <see cref="LineHeight"/> plus <see cref="LineSpacing"/>.
    /// </remarks>
    public Vect2 Measure(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vect2.Zero;

        float maxLineWidth = 0;
        float currentLineWidth = 0;
        int lineCount = 1;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                lineCount++;
                if (currentLineWidth > maxLineWidth)
                    maxLineWidth = currentLineWidth;
                currentLineWidth = 0;
                continue;
            }

            if (c == '\r')
                continue;

            currentLineWidth += GetGlyph(c).Advance;
        }

        if (currentLineWidth > maxLineWidth)
            maxLineWidth = currentLineWidth;

        float lineHeight = LineHeight + LineSpacing;
        return new Vect2(maxLineWidth, lineCount * lineHeight);
    }

    /// <summary>
    /// Calculates a line height from the tallest glyph currently stored by the font.
    /// </summary>
    /// <returns>The tallest glyph height, or 16 when no glyph has a positive height.</returns>
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
    /// Sets the renderer-neutral RGBA8 glyph atlas used to create backend textures.
    /// </summary>
    /// <param name="width">The atlas width in pixels.</param>
    /// <param name="height">The atlas height in pixels.</param>
    /// <param name="rgbaPixels">The tightly packed RGBA8 pixel data.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="width"/> or <paramref name="height"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rgbaPixels"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the pixel buffer length does not equal width multiplied by height multiplied by four.
    /// </exception>
    protected void SetTexturePixels(int width, int height, byte[] rgbaPixels)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentNullException.ThrowIfNull(rgbaPixels);

        int expectedLength = checked(width * height * 4);
        if (rgbaPixels.Length != expectedLength)
            throw new ArgumentException($"RGBA font atlas requires exactly {expectedLength} bytes.", nameof(rgbaPixels));

        if (_graphicsTexture != null)
            ReleaseGraphicsTexture();

        _texturePixelWidth = width;
        _texturePixelHeight = height;
        _texturePixels = rgbaPixels;
    }

    /// <summary>
    /// Ensures the font has renderer-neutral RGBA8 atlas pixels.
    /// </summary>
    /// <remarks>
    /// When no atlas has been supplied with <see cref="SetTexturePixels"/>, the
    /// method attempts to decode <see cref="Data"/> as an image. Generated font
    /// implementations should provide their own atlas before calling this method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no source data is available or the source data cannot be decoded.
    /// </exception>
    protected void EnsureTexturePixels()
    {
        if (HasTexturePixels)
            return;

        if (Data.Length == 0)
            throw new InvalidOperationException(
                $"Font '{Tag ?? GetType().Name}' has no source image and has not supplied texture pixels.");

        try
        {
            DecodedImage decoded = ImageDecoder.DecodeRgba(Data);
            SetTexturePixels(decoded.Width, decoded.Height, decoded.Pixels);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Font '{Tag ?? GetType().Name}' could not build a glyph atlas from Data. " +
                "Custom font implementations should call SetTexturePixels(width, height, rgbaPixels) from Load().",
                ex);
        }
    }

    /// <summary>
    /// Reads one pixel from the renderer-neutral glyph atlas.
    /// </summary>
    /// <param name="x">The zero-based horizontal pixel coordinate.</param>
    /// <param name="y">The zero-based vertical pixel coordinate.</param>
    /// <returns>The RGBA color stored at the requested coordinate.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when atlas pixels have not been initialized.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either coordinate falls outside the atlas.
    /// </exception>
    protected Color GetTexturePixel(int x, int y)
    {
        if (!HasTexturePixels)
            throw new InvalidOperationException("Font texture pixels have not been initialized.");
        if ((uint)x >= (uint)_texturePixelWidth || (uint)y >= (uint)_texturePixelHeight)
            throw new ArgumentOutOfRangeException(x < 0 || x >= _texturePixelWidth ? nameof(x) : nameof(y));

        int index = checked((y * _texturePixelWidth + x) * 4);
        return new Color(
            _texturePixels[index],
            _texturePixels[index + 1],
            _texturePixels[index + 2],
            _texturePixels[index + 3]);
    }

    internal bool TryCopyPixelRegion(Rect2 sourceRect, out byte[] pixels, out int width, out int height)
    {
        LastAccessTime = DateTime.Now;

        if (!IsValid)
            Load();

        EnsureTexturePixels();
        return RgbaPixelBuffer.TryCopyRegion(
            _texturePixels,
            _texturePixelWidth,
            _texturePixelHeight,
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

        if (!IsValid)
            Load();

        if (_graphicsTexture != null && !ReferenceEquals(activeDevice, _graphicsDevice))
            ReleaseGraphicsTexture();

        if (_graphicsTexture == null)
        {
            EnsureTexturePixels();

            var description = new TextureDescription(
                _texturePixelWidth,
                _texturePixelHeight,
                TextureFormat.RGBA8,
                TextureUsage.Sampled | TextureUsage.TransferDestination,
                TextureFilter.Nearest,
                TextureFilter.Nearest,
                TextureWrap.ClampToEdge,
                TextureWrap.ClampToEdge);

            _graphicsDevice = activeDevice;
            _graphicsTexture = activeDevice.CreateTexture(description, _texturePixels);
        }

        texture = _graphicsTexture;
        return true;
    }

    private void ClearTexturePixels()
    {
        _texturePixels = null;
        _texturePixelWidth = 0;
        _texturePixelHeight = 0;
    }

    private void ReleaseGraphicsTexture()
    {
        _graphicsTexture?.Dispose();
        _graphicsTexture = null;
        _graphicsDevice = null;
    }
}
