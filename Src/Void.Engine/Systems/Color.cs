// ============================================================================
//  Color.cs
// ============================================================================
//  RGBA color structure with hex parsing, predefined colors, interpolation,
//  alpha helpers, and component-wise arithmetic.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents an RGBA color using 8-bit red, green, blue, and alpha channels.
/// </summary>
/// <remarks>
/// <para>
/// Colors can be created from byte, integer, normalized floating-point, or hexadecimal values.
/// The type also provides predefined colors, interpolation, alpha replacement, and
/// component-wise arithmetic.
/// </para>
/// <para>
/// Example:
/// <code>
/// var orange = new Color(255, 128, 64);
/// var fromHex = new Color("#FF8040");
/// var faded = orange.WithAlpha(0.5f);
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
    /// <summary>Gets a fully transparent color with all channels set to zero.</summary>
    public static Color Transparent => _colorTransparent;

    /// <summary>Gets opaque white.</summary>
    public static Color White => _colorWhite;

    /// <summary>Gets opaque black.</summary>
    public static Color Black => _colorBlack;

    /// <summary>Gets opaque red.</summary>
    public static Color Red => _colorRed;

    /// <summary>Gets opaque green.</summary>
    public static Color Green => _colorGreen;

    /// <summary>Gets opaque blue.</summary>
    public static Color Blue => _colorBlue;

    /// <summary>Gets opaque magenta.</summary>
    public static Color Magenta => _colorMagenta;

    /// <summary>Gets opaque cyan.</summary>
    public static Color Cyan => _colorCyan;

    /// <summary>Gets opaque yellow.</summary>
    public static Color Yellow => _colorYellow;

    /// <summary>Gets opaque orange.</summary>
    public static Color Orange => _colorOrange;

    /// <summary>Gets opaque purple.</summary>
    public static Color Purple => _colorPurple;

    /// <summary>Gets opaque pink.</summary>
    public static Color Pink => _colorPink;

    /// <summary>Gets opaque brown.</summary>
    public static Color Brown => _colorBrown;

    /// <summary>Gets opaque gray.</summary>
    public static Color Gray => _colorGray;

    /// <summary>Gets opaque dark gray.</summary>
    public static Color DarkGray => _colorDarkGray;

    /// <summary>Gets opaque light gray.</summary>
    public static Color LightGray => _colorLightGray;

    /// <summary>Gets opaque gold.</summary>
    public static Color Gold => _colorGold;

    /// <summary>Gets opaque silver.</summary>
    public static Color Silver => _colorSilver;

    /// <summary>Gets opaque navy.</summary>
    public static Color Navy => _colorNavy;

    /// <summary>Gets opaque olive.</summary>
    public static Color Olive => _colorOlive;

    /// <summary>Gets opaque teal.</summary>
    public static Color Teal => _colorTeal;

    /// <summary>Gets opaque aqua.</summary>
    public static Color Aqua => _colorAqua;

    /// <summary>Gets opaque coral.</summary>
    public static Color Coral => _colorCoral;

    /// <summary>Gets opaque crimson.</summary>
    public static Color Crimson => _colorCrimson;

    /// <summary>Gets opaque dark blue.</summary>
    public static Color DarkBlue => _colorDarkBlue;

    /// <summary>Gets opaque dark green.</summary>
    public static Color DarkGreen => _colorDarkGreen;

    /// <summary>Gets opaque dark red.</summary>
    public static Color DarkRed => _colorDarkRed;

    /// <summary>Red channel in the range 0 to 255.</summary>
    public byte R;

    /// <summary>Green channel in the range 0 to 255.</summary>
    public byte G;

    /// <summary>Blue channel in the range 0 to 255.</summary>
    public byte B;

    /// <summary>Alpha channel in the range 0 to 255.</summary>
    public byte A;

    /// <summary>
    /// Gets whether all four channels are zero.
    /// </summary>
    public readonly bool IsEmpty => R == 0 && G == 0 && B == 0 && A == 0;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a color from byte channel values.
    /// </summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <param name="a">Alpha channel.</param>
    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates an opaque color from byte RGB values.
    /// </summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    public Color(byte r, byte g, byte b) : this(r, g, b, (byte)255) { }

    /// <summary>
    /// Creates a color from normalized floating-point channel values.
    /// </summary>
    /// <param name="r">Red channel, normally from 0 to 1.</param>
    /// <param name="g">Green channel, normally from 0 to 1.</param>
    /// <param name="b">Blue channel, normally from 0 to 1.</param>
    /// <param name="a">Alpha channel, normally from 0 to 1.</param>
    /// <remarks>Values outside the normalized range are clamped.</remarks>
    public Color(float r, float g, float b, float a) : this(
        (byte)Math.Clamp(r * 255f, 0f, 255f),
        (byte)Math.Clamp(g * 255f, 0f, 255f),
        (byte)Math.Clamp(b * 255f, 0f, 255f),
        (byte)Math.Clamp(a * 255f, 0f, 255f)
    )
    { }

    /// <summary>
    /// Creates an opaque color from normalized floating-point RGB values.
    /// </summary>
    /// <param name="r">Red channel, normally from 0 to 1.</param>
    /// <param name="g">Green channel, normally from 0 to 1.</param>
    /// <param name="b">Blue channel, normally from 0 to 1.</param>
    /// <remarks>Values outside the normalized range are clamped.</remarks>
    public Color(float r, float g, float b) : this(r, g, b, 1.0f) { }

    /// <summary>
    /// Creates a color from integer channel values.
    /// </summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <param name="a">Alpha channel.</param>
    /// <remarks>Each channel is clamped to the range 0 to 255.</remarks>
    public Color(int r, int g, int b, int a) : this(
        (byte)Math.Clamp(r, 0, 255),
        (byte)Math.Clamp(g, 0, 255),
        (byte)Math.Clamp(b, 0, 255),
        (byte)Math.Clamp(a, 0, 255)
    )
    { }

    /// <summary>
    /// Creates an opaque color from integer RGB values.
    /// </summary>
    /// <param name="r">Red channel.</param>
    /// <param name="g">Green channel.</param>
    /// <param name="b">Blue channel.</param>
    /// <remarks>Each channel is clamped to the range 0 to 255.</remarks>
    public Color(int r, int g, int b) : this(r, g, b, 255) { }

    /// <summary>
    /// Creates a color by parsing a hexadecimal color string.
    /// </summary>
    /// <param name="hex">
    /// Hexadecimal color in <c>AARRGGBB</c>, <c>RRGGBB</c>, <c>ARGB</c>, or
    /// <c>RGB</c> form, with or without a leading <c>#</c>.
    /// </param>
    /// <exception cref="InvalidDataContractException">
    /// Thrown when the hexadecimal string length does not match a supported format.
    /// </exception>
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
    /// Linearly interpolates from this color to a target color.
    /// </summary>
    /// <param name="target">Target color.</param>
    /// <param name="t">Interpolation factor. Values outside 0 to 1 are clamped.</param>
    /// <returns>The interpolated color.</returns>
    public readonly Color Lerp(in Color target, float t) => Lerp(this, target, t);

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="a">Start color.</param>
    /// <param name="b">End color.</param>
    /// <param name="t">Interpolation factor. Values outside 0 to 1 are clamped.</param>
    /// <returns>The interpolated color.</returns>
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
    /// Returns this color with a normalized alpha value.
    /// </summary>
    /// <param name="alpha">Alpha value, normally from 0 to 1.</param>
    /// <returns>A color with the same RGB channels and the requested alpha.</returns>
    public readonly Color WithAlpha(float alpha) => WithAlpha(this, alpha);

    /// <summary>
    /// Returns this color with the specified byte alpha value.
    /// </summary>
    /// <param name="alpha">Alpha channel.</param>
    /// <returns>A color with the same RGB channels and the requested alpha.</returns>
    public readonly Color WithAlpha(byte alpha) => WithAlpha(this, alpha);

    /// <summary>
    /// Returns a color with the same RGB channels and a normalized alpha value.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="alpha">Alpha value, normally from 0 to 1.</param>
    /// <returns>A color with the requested alpha.</returns>
    /// <remarks>The alpha value is clamped to the normalized range.</remarks>
    public static Color WithAlpha(in Color color, float alpha)
        => new(color.R, color.G, color.B, (byte)Math.Clamp(alpha * 255f, 0f, 255f));

    /// <summary>
    /// Returns a color with the same RGB channels and the specified byte alpha value.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="alpha">Alpha channel.</param>
    /// <returns>A color with the requested alpha.</returns>
    public static Color WithAlpha(in Color color, byte alpha)
        => new(color.R, color.G, color.B, alpha);
    #endregion

    #region Operators
    /// <summary>Determines whether two colors have identical channel values.</summary>
    public static bool operator ==(in Color a, in Color b) => a.Equals(b);

    /// <summary>Determines whether two colors have different channel values.</summary>
    public static bool operator !=(in Color a, in Color b) => !a.Equals(b);

    /// <summary>
    /// Scales each color channel by a scalar value.
    /// </summary>
    /// <remarks>The scalar is clamped to the range 0 to 1 before scaling.</remarks>
    public static Color operator *(float scalar, in Color color) => color * scalar;

    /// <summary>
    /// Scales each color channel by a scalar value.
    /// </summary>
    /// <remarks>The scalar is clamped to the range 0 to 1 before scaling.</remarks>
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
    /// Multiplies two colors component-wise using normalized channel multiplication.
    /// </summary>
    public static Color operator *(in Color a, in Color b)
        => new(
            (byte)(a.R * b.R / 255f),
            (byte)(a.G * b.G / 255f),
            (byte)(a.B * b.B / 255f),
            (byte)(a.A * b.A / 255f)
        );

    /// <summary>
    /// Adds two colors component-wise and clamps each channel to 255.
    /// </summary>
    public static Color operator +(in Color a, in Color b)
        => new(
            (byte)Math.Min(a.R + b.R, 255),
            (byte)Math.Min(a.G + b.G, 255),
            (byte)Math.Min(a.B + b.B, 255),
            (byte)Math.Min(a.A + b.A, 255)
        );

    /// <summary>
    /// Subtracts two colors component-wise and clamps each channel to zero.
    /// </summary>
    public static Color operator -(in Color a, in Color b)
        => new(
            (byte)Math.Max(a.R - b.R, 0),
            (byte)Math.Max(a.G - b.G, 0),
            (byte)Math.Max(a.B - b.B, 0),
            (byte)Math.Max(a.A - b.A, 0)
        );
    #endregion

    #region IEquatable
    /// <summary>
    /// Determines whether this color has the same channel values as another color.
    /// </summary>
    /// <param name="other">Color to compare.</param>
    /// <returns><see langword="true"/> when all channels are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(Color other)
        => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <summary>
    /// Determines whether this color is equal to the specified object.
    /// </summary>
    /// <param name="obj">Object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal <see cref="Color"/>; otherwise, <see langword="false"/>.</returns>
    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Color value && Equals(value);

    /// <summary>
    /// Returns the hash code for this color.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public readonly override int GetHashCode()
        => HashCode.Combine(R, G, B, A);

    /// <summary>
    /// Returns the RGBA channel values for this color.
    /// </summary>
    /// <returns>A string in <c>Color(R, G, B, A)</c> form.</returns>
    public readonly override string ToString()
        => $"Color({R}, {G}, {B}, {A})";
    #endregion
}
