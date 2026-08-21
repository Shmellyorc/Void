// ============================================================================
//  MathHelper.cs
// ============================================================================
//  Comprehensive collection of mathematical utility functions for
//  interpolation, clamping, wrapping, conversion, and common game math operations.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides a comprehensive collection of mathematical utility functions for
/// interpolation, clamping, wrapping, conversion, and common game math operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MathHelper"/> class contains static methods for common
/// mathematical operations used in game development. It includes constants
/// for PI, conversion functions, interpolation, clamping, and various
/// utility functions.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Math constants (PI, TwoPI, HalfPI, etc.)</description></item>
///   <item><description>Angle conversion (degrees ↔ radians)</description></item>
///   <item><description>Interpolation (Lerp, SmoothStep, InverseLerp)</description></item>
///   <item><description>Clamping and saturation</description></item>
///   <item><description>Wrapping for values and angles</description></item>
///   <item><description>Direction and angle conversion</description></item>
///   <item><description>Snap and rounding utilities</description></item>
///   <item><description>Epsilon-based equality comparisons</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Interpolation
/// float value = MathHelper.Lerp(0f, 10f, 0.5f); // 5f
/// float smooth = MathHelper.SmoothStep(0f, 10f, 0.5f);
/// 
/// // Clamping
/// float clamped = MathHelper.Clamp(value, 0f, 1f);
/// float saturated = MathHelper.Saturate(value);
/// 
/// // Wrapping
/// float wrapped = MathHelper.Wrap(angle, -PI, PI);
/// 
/// // Conversion
/// float radians = MathHelper.ToRadians(90f); // PI/2
/// float degrees = MathHelper.ToDegrees(PI); // 180f
/// 
/// // Direction to angle
/// float angle = MathHelper.DirectionToAngle(new Vect2(1f, 0f)); // 0f
/// Vect2 direction = MathHelper.AngleToDirection(angle);
/// 
/// // Snapping
/// float snapped = MathHelper.Snap(12.3f, 5f); // 10f
/// </code>
/// </para>
/// </remarks>
public static class MathHelper
{
    /// <summary>Pi constant.</summary>
    public const float PI = MathF.PI;

    /// <summary>Two times Pi (2π).</summary>
    public const float TwoPI = MathF.PI * 2f;

    /// <summary>Pi divided by two (π/2).</summary>
    public const float HalfPI = MathF.PI / 2f;

    /// <summary>Pi divided by four (π/4).</summary>
    public const float QuarterPI = MathF.PI / 4f;

    /// <summary>One divided by Pi (1/π).</summary>
    public const float InvPI = 1f / MathF.PI;

    /// <summary>Degrees to radians conversion factor (π/180).</summary>
    public const float DegToRad = MathF.PI / 180f;

    /// <summary>Radians to degrees conversion factor (180/π).</summary>
    public const float RadToDeg = 180f / MathF.PI;

    /// <summary>Epsilon value for floating-point comparisons.</summary>
    public const float Epsilon = 0.001f;

    /// <summary>
    /// Determines whether a floating-point value is approximately zero.
    /// </summary>
    /// <param name="v">The value to check.</param>
    /// <param name="e">The epsilon tolerance.</param>
    /// <returns><see langword="true"/> if the value is within epsilon of zero; otherwise, <see langword="false"/>.</returns>
    public static bool AlmostZero(float v, float e)
        => MathF.Abs(v) <= e;

    /// <summary>
    /// Determines whether two floating-point values are approximately equal.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="e">The epsilon tolerance.</param>
    /// <returns><see langword="true"/> if the values are within epsilon of each other; otherwise, <see langword="false"/>.</returns>
    public static bool AlmostEquals(float a, float b, float e)
        => MathF.Abs(a - b) <= e;

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static float ToRadians(float degrees)
        => degrees * DegToRad;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static float ToDegress(float radians)
        => radians * RadToDeg;

    /// <summary>
    /// Linearly interpolates between two values.
    /// </summary>
    /// <param name="a">The start value.</param>
    /// <param name="b">The end value.</param>
    /// <param name="t">The interpolation factor (0-1).</param>
    /// <returns>The interpolated value.</returns>
    public static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    /// <summary>
    /// Clamps a value between a minimum and maximum.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    /// <summary>
    /// Calculates the center offset between two values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <param name="clamped">If true, rounds the result.</param>
    /// <returns>The center offset.</returns>
    public static float Center(float a, float b, bool clamped = false)
        => clamped ? MathF.Round((a - b) / 2f) : (a - b) / 2f;

    /// <summary>
    /// Clamps a value between 0 and 1.
    /// </summary>
    /// <param name="value">The value to saturate.</param>
    /// <returns>The saturated value (0-1).</returns>
    public static float Saturate(float value)
        => value < 0f ? 0f : value > 1f ? 1f : value;

    /// <summary>
    /// Performs smooth Hermite interpolation between two values.
    /// </summary>
    /// <param name="a">The start value.</param>
    /// <param name="b">The end value.</param>
    /// <param name="t">The interpolation factor (0-1).</param>
    /// <returns>The smoothly interpolated value.</returns>
    public static float SmoothStep(float a, float b, float t)
    {
        t = Saturate(t);
        t = t * t * (3f - 2f * t);

        return Lerp(a, b, t);
    }

    /// <summary>
    /// Ping-pongs a value between 0 and the specified length.
    /// </summary>
    /// <param name="value">The value to ping-pong.</param>
    /// <param name="length">The maximum length.</param>
    /// <returns>The ping-ponged value between 0 and <paramref name="length"/>.</returns>
    public static float PingPong(float value, float length)
    {
        if (length <= 0f)
            return 0f;

        float mod = value % (length * 2f);

        return mod < length ? mod : length * 2f - mod;
    }

    /// <summary>
    /// Wraps a floating-point value within a specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>The wrapped value within the range.</returns>
    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;

        if (range <= 0f)
            return min;

        value = (value - min) % range;

        if (value < 0f)
            value += range;

        return value + min;
    }

    /// <summary>
    /// Wraps an integer value within a specified range.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (exclusive).</param>
    /// <returns>The wrapped value within the range.</returns>
    public static int Wrap(int value, int min, int max)
    {
        int range = max - min;
        if (range <= 0)
            return min;

        int mod = (value - min) % range;
        if (mod < 0)
            mod += range;

        return mod + min;
    }

    /// <summary>
    /// Moves a value towards a target by a maximum delta.
    /// </summary>
    /// <param name="current">The current value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="maxDelta">The maximum amount to move.</param>
    /// <returns>The new value after moving towards the target.</returns>
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }

    /// <summary>
    /// Calculates the inverse interpolation factor between two values.
    /// </summary>
    /// <param name="a">The start value.</param>
    /// <param name="b">The end value.</param>
    /// <param name="value">The value to interpolate.</param>
    /// <returns>The interpolation factor (0-1) representing where <paramref name="value"/> lies between <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static float InverseLerp(float a, float b, float value)
    {
        if (MathF.Abs(b - a) < Epsilon)
            return 0f;

        return Saturate((value - a) / (b - a));
    }

    /// <summary>
    /// Remaps a value from one range to another.
    /// </summary>
    /// <param name="value">The value to remap.</param>
    /// <param name="fromMin">The minimum of the source range.</param>
    /// <param name="fromMax">The maximum of the source range.</param>
    /// <param name="toMin">The minimum of the target range.</param>
    /// <param name="toMax">The maximum of the target range.</param>
    /// <returns>The remapped value.</returns>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = InverseLerp(fromMin, fromMax, value);
        return Lerp(toMin, toMax, t);
    }

    /// <summary>
    /// Converts a direction vector to an angle in radians.
    /// </summary>
    /// <param name="direction">The direction vector.</param>
    /// <returns>The angle in radians.</returns>
    public static float DirectionToAngle(Vect2 direction)
        => MathF.Atan2(direction.Y, direction.X);

    /// <summary>
    /// Converts an angle in radians to a direction vector.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The direction vector.</returns>
    public static Vect2 AngleToDirection(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>
    /// Rounds a floating-point value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded integer value.</returns>
    public static int RoundToInt(float value)
        => (int)MathF.Round(value);

    /// <summary>
    /// Floors a floating-point value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to floor.</param>
    /// <returns>The floored integer value.</returns>
    public static int FloorToInt(float value)
        => (int)MathF.Floor(value);

    /// <summary>
    /// Ceils a floating-point value to the nearest integer.
    /// </summary>
    /// <param name="value">The value to ceil.</param>
    /// <returns>The ceiled integer value.</returns>
    public static int CeilToInt(float value)
        => (int)MathF.Ceiling(value);

    /// <summary>
    /// Snaps a value to the nearest multiple of a grid size.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="gridSize">The grid size to snap to.</param>
    /// <returns>The snapped value.</returns>
    public static float Snap(float value, float gridSize)
        => MathF.Round(value / gridSize) * gridSize;
}