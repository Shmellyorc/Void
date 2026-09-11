// ============================================================================
//  Font.cs
// ============================================================================
//  Abstract font asset. Custom font implementations provide glyph metrics and
//  may provide an RGBA glyph atlas through SetTexturePixels(). The renderer
//  creates its own backend texture from those pixels. No SFML resource is owned here.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Abstract base class for font assets providing glyph access, measurement and
/// renderer-neutral glyph-atlas backing.
/// </summary>
public abstract class Font : IAsset
{
    protected Glyph[] _glyphs = Array.Empty<Glyph>();
    protected int _firstCharacter = 32;
    protected int _characterCount;

    private byte[] _texturePixels;
    private int _texturePixelWidth;
    private int _texturePixelHeight;
    private IGraphicsDevice _graphicsDevice;
    private IGraphicsTexture _graphicsTexture;

    public uint Id { get; }
    public string Tag { get; }

    /// <summary>Original encoded font/source bytes retained for reloads.</summary>
    public byte[] Data { get; }

    public bool IsValid { get; protected set; }
    public DateTime LastAccessTime { get; set; }
    public AssetType Type { get; }
    public abstract float LineHeight { get; }
    public float LineSpacing { get; protected internal set; }
    public float Spacing { get; protected internal set; }

    /// <summary>Width of the renderer-neutral glyph atlas in pixels.</summary>
    protected int TexturePixelWidth => _texturePixelWidth;

    /// <summary>Height of the renderer-neutral glyph atlas in pixels.</summary>
    protected int TexturePixelHeight => _texturePixelHeight;

    /// <summary>RGBA8 pixels for the renderer-neutral glyph atlas.</summary>
    protected ReadOnlySpan<byte> TexturePixels => _texturePixels;

    /// <summary>Whether this font has supplied a renderer-neutral glyph atlas.</summary>
    protected bool HasTexturePixels => _texturePixels is { Length: > 0 };

    protected Font(uint id, byte[] data, string tag, AssetType type = AssetType.Normal)
    {
        Id = id;
        Data = data ?? Array.Empty<byte>();
        Tag = tag;
        Type = type;
        LastAccessTime = DateTime.Now;
    }

    public virtual void Load() => LastAccessTime = DateTime.Now;

    public virtual void Unload()
    {
        if (!IsValid && _graphicsTexture == null)
            return;

        ReleaseGraphicsTexture();
        ClearTexturePixels();

        IsValid = false;
    }

    public virtual void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    public abstract Glyph GetGlyph(char c);

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
    /// Supplies the RGBA8 glyph atlas used by renderer backends. Custom font
    /// implementations (TrueType, SDF, vector-to-atlas, etc.) can call this from
    /// their Load method after generating their atlas.
    /// </summary>
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
    /// Ensures bitmap-like font data has a renderer-neutral RGBA atlas. Custom
    /// font implementations that generate their own atlas should call
    /// SetTexturePixels before this is needed.
    /// </summary>
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

    /// <summary>Reads one RGBA pixel from the renderer-neutral glyph atlas.</summary>
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

    /// <summary>
    /// Copies a glyph-atlas region as tightly packed RGBA8 pixels for renderer-neutral atlasing.
    /// </summary>
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
