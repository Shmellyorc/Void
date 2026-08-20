// ============================================================================
//  Color.cs
// ============================================================================
//  RGBA color structure with hex parsing, pre-defined colors, blending, and
//  arithmetic operations. Used throughout the engine for rendering.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents an RGBA color with byte channels for each component.
/// </summary>
/// <remarks>
/// <para>
/// This structure provides comprehensive color manipulation including hex string
/// parsing, arithmetic operations, blending, and implicit conversion to and
/// from SFML's color type.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Create from RGB values
/// var color1 = new Color(255, 128, 64);
/// 
/// // Create from hex
/// var color2 = new Color("#FF8040");
/// 
/// // Use pre-defined colors
/// var red = Color.Red;
/// var transparentRed = red.WithAlpha(0.5f);
/// 
/// // Blend colors
/// var blended = Color.Lerp(Color.Red, Color.Blue, 0.5f);
/// </code>
/// </para>
/// </remarks>
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
    /// <summary>
    /// Gets a fully transparent color with all channels set to zero.
    /// </summary>
    public static Color Transparent => _colorTransparent;

    /// <summary>
    /// Gets a white color with all channels set to 255.
    /// </summary>
    public static Color White => _colorWhite;

    /// <summary>
    /// Gets a black color with RGB set to 0 and alpha set to 255.
    /// </summary>
    public static Color Black => _colorBlack;

    /// <summary>
    /// Gets a red color with RGB set to 255, 0, 0 and alpha set to 255.
    /// </summary>
    public static Color Red => _colorRed;

    /// <summary>
    /// Gets a green color with RGB set to 0, 255, 0 and alpha set to 255.
    /// </summary>
    public static Color Green => _colorGreen;

    /// <summary>
    /// Gets a blue color with RGB set to 0, 0, 255 and alpha set to 255.
    /// </summary>
    public static Color Blue => _colorBlue;

    /// <summary>
    /// Gets a magenta color with RGB set to 255, 0, 255 and alpha set to 255.
    /// </summary>
    public static Color Magenta => _colorMagenta;

    /// <summary>
    /// Gets a cyan color with RGB set to 0, 255, 255 and alpha set to 255.
    /// </summary>
    public static Color Cyan => _colorCyan;

    /// <summary>
    /// Gets a yellow color with RGB set to 255, 255, 0 and alpha set to 255.
    /// </summary>
    public static Color Yellow => _colorYellow;

    /// <summary>
    /// Gets an orange color with RGB set to 255, 165, 0 and alpha set to 255.
    /// </summary>
    public static Color Orange => _colorOrange;

    /// <summary>
    /// Gets a purple color with RGB set to 128, 0, 128 and alpha set to 255.
    /// </summary>
    public static Color Purple => _colorPurple;

    /// <summary>
    /// Gets a pink color with RGB set to 255, 192, 203 and alpha set to 255.
    /// </summary>
    public static Color Pink => _colorPink;

    /// <summary>
    /// Gets a brown color with RGB set to 165, 42, 42 and alpha set to 255.
    /// </summary>
    public static Color Brown => _colorBrown;

    /// <summary>
    /// Gets a gray color with RGB set to 128, 128, 128 and alpha set to 255.
    /// </summary>
    public static Color Gray => _colorGray;

    /// <summary>
    /// Gets a dark gray color with RGB set to 64, 64, 64 and alpha set to 255.
    /// </summary>
    public static Color DarkGray => _colorDarkGray;

    /// <summary>
    /// Gets a light gray color with RGB set to 192, 192, 192 and alpha set to 255.
    /// </summary>
    public static Color LightGray => _colorLightGray;

    /// <summary>
    /// Gets a gold color with RGB set to 255, 215, 0 and alpha set to 255.
    /// </summary>
    public static Color Gold => _colorGold;

    /// <summary>
    /// Gets a silver color with RGB set to 192, 192, 192 and alpha set to 255.
    /// </summary>
    public static Color Silver => _colorSilver;

    /// <summary>
    /// Gets a navy color with RGB set to 0, 0, 128 and alpha set to 255.
    /// </summary>
    public static Color Navy => _colorNavy;

    /// <summary>
    /// Gets an olive color with RGB set to 128, 128, 0 and alpha set to 255.
    /// </summary>
    public static Color Olive => _colorOlive;

    /// <summary>
    /// Gets a teal color with RGB set to 0, 128, 128 and alpha set to 255.
    /// </summary>
    public static Color Teal => _colorTeal;

    /// <summary>
    /// Gets an aqua color with RGB set to 0, 255, 255 and alpha set to 255.
    /// </summary>
    public static Color Aqua => _colorAqua;

    /// <summary>
    /// Gets a coral color with RGB set to 255, 127, 80 and alpha set to 255.
    /// </summary>
    public static Color Coral => _colorCoral;

    /// <summary>
    /// Gets a crimson color with RGB set to 220, 20, 60 and alpha set to 255.
    /// </summary>
    public static Color Crimson => _colorCrimson;

    /// <summary>
    /// Gets a dark blue color with RGB set to 0, 0, 139 and alpha set to 255.
    /// </summary>
    public static Color DarkBlue => _colorDarkBlue;

    /// <summary>
    /// Gets a dark green color with RGB set to 0, 100, 0 and alpha set to 255.
    /// </summary>
    public static Color DarkGreen => _colorDarkGreen;

    /// <summary>
    /// Gets a dark red color with RGB set to 139, 0, 0 and alpha set to 255.
    /// </summary>
    public static Color DarkRed => _colorDarkRed;

    /// <summary>
    /// Gets or sets the red component of the color with a value between 0 and 255.
    /// </summary>
    public byte R;

    /// <summary>
    /// Gets or sets the green component of the color with a value between 0 and 255.
    /// </summary>
    public byte G;

    /// <summary>
    /// Gets or sets the blue component of the color with a value between 0 and 255.
    /// </summary>
    public byte B;

    /// <summary>
    /// Gets or sets the alpha component of the color with a value between 0 and 255.
    /// </summary>
    public byte A;

    /// <summary>
    /// Gets a value indicating whether all color channels are set to zero.
    /// </summary>
    public readonly bool IsEmpty => R == 0 && G == 0 && B == 0 && A == 0;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified byte values for each channel.
    /// </summary>
    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified RGB byte values and an alpha of 255.
    /// </summary>
    public Color(byte r, byte g, byte b) : this(r, g, b, (byte)255) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified float values between 0 and 1 for each channel.
    /// </summary>
    public Color(float r, float g, float b, float a) : this(
        (byte)Math.Clamp(r * 255f, 0f, 255f),
        (byte)Math.Clamp(g * 255f, 0f, 255f),
        (byte)Math.Clamp(b * 255f, 0f, 255f),
        (byte)Math.Clamp(a * 255f, 0f, 255f)
    )
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified RGB float values between 0 and 1 and an alpha of 1.0.
    /// </summary>
    public Color(float r, float g, float b) : this(r, g, b, 1.0f) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified integer values between 0 and 255 for each channel.
    /// </summary>
    public Color(int r, int g, int b, int a) : this(
        (byte)Math.Clamp(r, 0, 255),
        (byte)Math.Clamp(g, 0, 255),
        (byte)Math.Clamp(b, 0, 255),
        (byte)Math.Clamp(a, 0, 255)
    )
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure with the specified RGB integer values between 0 and 255 and an alpha of 255.
    /// </summary>
    public Color(int r, int g, int b) : this(r, g, b, 255) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> structure by parsing a hex string.
    /// </summary>
    /// <param name="hex">The hex string to parse. Supports formats: #AARRGGBB, #RRGGBB, #ARGB, #RGB (with or without #).</param>
    /// <exception cref="InvalidDataContractException">Thrown when the hex string format is not recognized.</exception>
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
    /// <summary>
    /// Linearly interpolates between this color and the specified target color using the given interpolation factor.
    /// </summary>
    public readonly Color Lerp(in Color target, float t) => Lerp(this, target, t);

    /// <summary>
    /// Linearly interpolates between two colors using the given interpolation factor.
    /// </summary>
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
    /// <summary>
    /// Returns a new color with the same RGB values but with the specified alpha value between 0 and 1.
    /// </summary>
    public readonly Color WithAlpha(float alpha) => WithAlpha(this, alpha);

    /// <summary>
    /// Returns a new color with the same RGB values but with the specified alpha value between 0 and 255.
    /// </summary>
    public readonly Color WithAlpha(byte alpha) => WithAlpha(this, alpha);

    /// <summary>
    /// Returns a new color with the same RGB values but with the specified alpha value between 0 and 1.
    /// </summary>
    public static Color WithAlpha(in Color color, float alpha)
        => new(color.R, color.G, color.B, (byte)Math.Clamp(alpha * 255f, 0f, 255f));

    /// <summary>
    /// Returns a new color with the same RGB values but with the specified alpha value between 0 and 255.
    /// </summary>
    public static Color WithAlpha(in Color color, byte alpha)
        => new(color.R, color.G, color.B, alpha);
    #endregion

    #region Operators
    /// <summary>
    /// Determines whether two specified colors have the same value.
    /// </summary>
    public static bool operator ==(in Color a, in Color b) => a.Equals(b);

    /// <summary>
    /// Determines whether two specified colors have different values.
    /// </summary>
    public static bool operator !=(in Color a, in Color b) => !a.Equals(b);

    /// <summary>
    /// Scales the specified color by a scalar value, saturating each channel.
    /// </summary>
    public static Color operator *(float scalar, in Color color) => color * scalar;

    /// <summary>
    /// Scales the specified color by a scalar value, saturating each channel.
    /// </summary>
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

    /// <summary>
    /// Multiplies two colors component-wise and returns the result.
    /// </summary>
    public static Color operator *(in Color a, in Color b)
        => new(
            (byte)(a.R * b.R / 255f),
            (byte)(a.G * b.G / 255f),
            (byte)(a.B * b.B / 255f),
            (byte)(a.A * b.A / 255f)
        );

    /// <summary>
    /// Adds two colors component-wise and clamps the result to 255.
    /// </summary>
    public static Color operator +(in Color a, in Color b)
        => new(
            (byte)Math.Min(a.R + b.R, 255),
            (byte)Math.Min(a.G + b.G, 255),
            (byte)Math.Min(a.B + b.B, 255),
            (byte)Math.Min(a.A + b.A, 255)
        );

    /// <summary>
    /// Subtracts two colors component-wise and clamps the result to 0.
    /// </summary>
    public static Color operator -(in Color a, in Color b)
        => new(
            (byte)Math.Max(a.R - b.R, 0),
            (byte)Math.Max(a.G - b.G, 0),
            (byte)Math.Max(a.B - b.B, 0),
            (byte)Math.Max(a.A - b.A, 0)
        );

    /// <summary>
    /// Implicitly converts an SFML color to a Void Engine color.
    /// </summary>
    public static implicit operator Color(in SFColor v) => new(v.R, v.G, v.B, v.A);

    /// <summary>
    /// Implicitly converts a Void Engine color to an SFML color.
    /// </summary>
    public static implicit operator SFColor(in Color v) => new(v.R, v.G, v.B, v.A);
    #endregion

    #region IEquatable
    /// <summary>
    /// Determines whether the current color is equal to another color.
    /// </summary>
    public readonly bool Equals(Color other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <summary>
    /// Determines whether the current color is equal to the specified object.
    /// </summary>
    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Color value && Equals(value);

    /// <summary>
    /// Returns the hash code for the current color.
    /// </summary>
    public readonly override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    /// <summary>
    /// Returns a string representation of the current color.
    /// </summary>
    public readonly override string ToString()
        => $"Color({R}, {G}, {B}, {A})";
    #endregion
}