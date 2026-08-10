namespace Void.Engine.Systems;

public struct Color : IEquatable<Color>
{
    #region Fields
    private static readonly Color _colorTransparent = new(0, 0, 0, 0);
    private static readonly Color _colorWhite = new(255, 255, 255);
    private static readonly Color _colorBlack = new(0, 0, 0);
    private static readonly Color _colorRed = new(255, 0, 0);
    private static readonly Color _colorGreen = new(0, 255, 0);
    private static readonly Color _colorBlue = new(0, 0, 255);
    private static readonly Color _colorMagenta = new(255, 0, 255);
    private static readonly Color _colorCyan = new(0, 255, 255);
    private static readonly Color _colorYellow = new(255, 255, 0);
    private static readonly Color _colorOrange = new(255, 165, 0);
    private static readonly Color _colorPurple = new(128, 0, 128);
    private static readonly Color _colorPink = new(255, 192, 203);
    private static readonly Color _colorBrown = new(165, 42, 42);
    private static readonly Color _colorGray = new(128, 128, 128);
    private static readonly Color _colorDarkGray = new(64, 64, 64);
    private static readonly Color _colorLightGray = new(192, 192, 192);
    private static readonly Color _colorGold = new(255, 215, 0);
    private static readonly Color _colorSilver = new(192, 192, 192);
    private static readonly Color _colorNavy = new(0, 0, 128);
    private static readonly Color _colorOlive = new(128, 128, 0);
    private static readonly Color _colorTeal = new(0, 128, 128);
    private static readonly Color _colorAqua = new(0, 255, 255);
    private static readonly Color _colorCoral = new(255, 127, 80);
    private static readonly Color _colorCrimson = new(220, 20, 60);
    private static readonly Color _colorDarkBlue = new(0, 0, 139);
    private static readonly Color _colorDarkGreen = new(0, 100, 0);
    private static readonly Color _colorDarkRed = new(139, 0, 0);
    #endregion



    #region Properties
    // Pre-defined colors
    public static Color Transparent => _colorTransparent;
    public static Color Black => _colorBlack;
    public static Color White => _colorWhite;
    public static Color Red => _colorRed;
    public static Color Green => _colorGreen;
    public static Color Blue => _colorBlue;
    public static Color Magenta => _colorMagenta;
    public static Color Cyan => _colorCyan;
    public static Color Yellow => _colorYellow;
    public static Color Orange => _colorOrange;
    public static Color Purple => _colorPurple;
    public static Color Pink => _colorPink;
    public static Color Brown => _colorBrown;
    public static Color Gray => _colorGray;
    public static Color DarkGray => _colorDarkGray;
    public static Color LightGray => _colorLightGray;
    public static Color Gold => _colorGold;
    public static Color Silver => _colorSilver;
    public static Color Navy => _colorNavy;
    public static Color Olive => _colorOlive;
    public static Color Teal => _colorTeal;
    public static Color Aqua => _colorAqua;
    public static Color Coral => _colorCoral;
    public static Color Crimson => _colorCrimson;
    public static Color DarkBlue => _colorDarkBlue;
    public static Color DarkGreen => _colorDarkGreen;
    public static Color DarkRed => _colorDarkRed;

    public byte R, G, B, A;

    public readonly bool IsEmpty => R == 0 && G == 0 && B == 0 && A == 0;
    #endregion



    #region Constructors
    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    public Color(byte r, byte g, byte b) : this(r, g, b, (byte)255) { }

    public Color(float r, float g, float b, float a) : this(
        (byte)Math.Clamp(r * 255f, 0f, 255f),
        (byte)Math.Clamp(g * 255f, 0f, 255f),
        (byte)Math.Clamp(b * 255f, 0f, 255f),
        (byte)Math.Clamp(a * 255f, 0f, 255f)
    )
    { }
    public Color(float r, float g, float b) : this(r, g, b, 1.0f) { }


    public Color(int r, int g, int b, int a) : this(
        (byte)Math.Clamp(r, 0, 255),
        (byte)Math.Clamp(g, 0, 255),
        (byte)Math.Clamp(b, 0, 255),
        (byte)Math.Clamp(a, 0, 255)
    )
    { }
    public Color(int r, int g, int b) : this(r, g, b, 255) { }


    public Color(string hex)
    {
        var value = hex.TrimStart('#');

        if (value.Length == 8) // AARRGGBB
        {
            A = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber);
            R = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber);
            G = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber);
            B = byte.Parse(value.Substring(6, 2), NumberStyles.HexNumber);
        }
        else if (value.Length == 6) // RRGGBB
        {
            R = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber);
            G = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber);
            B = byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber);
            A = 255;
        }
        else if (value.Length == 4) // ARGB
        {
            var a = byte.Parse(value.Substring(0, 1), NumberStyles.HexNumber);
            var r = byte.Parse(value.Substring(1, 1), NumberStyles.HexNumber);
            var g = byte.Parse(value.Substring(2, 1), NumberStyles.HexNumber);
            var b = byte.Parse(value.Substring(3, 1), NumberStyles.HexNumber);

            R = (byte)(r * 17);
            G = (byte)(g * 17);
            B = (byte)(b * 17);
            A = (byte)(a * 17);
        }
        else if (value.Length == 3) // RGB
        {
            var r = byte.Parse(value.Substring(0, 1), NumberStyles.HexNumber);
            var g = byte.Parse(value.Substring(1, 1), NumberStyles.HexNumber);
            var b = byte.Parse(value.Substring(2, 1), NumberStyles.HexNumber);

            R = (byte)(r * 17);
            G = (byte)(g * 17);
            B = (byte)(b * 17);
            A = 255;
        }
        else
            throw new InvalidDataContractException(
                $"Unable to process hex color '{hex}'. Use with # or not, of AARRGGBB, RRGGBB, ARGB, or RGB"
            );
    }
    #endregion



    #region Lerp
    public readonly Color Lerp(in Color target, float t) => Lerp(this, target, t);
    public static Color Lerp(in Color a, in Color b, float t)
    {
        t = MathHelper.Saturate(t);
        return new Color(
            (byte)MathHelper.Lerp(a.R, b.R, t),
            (byte)MathHelper.Lerp(a.G, b.G, t),
            (byte)MathHelper.Lerp(a.B, b.B, t),
            (byte)MathHelper.Lerp(a.A, b.A, t)
        );
    }
    #endregion



    #region WithAlpha
    public readonly Color WithAlpha(float alpha) => WithAlpha(this, alpha);
    public readonly Color WithAlpha(byte alpha) => WithAlpha(this, alpha);
    public static Color WithAlpha(in Color color, float alpha)
        => new(color.R, color.G, color.B, (byte)Math.Clamp(alpha * 255f, 0f, 255f));
    public static Color WithAlpha(in Color color, byte alpha)
        => new(color.R, color.G, color.B, alpha);
    #endregion



    #region Operators
    public static bool operator ==(in Color a, in Color b) => a.Equals(b);
    public static bool operator !=(in Color a, in Color b) => !a.Equals(b);

    public static Color operator *(float scalar, in Color color) => color * scalar;
    public static Color operator *(in Color color, float scalar)
    {
        scalar = MathHelper.Saturate(scalar);
        return new Color(
            (byte)(color.R * scalar),
            (byte)(color.G * scalar),
            (byte)(color.B * scalar),
            (byte)(color.A * scalar)
        );
    }
    public static Color operator *(in Color a, in Color b)
        => new(
            (byte)(a.R * b.R / 255f),
            (byte)(a.G * b.G / 255f),
            (byte)(a.B * b.B / 255f),
            (byte)(a.A * b.A / 255f)
        );
    public static Color operator +(in Color a, in Color b)
        => new(
            (byte)Math.Min(a.R + b.R, 255),
            (byte)Math.Min(a.G + b.G, 255),
            (byte)Math.Min(a.B + b.B, 255),
            (byte)Math.Min(a.A + b.A, 255)
        );
    public static Color operator -(in Color a, in Color b)
        => new(
            (byte)Math.Max(a.R - b.R, 0),
            (byte)Math.Max(a.G - b.G, 0),
            (byte)Math.Max(a.B - b.B, 0),
            (byte)Math.Max(a.A - b.A, 0)
        );

    public static implicit operator Color(in SFColor v) => new(v.R, v.G, v.B, v.A);
    public static implicit operator SFColor(in Color v) => new(v.R, v.G, v.B, v.A);
    #endregion



    #region IEquatable
    public readonly bool Equals(Color other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Color value && Equals(value);

    public readonly override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    public readonly override string ToString()
        => $"Color({R}, {G}, {B}, {A})";
    #endregion
}