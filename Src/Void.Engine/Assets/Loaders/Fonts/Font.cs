namespace Void.Engine.Assets.Loaders.Fonts;

public abstract class Font : IAsset
{
    protected Glyph[] _glyphs = Array.Empty<Glyph>();
    protected int _firstCharacter = 32;
    protected int _characterCount = 0;
    protected SFTexture _texture;
    protected SFImage _image;

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public bool IsValid { get; protected set; }
    public DateTime LastAccessTime { get; protected set; }
    public AssetType Type { get; }
    public abstract float LineHeight { get; }
    public float LineSpacing { get; protected set; }
    public float Spacing { get; protected set; }

    protected Font(uint id, byte[] data, string tag, AssetType type = AssetType.Normal)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = type;
        LastAccessTime = DateTime.Now;
    }

    public virtual void Load() => LastAccessTime = DateTime.Now;
    public virtual void Unload()
    {
        if (!IsValid)
            return;

        _image?.Dispose();
        _texture?.Dispose();

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

            var glyph = GetGlyph(c);
            currentLineWidth += glyph.Advance + Spacing;
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

    public static implicit operator SFTexture(Font font)
    {
        font.LastAccessTime = DateTime.Now;

        return font._texture;
    }
}
