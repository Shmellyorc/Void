// ============================================================================
//  IntExtensions.cs
// ============================================================================
//  Extension methods for integer operations including clamping, wrapping,
//  conversion, and common game development math utilities.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for integer operations including clamping, wrapping,
/// conversion, and common game development math utilities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IntExtensions"/> class provides a comprehensive set of
/// extension methods for <see cref="int"/> values, making common mathematical
/// and game development operations more intuitive and readable.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Clamping and wrapping</description></item>
///   <item><description>Angle conversion (degrees ↔ radians)</description></item>
///   <item><description>Even, odd, and power-of-two checks</description></item>
///   <item><description>Time and frame conversion</description></item>
///   <item><description>Tile coordinate conversion</description></item>
///   <item><description>Sign and absolute value</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// int value = 5;
/// 
/// // Clamping
/// int clamped = value.Clamp(0, 3); // 3
/// 
/// // Wrapping
/// int wrapped = 7.Wrap(0, 5); // 2
/// 
/// // Even/odd checks
/// bool isEven = 4.IsEven(); // true
/// bool isOdd = 3.IsOdd(); // true
/// 
/// // Power of two
/// bool isPower = 8.IsPowerOfTwo(); // true
/// int nextPower = 5.NextPowerOfTwo(); // 8
/// 
/// // Conversion
/// float radians = 90.ToRadians(); // PI/2
/// float degrees = 3.ToDegrees(); // ~171.9
/// 
/// // Time conversion
/// int ms = 2.SecondsToMilliseconds(); // 2000
/// float seconds = 1500.MillisecondsToSeconds(); // 1.5f
/// 
/// // Frame conversion
/// float secs = 120.FramesToSeconds(60); // 2.0f
/// int frames = 3.SecondsToFrames(60); // 180
/// 
/// // Tile conversion
/// int tileX = 128.PixelsToTiles(32); // 4
/// int pixelX = 5.TilesToPixels(32); // 160
/// 
/// // Boolean conversion
/// bool isTrue = 1.ToBool(); // true
/// 
/// // Sign and absolute
/// int sign = (-5).Sign(); // -1
/// int abs = (-5).Abs(); // 5
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe as they operate on value types.
/// </para>
/// </remarks>
public static class IntExtensions
{
    /// <summary>
    /// Clamps the value between the specified minimum and maximum.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static int Clamp(this int value, int min, int max)
        => Math.Clamp(value, min, max);

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="value">The value in degrees.</param>
    /// <returns>The value in radians.</returns>
    public static float ToRadians(this int value)
        => value * MathHelper.DegToRad;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>The value in degrees.</returns>
    public static float ToDegrees(this int value)
        => value * MathHelper.RadToDeg;

    /// <summary>
    /// Wraps the value within the range [0, max).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="max">The exclusive maximum value.</param>
    /// <returns>The wrapped value.</returns>
    public static int Wrap(this int value, int max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Wraps the value within the range [min, max).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The exclusive maximum value.</param>
    /// <returns>The wrapped value.</returns>
    public static int Wrap(this int value, int min, int max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    /// <summary>
    /// Determines whether the value is even.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is even; otherwise, <see langword="false"/>.</returns>
    public static bool IsEven(this int value)
        => (value & 1) == 0;

    /// <summary>
    /// Determines whether the value is odd.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is odd; otherwise, <see langword="false"/>.</returns>
    public static bool IsOdd(this int value)
        => (value & 1) == 1;

    /// <summary>
    /// Determines whether the value is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is a power of two; otherwise, <see langword="false"/>.</returns>
    public static bool IsPowerOfTwo(this int value)
        => value > 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Rounds up to the nearest power of two.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The next power of two greater than or equal to the value.</returns>
    public static int NextPowerOfTwo(this int value)
    {
        int result = 1;

        while (result < value)
            result <<= 1;
            
        return result;
    }

    /// <summary>
    /// Gets the sign of the value (-1, 0, or 1).
    /// </summary>
    /// <param name="value">The value to get the sign of.</param>
    /// <returns>The sign of the value.</returns>
    public static int Sign(this int value)
        => Math.Sign(value);

    /// <summary>
    /// Converts seconds to milliseconds.
    /// </summary>
    /// <param name="value">The value in seconds.</param>
    /// <returns>The value in milliseconds.</returns>
    public static int SecondsToMilliseconds(this int value)
        => value * 1000;

    /// <summary>
    /// Converts milliseconds to seconds.
    /// </summary>
    /// <param name="value">The value in milliseconds.</param>
    /// <returns>The value in seconds.</returns>
    public static float MillisecondsToSeconds(this int value)
        => value / 1000f;

    /// <summary>
    /// Converts frames to seconds at the specified FPS.
    /// </summary>
    /// <param name="frames">The number of frames.</param>
    /// <param name="fps">The frames per second rate.</param>
    /// <returns>The time in seconds.</returns>
    public static float FramesToSeconds(this int frames, int fps = 60)
        => frames / (float)fps;

    /// <summary>
    /// Converts seconds to frames at the specified FPS.
    /// </summary>
    /// <param name="seconds">The time in seconds.</param>
    /// <param name="fps">The frames per second rate.</param>
    /// <returns>The number of frames.</returns>
    public static int SecondsToFrames(this int seconds, int fps = 60)
        => seconds * fps;

    /// <summary>
    /// Converts pixels to tile coordinates.
    /// </summary>
    /// <param name="pixels">The value in pixels.</param>
    /// <param name="tileSize">The size of one tile in pixels.</param>
    /// <returns>The tile coordinate.</returns>
    public static int PixelsToTiles(this int pixels, int tileSize)
        => pixels / tileSize;

    /// <summary>
    /// Converts tile coordinates to pixels.
    /// </summary>
    /// <param name="tiles">The value in tiles.</param>
    /// <param name="tileSize">The size of one tile in pixels.</param>
    /// <returns>The pixel coordinate.</returns>
    public static int TilesToPixels(this int tiles, int tileSize)
        => tiles * tileSize;

    /// <summary>
    /// Converts the integer value to a boolean.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns><see langword="true"/> if the value is not zero; otherwise, <see langword="false"/>.</returns>
    public static bool ToBool(this int value)
        => value != 0;

    /// <summary>
    /// Gets the absolute value.
    /// </summary>
    /// <param name="value">The value to get the absolute value of.</param>
    /// <returns>The absolute value.</returns>
    public static int Abs(this int value)
        => Math.Abs(value);
}