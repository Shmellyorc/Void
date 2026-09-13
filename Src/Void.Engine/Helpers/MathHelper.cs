// ============================================================================
//  MathHelper.cs
// ============================================================================
//  Common scalar math helpers for interpolation, wrapping, angles, snapping,
//  and approximate floating-point comparisons.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides common scalar math helpers used throughout VOID.
/// </summary>
/// <remarks>
/// <para>
/// Angle helpers use radians unless a member explicitly says degrees. Interpolation
/// helpers such as <see cref="SmoothStep"/> and <see cref="InverseLerp"/> clamp their
/// interpolation factor to the zero-through-one range.
/// </para>
/// <code>
/// float halfway = MathHelper.Lerp(0f, 10f, 0.5f);
/// float radians = MathHelper.ToRadians(90f);
/// float degrees = MathHelper.ToDegrees(MathHelper.PI);
/// float wrapped = MathHelper.WrapAngle(radians);
/// </code>
/// </remarks>
public static class MathHelper
{
    /// <summary>Pi.</summary>
    public const float PI = MathF.PI;

    /// <summary>Two times pi.</summary>
    public const float TwoPI = MathF.PI * 2f;

    /// <summary>Pi divided by two.</summary>
    public const float HalfPI = MathF.PI / 2f;

    /// <summary>Pi divided by four.</summary>
    public const float QuarterPI = MathF.PI / 4f;

    /// <summary>One divided by pi.</summary>
    public const float InvPI = 1f / MathF.PI;

    /// <summary>Degrees-to-radians conversion factor.</summary>
    public const float DegToRad = MathF.PI / 180f;

    /// <summary>Radians-to-degrees conversion factor.</summary>
    public const float RadToDeg = 180f / MathF.PI;

    /// <summary>Default tolerance used by VOID for approximate float comparisons.</summary>
    public const float Epsilon = 0.001f;

    /// <summary>Determines whether a value is within a supplied tolerance of zero.</summary>
    /// <param name="v">Value to test.</param>
    /// <param name="e">Absolute tolerance.</param>
    /// <returns><see langword="true"/> when the absolute value is less than or equal to <paramref name="e"/>.</returns>
    public static bool AlmostZero(float v, float e)
        => MathF.Abs(v) <= e;

    /// <summary>Determines whether two values differ by no more than a supplied tolerance.</summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <param name="e">Absolute tolerance.</param>
    /// <returns><see langword="true"/> when the values are approximately equal.</returns>
    public static bool AlmostEquals(float a, float b, float e)
        => MathF.Abs(a - b) <= e;

    /// <summary>Converts degrees to radians.</summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static float ToRadians(float degrees)
        => degrees * DegToRad;

    /// <summary>Converts radians to degrees.</summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static float ToDegrees(float radians)
        => radians * RadToDeg;

    /// <summary>Converts radians to degrees.</summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    /// <remarks>Use <see cref="ToDegrees(float)"/>. This misspelled compatibility alias will be removed in a future breaking release.</remarks>
    [Obsolete("Use ToDegrees(float) instead.")]
    public static float ToDegress(float radians)
        => ToDegrees(radians);

    /// <summary>Linearly interpolates between two values.</summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor. Values outside zero through one extrapolate.</param>
    /// <returns>The interpolated value.</returns>
    public static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    /// <summary>Clamps a value between a minimum and maximum.</summary>
    /// <param name="value">Value to clamp.</param>
    /// <param name="min">Minimum returned value.</param>
    /// <param name="max">Maximum returned value.</param>
    /// <returns>The clamped value.</returns>
    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    /// <summary>Calculates half of the remaining size between a parent and child value.</summary>
    /// <param name="a">Parent or outer size.</param>
    /// <param name="b">Child or inner size.</param>
    /// <param name="clamped">Whether to round the resulting offset to the nearest whole value.</param>
    /// <returns><c>(a - b) / 2</c>, optionally rounded.</returns>
    public static float CenterOffset(float a, float b, bool clamped = false)
        => clamped ? MathF.Round((a - b) / 2f) : (a - b) / 2f;

    /// <summary>Calculates the midpoint between two values.</summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <param name="clamped">Whether to round the midpoint to the nearest whole value.</param>
    /// <returns>The midpoint.</returns>
    public static float Center(float a, float b, bool clamped = false)
        => clamped ? MathF.Round((a + b) / 2f) : (a + b) / 2f;

    /// <summary>Clamps a value to the range zero through one.</summary>
    /// <param name="value">Value to clamp.</param>
    /// <returns>The saturated value.</returns>
    public static float Saturate(float value)
        => value < 0f ? 0f : value > 1f ? 1f : value;

    /// <summary>Interpolates using a cubic smooth-step curve.</summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor, clamped to zero through one.</param>
    /// <returns>The smoothly interpolated value.</returns>
    public static float SmoothStep(float a, float b, float t)
    {
        t = Saturate(t);
        t = t * t * (3f - 2f * t);

        return Lerp(a, b, t);
    }

    /// <summary>Reflects a value back and forth between zero and a length.</summary>
    /// <param name="value">Value to evaluate. Negative values are supported.</param>
    /// <param name="length">Positive end of the interval.</param>
    /// <returns>The ping-pong value, or zero when <paramref name="length"/> is not positive.</returns>
    public static float PingPong(float value, float length)
    {
        if (length <= 0f)
            return 0f;

        float repeated = Wrap(value, 0f, length * 2f);
        return length - MathF.Abs(repeated - length);
    }

    /// <summary>Wraps a floating-point value into a half-open interval.</summary>
    /// <param name="value">Value to wrap.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Exclusive upper bound.</param>
    /// <returns>The wrapped value, or <paramref name="min"/> when the interval is empty or reversed.</returns>
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

    /// <summary>Wraps an integer into a half-open interval.</summary>
    /// <param name="value">Value to wrap.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Exclusive upper bound.</param>
    /// <returns>The wrapped value, or <paramref name="min"/> when the interval is empty or reversed.</returns>
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

    /// <summary>Moves a value toward a target by at most a supplied delta.</summary>
    /// <param name="current">Current value.</param>
    /// <param name="target">Target value.</param>
    /// <param name="maxDelta">Maximum movement amount. Callers should supply a nonnegative value.</param>
    /// <returns>The moved value.</returns>
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }

    /// <summary>Calculates a clamped interpolation factor for a value between two endpoints.</summary>
    /// <param name="a">Start of the source interval.</param>
    /// <param name="b">End of the source interval.</param>
    /// <param name="value">Value to locate.</param>
    /// <returns>A factor from zero through one, or zero when the endpoints are approximately equal.</returns>
    public static float InverseLerp(float a, float b, float value)
    {
        if (MathF.Abs(b - a) < Epsilon)
            return 0f;

        return Saturate((value - a) / (b - a));
    }

    /// <summary>Maps a value from one interval into another using clamped inverse interpolation.</summary>
    /// <param name="value">Value in the source interval.</param>
    /// <param name="fromMin">Source interval start.</param>
    /// <param name="fromMax">Source interval end.</param>
    /// <param name="toMin">Target interval start.</param>
    /// <param name="toMax">Target interval end.</param>
    /// <returns>The remapped value.</returns>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = InverseLerp(fromMin, fromMax, value);
        return Lerp(toMin, toMax, t);
    }

    /// <summary>Converts a 2D direction into an angle in radians.</summary>
    /// <param name="direction">Direction vector.</param>
    /// <returns>The angle returned by <see cref="MathF.Atan2(float, float)"/>.</returns>
    public static float DirectionToAngle(Vect2 direction)
        => MathF.Atan2(direction.Y, direction.X);

    /// <summary>Converts an angle in radians into a unit direction.</summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The corresponding direction vector.</returns>
    public static Vect2 AngleToDirection(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>Rounds a floating-point value to the nearest integer.</summary>
    /// <param name="value">Value to round.</param>
    /// <returns>The rounded integer.</returns>
    public static int RoundToInt(float value)
        => (int)MathF.Round(value);

    /// <summary>Rounds a floating-point value down to an integer.</summary>
    /// <param name="value">Value to floor.</param>
    /// <returns>The floored integer.</returns>
    public static int FloorToInt(float value)
        => (int)MathF.Floor(value);

    /// <summary>Rounds a floating-point value up to an integer.</summary>
    /// <param name="value">Value to ceil.</param>
    /// <returns>The ceiled integer.</returns>
    public static int CeilToInt(float value)
        => (int)MathF.Ceiling(value);

    /// <summary>Rounds a value to the nearest multiple of a grid size.</summary>
    /// <param name="value">Value to snap.</param>
    /// <param name="gridSize">Grid interval. Callers should supply a nonzero value.</param>
    /// <returns>The snapped value.</returns>
    public static float Snap(float value, float gridSize)
        => MathF.Round(value / gridSize) * gridSize;

    /// <summary>Wraps an angle in radians into the range from negative pi inclusive to pi exclusive.</summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The wrapped angle.</returns>
    public static float WrapAngle(float radians)
        => Wrap(radians, -PI, PI);

    /// <summary>Wraps an angle in degrees into the range from negative 180 inclusive to 180 exclusive.</summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The wrapped angle.</returns>
    public static float WrapAngleDegrees(float degrees)
        => Wrap(degrees, -180f, 180f);

    /// <summary>Calculates the shortest signed angular difference from one angle to another.</summary>
    /// <param name="a">Starting angle in radians.</param>
    /// <param name="b">Target angle in radians.</param>
    /// <returns>The wrapped signed difference.</returns>
    public static float AngleDifference(float a, float b)
        => WrapAngle(b - a);

    /// <summary>Moves an angle toward a target along the shortest wrapped path.</summary>
    /// <param name="current">Current angle in radians.</param>
    /// <param name="target">Target angle in radians.</param>
    /// <param name="maxDelta">Maximum angular movement. Callers should supply a nonnegative value.</param>
    /// <returns>The moved angle.</returns>
    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float diff = WrapAngle(target - current);

        if (MathF.Abs(diff) <= maxDelta)
            return target;

        return current + MathF.Sign(diff) * maxDelta;
    }

    /// <summary>Exponentially damps a value toward a target.</summary>
    /// <param name="a">Current value.</param>
    /// <param name="b">Target value.</param>
    /// <param name="smoothing">Smoothing rate.</param>
    /// <param name="dt">Elapsed time in seconds.</param>
    /// <returns>The damped value.</returns>
    public static float Damp(float a, float b, float smoothing, float dt)
        => Lerp(a, b, 1f - MathF.Exp(-smoothing * dt));

    /// <summary>Repeats a value over a zero-based interval.</summary>
    /// <param name="value">Value to repeat. Negative values are supported.</param>
    /// <param name="length">Positive interval length.</param>
    /// <returns>The repeated value, or zero when <paramref name="length"/> is not positive.</returns>
    public static float Repeat(float value, float length)
        => length <= 0f ? 0f : Wrap(value, 0f, length);

    /// <summary>Interpolates using a fifth-order smoother-step curve.</summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor, clamped to zero through one.</param>
    /// <returns>The smoothly interpolated value.</returns>
    public static float SmootherStep(float a, float b, float t)
    {
        t = Saturate(t);
        t = t * t * t * (t * (t * 6f - 15f) + 10f);

        return Lerp(a, b, t);
    }

    /// <summary>Gets the fractional part of a value using floor-based decomposition.</summary>
    /// <param name="value">Value to inspect.</param>
    /// <returns>A fractional value in the range zero inclusive to one exclusive for finite input.</returns>
    public static float Frac(float value)
        => value - MathF.Floor(value);
}
