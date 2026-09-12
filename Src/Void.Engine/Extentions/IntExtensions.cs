// ============================================================================
//  IntExtensions.cs
// ============================================================================
//  Common integer math and conversion helpers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides common math, conversion, wrapping, and grid helpers for <see cref="int"/> values.
/// </summary>
public static class IntExtensions
{
    /// <summary>
    /// Clamps the value to the inclusive range defined by <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static int Clamp(this int value, int min, int max)
        => Math.Clamp(value, min, max);

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="value">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static float ToRadians(this int value)
        => value * MathHelper.DegToRad;

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static float ToDegrees(this int value)
        => value * MathHelper.RadToDeg;

    /// <summary>
    /// Wraps the value into the range [0, <paramref name="max"/>).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value.</returns>
    public static int Wrap(this int value, int max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Wraps the value into the range [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value.</returns>
    public static int Wrap(this int value, int min, int max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    /// <summary>
    /// Determines whether the value is even.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is even; otherwise, <see langword="false"/>.</returns>
    public static bool IsEven(this int value)
        => (value & 1) == 0;

    /// <summary>
    /// Determines whether the value is odd.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is odd; otherwise, <see langword="false"/>.</returns>
    public static bool IsOdd(this int value)
        => (value & 1) == 1;

    /// <summary>
    /// Determines whether the value is a positive power of two.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is a positive power of two; otherwise, <see langword="false"/>.</returns>
    public static bool IsPowerOfTwo(this int value)
        => value > 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Returns the smallest power of two that is greater than or equal to the value.
    /// </summary>
    /// <param name="value">The value to round upward.</param>
    /// <returns>The next power of two, starting from 1.</returns>
    public static int NextPowerOfTwo(this int value)
    {
        int result = 1;

        while (result < value)
            result <<= 1;
            
        return result;
    }

    /// <summary>
    /// Returns the sign of the value.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>-1 for negative values, 0 for zero, or 1 for positive values.</returns>
    public static int Sign(this int value)
        => Math.Sign(value);

    /// <summary>
    /// Converts whole seconds to milliseconds.
    /// </summary>
    /// <param name="value">The number of seconds.</param>
    /// <returns>The number of milliseconds.</returns>
    public static int SecondsToMilliseconds(this int value)
        => value * 1000;

    /// <summary>
    /// Converts milliseconds to seconds.
    /// </summary>
    /// <param name="value">The number of milliseconds.</param>
    /// <returns>The number of seconds.</returns>
    public static float MillisecondsToSeconds(this int value)
        => value / 1000f;

    /// <summary>
    /// Converts a frame count to seconds at the specified frame rate.
    /// </summary>
    /// <param name="frames">The frame count.</param>
    /// <param name="fps">The frame rate used for the conversion.</param>
    /// <returns>The equivalent duration in seconds.</returns>
    public static float FramesToSeconds(this int frames, int fps = 60)
        => frames / (float)fps;

    /// <summary>
    /// Converts whole seconds to a frame count at the specified frame rate.
    /// </summary>
    /// <param name="seconds">The number of seconds.</param>
    /// <param name="fps">The frame rate used for the conversion.</param>
    /// <returns>The equivalent number of frames.</returns>
    public static int SecondsToFrames(this int seconds, int fps = 60)
        => seconds * fps;

    /// <summary>
    /// Converts a pixel coordinate to a tile coordinate using integer division.
    /// </summary>
    /// <param name="pixels">The pixel coordinate.</param>
    /// <param name="tileSize">The tile size in pixels.</param>
    /// <returns>The corresponding tile coordinate.</returns>
    public static int PixelsToTiles(this int pixels, int tileSize)
        => pixels / tileSize;

    /// <summary>
    /// Converts a tile coordinate to pixels.
    /// </summary>
    /// <param name="tiles">The tile coordinate.</param>
    /// <param name="tileSize">The tile size in pixels.</param>
    /// <returns>The corresponding pixel coordinate.</returns>
    public static int TilesToPixels(this int tiles, int tileSize)
        => tiles * tileSize;

    /// <summary>
    /// Converts zero to <see langword="false"/> and every nonzero value to <see langword="true"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The Boolean representation of the value.</returns>
    public static bool ToBool(this int value)
        => value != 0;

    /// <summary>
    /// Returns the absolute value.
    /// </summary>
    /// <param name="value">The value to convert to its magnitude.</param>
    /// <returns>The absolute value.</returns>
    public static int Abs(this int value)
        => Math.Abs(value);
}
