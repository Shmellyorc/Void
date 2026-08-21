// ============================================================================
//  FloatExtensions.cs
// ============================================================================
//  Extension methods for floating-point operations including clamping,
//  wrapping, conversion, and rounding utilities.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for floating-point operations including
/// clamping, wrapping, conversion, and rounding utilities.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FloatExtensions"/> class provides a comprehensive set of
/// extension methods for <see cref="float"/> values, making common
/// mathematical operations more intuitive and readable.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Clamping and saturation</description></item>
///   <item><description>Angle conversion (degrees ↔ radians)</description></item>
///   <item><description>Wrapping values within ranges</description></item>
///   <item><description>Rounding and snapping</description></item>
///   <item><description>Epsilon-based equality comparisons</description></item>
///   <item><description>Formatting (percent, time)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// float value = 1.5f;
/// 
/// // Clamping
/// float clamped = value.Clamp(0f, 1f); // 1f
/// float saturated = value.Saturate(); // 1f
/// 
/// // Wrapping
/// float wrapped = 5f.Wrap(0f, 3f); // 2f
/// 
/// // Conversion
/// float radians = 90f.ToRadians(); // PI/2
/// float degrees = PI.ToDegrees(); // 180f
/// 
/// // Rounding
/// int rounded = 3.7f.RoundToInt(); // 4
/// int floored = 3.7f.FloorToInt(); // 3
/// int ceiled = 3.2f.CeilToInt(); // 4
/// 
/// // Snapping
/// float snapped = 12.3f.Snap(5f); // 10f
/// 
/// // Formatting
/// string percent = 0.75f.ToPercent(); // "75%"
/// string time = 125f.ToTimeString(); // "2:05"
/// 
/// // Interpolation
/// float lerped = 0f.LerpTo(10f, 0.5f); // 5f
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe as they operate on value types.
/// </para>
/// </remarks>
public static class FloatExtensions
{
    /// <summary>
    /// Clamps the value between a minimum and maximum.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(this float value, float min, float max)
        => Math.Clamp(value, min, max);

    /// <summary>
    /// Clamps the value between 0 and 1.
    /// </summary>
    /// <param name="value">The value to saturate.</param>
    /// <returns>The saturated value between 0 and 1.</returns>
    public static float Saturate(this float value)
        => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// Wraps the value within the range [0, max).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="max">The exclusive maximum value.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(this float value, float max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Wraps the value within the range [min, max).
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The exclusive maximum value.</param>
    /// <returns>The wrapped value.</returns>
    public static float Wrap(this float value, float min, float max)
        => min + ((value - min) % (max - min) + (max - min)) % (max - min);

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="value">The value in degrees.</param>
    /// <returns>The value in radians.</returns>
    public static float ToRadians(this float value)
        => value * MathHelper.DegToRad;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="value">The value in radians.</param>
    /// <returns>The value in degrees.</returns>
    public static float ToDegrees(this float value)
        => value * MathHelper.RadToDeg;

    /// <summary>
    /// Gets the sign of the value (-1, 0, or 1).
    /// </summary>
    /// <param name="value">The value to get the sign of.</param>
    /// <returns>The sign of the value.</returns>
    public static float Sign(this float value)
        => MathF.Sign(value);

    /// <summary>
    /// Gets the absolute value.
    /// </summary>
    /// <param name="value">The value to get the absolute value of.</param>
    /// <returns>The absolute value.</returns>
    public static float Abs(this float value)
        => MathF.Abs(value);

    /// <summary>
    /// Determines whether the value is approximately zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is within epsilon of zero; otherwise, <see langword="false"/>.</returns>
    public static bool IsZero(this float value)
        => MathF.Abs(value) < MathHelper.Epsilon;

    /// <summary>
    /// Determines whether the value is approximately equal to another value.
    /// </summary>
    /// <param name="value">The value to compare.</param>
    /// <param name="other">The other value to compare against.</param>
    /// <returns><see langword="true"/> if the values are within epsilon of each other; otherwise, <see langword="false"/>.</returns>
    public static bool ApproxEquals(this float value, float other)
        => MathF.Abs(value - other) < MathHelper.Epsilon;

    /// <summary>
    /// Rounds the value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded integer value.</returns>
    public static int RoundToInt(this float value)
        => (int)MathF.Round(value);

    /// <summary>
    /// Floors the value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to floor.</param>
    /// <returns>The floored integer value.</returns>
    public static int FloorToInt(this float value)
        => (int)MathF.Floor(value);

    /// <summary>
    /// Ceils the value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to ceil.</param>
    /// <returns>The ceiled integer value.</returns>
    public static int CeilToInt(this float value)
        => (int)MathF.Ceiling(value);

    /// <summary>
    /// Rounds the value to the specified number of decimal places.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="decimals">The number of decimal places.</param>
    /// <returns>The rounded value.</returns>
    public static float Round(this float value, int decimals = 0)
        => MathF.Round(value, decimals);

    /// <summary>
    /// Snaps the value to the nearest multiple of the specified grid size.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="gridSize">The grid size to snap to.</param>
    /// <returns>The snapped value.</returns>
    public static float Snap(this float value, float gridSize)
        => MathF.Round(value / gridSize) * gridSize;

    /// <summary>
    /// Converts the value to a percentage string.
    /// </summary>
    /// <param name="value">The value (0-1) to convert.</param>
    /// <returns>The percentage string.</returns>
    public static string ToPercent(this float value)
        => $"{value * 100f:0}%";

    /// <summary>
    /// Converts the value to a time string in M:SS format.
    /// </summary>
    /// <param name="value">The time in seconds.</param>
    /// <returns>The formatted time string.</returns>
    public static string ToTimeString(this float value)
    {
        int minutes = (int)(value / 60f);
        int seconds = (int)(value % 60f);
        return $"{minutes}:{seconds:00}";
    }

    /// <summary>
    /// Converts the value to a boolean.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns><see langword="true"/> if the value is not zero; otherwise, <see langword="false"/>.</returns>
    public static bool ToBool(this float value)
        => value != 0f;

    /// <summary>
    /// Linearly interpolates from this value to another.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The target value.</param>
    /// <param name="t">The interpolation factor (0-1).</param>
    /// <returns>The interpolated value.</returns>
    public static float LerpTo(this float from, float to, float t)
        => from + (to - from) * t;
}