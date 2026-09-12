// ============================================================================
//  FloatExtensions.cs
// ============================================================================
//  Common floating-point math, conversion, formatting, and rounding helpers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides common math, conversion, formatting, and rounding helpers for <see cref="float"/> values.
/// </summary>
public static class FloatExtensions
{
    /// <summary>
    /// Clamps the value to the inclusive range defined by <paramref name="min"/> and <paramref name="max"/>.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(this float value, float min, float max)
        => Math.Clamp(value, min, max);

    /// <summary>
    /// Clamps the value to the inclusive range from 0 to 1.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    public static float Saturate(this float value)
        => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// Wraps the value into the range [0, <paramref name="max"/>).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(this float value, float max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Wraps the value into the range [<paramref name="min"/>, <paramref name="max"/>).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(this float value, float min, float max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="value">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static float ToRadians(this float value)
        => value * MathHelper.DegToRad;

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="value">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static float ToDegrees(this float value)
        => value * MathHelper.RadToDeg;

    /// <summary>
    /// Returns the sign of the value.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>-1 for negative values, 0 for zero, or 1 for positive values.</returns>
    public static float Sign(this float value)
        => MathF.Sign(value);

    /// <summary>
    /// Returns the absolute value.
    /// </summary>
    /// <param name="value">The value to convert to its magnitude.</param>
    /// <returns>The absolute value.</returns>
    public static float Abs(this float value)
        => MathF.Abs(value);

    /// <summary>
    /// Determines whether the absolute value is smaller than <see cref="MathHelper.Epsilon"/>.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the value is within the engine epsilon of zero; otherwise, <see langword="false"/>.</returns>
    public static bool IsZero(this float value)
        => MathF.Abs(value) < MathHelper.Epsilon;

    /// <summary>
    /// Determines whether two values differ by less than <see cref="MathHelper.Epsilon"/>.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true"/> when the values are within the engine epsilon; otherwise, <see langword="false"/>.</returns>
    public static bool ApproxEquals(this float value, float other)
        => MathF.Abs(value - other) < MathHelper.Epsilon;

    /// <summary>
    /// Rounds the value to the nearest integer using <see cref="MathF.Round(float)"/>.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded integer.</returns>
    public static int RoundToInt(this float value)
        => (int)MathF.Round(value);

    /// <summary>
    /// Rounds the value downward to the nearest integer.
    /// </summary>
    /// <param name="value">The value to floor.</param>
    /// <returns>The floored integer.</returns>
    public static int FloorToInt(this float value)
        => (int)MathF.Floor(value);

    /// <summary>
    /// Rounds the value upward to the nearest integer.
    /// </summary>
    /// <param name="value">The value to ceil.</param>
    /// <returns>The ceiled integer.</returns>
    public static int CeilToInt(this float value)
        => (int)MathF.Ceiling(value);

    /// <summary>
    /// Rounds the value to the specified number of fractional digits.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="decimals">The number of fractional digits.</param>
    /// <returns>The rounded value.</returns>
    public static float Round(this float value, int decimals = 0)
        => MathF.Round(value, decimals);

    /// <summary>
    /// Rounds the value to the nearest multiple of <paramref name="gridSize"/>.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="gridSize">The spacing between snap points.</param>
    /// <returns>The snapped value.</returns>
    public static float Snap(this float value, float gridSize)
        => MathF.Round(value / gridSize) * gridSize;

    /// <summary>
    /// Formats the value as a whole-number percentage after multiplying it by 100.
    /// </summary>
    /// <param name="value">The fractional value to format.</param>
    /// <returns>The formatted percentage string.</returns>
    public static string ToPercent(this float value)
        => $"{value * 100f:0}%";

    /// <summary>
    /// Formats a number of seconds as minutes and two-digit seconds.
    /// </summary>
    /// <param name="value">The time value in seconds.</param>
    /// <returns>A string in M:SS form.</returns>
    public static string ToTimeString(this float value)
    {
        int minutes = (int)(value / 60f);
        int seconds = (int)(value % 60f);
        return $"{minutes}:{seconds:00}";
    }

    /// <summary>
    /// Converts zero to <see langword="false"/> and every nonzero value to <see langword="true"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The Boolean representation of the value.</returns>
    public static bool ToBool(this float value)
        => value != 0f;

    /// <summary>
    /// Linearly interpolates from this value toward <paramref name="to"/>.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The target value.</param>
    /// <param name="t">The interpolation amount. The value is not clamped.</param>
    /// <returns>The interpolated value.</returns>
    public static float LerpTo(this float from, float to, float t)
        => from + (to - from) * t;
}
