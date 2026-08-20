// ============================================================================
//  Vect2.cs
// ============================================================================
//  2D vector structure with comprehensive mathematical operations including
//  arithmetic, transformations, distance calculations, normalization, and
//  geometric utilities for game development.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 2D vector with floating-point components for position, direction,
/// and velocity calculations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Vect2"/> structure provides comprehensive mathematical operations
/// for 2D vectors including addition, subtraction, multiplication, division,
/// distance calculations, normalization, rotation, reflection, and interpolation.
/// It is used extensively throughout the engine for positions, directions,
/// velocities, and other spatial calculations.
/// </para>
/// <para>
/// All operations are performed with single-precision floating-point values
/// and include epsilon-based equality comparisons to handle floating-point
/// precision issues.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Create vectors
/// var position = new Vect2(10f, 20f);
/// var velocity = new Vect2(5f, -3f);
/// 
/// // Calculate distance
/// float distance = position.Distance(new Vect2(100f, 50f));
/// 
/// // Normalize a direction
/// var direction = new Vect2(3f, 4f).Normalized();
/// 
/// // Interpolate between positions
/// var midPoint = position.Lerp(target, 0.5f);
/// 
/// // Rotate a vector
/// var rotated = direction.Rotate(MathHelper.PI / 2f);
/// </code>
/// </para>
/// </remarks>
public struct Vect2 : IEquatable<Vect2>
{
    #region Fields
    private static readonly Vect2 _vectZero = new(0);
    private static readonly Vect2 _vectOne = new(1);
    private static readonly Vect2 _vectUp = new(0, -1);
    private static readonly Vect2 _vectDown = new(0, 1);
    private static readonly Vect2 _vectLeft = new(-1, 0);
    private static readonly Vect2 _vectRight = new(1, 0);
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the X-component of the vector.
    /// </summary>
    public float X, Y;

    /// <summary>
    /// Gets a vector with both components set to zero.
    /// </summary>
    public static Vect2 Zero => _vectZero;

    /// <summary>
    /// Gets a vector with both components set to one.
    /// </summary>
    public static Vect2 One => _vectOne;

    /// <summary>
    /// Gets a vector pointing upward (0, -1).
    /// </summary>
    public static Vect2 Up => _vectUp;

    /// <summary>
    /// Gets a vector pointing downward (0, 1).
    /// </summary>
    public static Vect2 Down => _vectDown;

    /// <summary>
    /// Gets a vector pointing left (-1, 0).
    /// </summary>
    public static Vect2 Left => _vectLeft;

    /// <summary>
    /// Gets a vector pointing right (1, 0).
    /// </summary>
    public static Vect2 Right => _vectRight;

    /// <summary>
    /// Gets a value indicating whether both components are approximately zero.
    /// </summary>
    public readonly bool IsZero
        => MathHelper.AlmostZero(X, MathHelper.Epsilon) && MathHelper.AlmostZero(Y, MathHelper.Epsilon);
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Vect2"/> structure with the specified X and Y components.
    /// </summary>
    /// <param name="x">The X-component of the vector.</param>
    /// <param name="y">The Y-component of the vector.</param>
    public Vect2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vect2"/> structure with both components set to the specified value.
    /// </summary>
    /// <param name="value">The value to set for both X and Y components.</param>
    public Vect2(float value) : this(value, value) { }
    #endregion

    #region Operators
    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    public static bool operator ==(in Vect2 a, in Vect2 b) => a.Equals(b);

    /// <summary>
    /// Determines whether two vectors are not equal.
    /// </summary>
    public static bool operator !=(in Vect2 a, in Vect2 b) => !a.Equals(b);

    /// <summary>
    /// Implicitly converts an SFML Vector2f to a Vect2.
    /// </summary>
    public static implicit operator Vect2(in SFVector2f v) => new(v.X, v.Y);

    /// <summary>
    /// Implicitly converts an SFML Vector2i to a Vect2.
    /// </summary>
    public static implicit operator Vect2(in SFVector2i v) => new(v.X, v.Y);

    /// <summary>
    /// Implicitly converts an SFML Vector2u to a Vect2.
    /// </summary>
    public static implicit operator Vect2(in SFVector2u v) => new(v.X, v.Y);

    /// <summary>
    /// Implicitly converts a Vect2 to an SFML Vector2f.
    /// </summary>
    public static implicit operator SFVector2f(in Vect2 v) => new(v.X, v.Y);

    /// <summary>
    /// Implicitly converts a Vect2 to an SFML Vector2i.
    /// </summary>
    public static implicit operator SFVector2i(in Vect2 v) => new((int)v.X, (int)v.Y);

    /// <summary>
    /// Implicitly converts a Vect2 to an SFML Vector2u.
    /// </summary>
    public static implicit operator SFVector2u(in Vect2 v) => new((uint)v.X, (uint)v.Y);

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    public static Vect2 operator +(in Vect2 a, in Vect2 b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>
    /// Adds a scalar to both components of a vector.
    /// </summary>
    public static Vect2 operator +(in Vect2 a, float b) => new(a.X + b, a.Y + b);

    /// <summary>
    /// Adds a scalar to both components of a vector.
    /// </summary>
    public static Vect2 operator +(float a, in Vect2 b) => b + a;

    /// <summary>
    /// Divides two vectors component-wise.
    /// </summary>
    public static Vect2 operator /(in Vect2 a, in Vect2 b) => new(a.X / b.X, a.Y / b.Y);

    /// <summary>
    /// Divides a vector by a scalar.
    /// </summary>
    public static Vect2 operator /(in Vect2 a, float b) => new(a.X / b, a.Y / b);

    /// <summary>
    /// Divides a scalar by a vector component-wise.
    /// </summary>
    public static Vect2 operator /(float a, in Vect2 b) => b / a;

    /// <summary>
    /// Multiplies two vectors component-wise.
    /// </summary>
    public static Vect2 operator *(in Vect2 a, in Vect2 b) => new(a.X * b.X, a.Y * b.Y);

    /// <summary>
    /// Multiplies a vector by a scalar.
    /// </summary>
    public static Vect2 operator *(in Vect2 a, float b) => new(a.X * b, a.Y * b);

    /// <summary>
    /// Multiplies a scalar by a vector.
    /// </summary>
    public static Vect2 operator *(float a, in Vect2 b) => b * a;

    /// <summary>
    /// Negates a vector.
    /// </summary>
    public static Vect2 operator -(in Vect2 value) => new(-value.X, -value.Y);

    /// <summary>
    /// Subtracts two vectors component-wise.
    /// </summary>
    public static Vect2 operator -(in Vect2 a, in Vect2 b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>
    /// Subtracts a scalar from both components of a vector.
    /// </summary>
    public static Vect2 operator -(in Vect2 a, float b) => new(a.X - b, a.Y - b);

    /// <summary>
    /// Subtracts a vector from a scalar component-wise.
    /// </summary>
    public static Vect2 operator -(float a, in Vect2 b) => b - a;
    #endregion

    #region Transform
    /// <summary>
    /// Transforms the vector from screen space to world space using the specified camera.
    /// </summary>
    /// <param name="camera">The camera to use for the transformation.</param>
    /// <returns>The vector transformed to world space.</returns>
    public readonly Vect2 Transform(in Camera camera)
        => Transform(this, camera);

    /// <summary>
    /// Transforms a vector from screen space to world space using the specified camera.
    /// </summary>
    /// <param name="mouse">The screen-space vector to transform.</param>
    /// <param name="camera">The camera to use for the transformation.</param>
    /// <returns>The vector transformed to world space.</returns>
    public static Vect2 Transform(in Vect2 mouse, in Camera camera)
        => camera.ScreenToWorld(mouse);
    #endregion

    #region DistanceSquared
    /// <summary>
    /// Calculates the squared distance between this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The squared distance between the vectors.</returns>
    public readonly float DistanceSquared(in Vect2 other) => DistanceSquared(this, other);

    /// <summary>
    /// Calculates the squared distance between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The squared distance between the vectors.</returns>
    public static float DistanceSquared(in Vect2 a, in Vect2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return LengthSquared(new(dx, dy));
    }
    #endregion

    #region Distance
    /// <summary>
    /// Calculates the distance between this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The distance between the vectors.</returns>
    public readonly float Distance(in Vect2 other) => Distance(this, other);

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The distance between the vectors.</returns>
    public static float Distance(in Vect2 a, in Vect2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return Length(new(dx, dy));
    }
    #endregion

    #region Length
    /// <summary>
    /// Calculates the length (magnitude) of the vector.
    /// </summary>
    /// <returns>The length of the vector.</returns>
    public readonly float Length() => Length(this);

    /// <summary>
    /// Calculates the length (magnitude) of a vector.
    /// </summary>
    /// <param name="value">The vector to calculate the length of.</param>
    /// <returns>The length of the vector.</returns>
    public static float Length(in Vect2 value)
    {
        return MathF.Sqrt(value.X * value.X + value.Y * value.Y);
    }
    #endregion

    #region LengthSquared
    /// <summary>
    /// Calculates the squared length (magnitude) of the vector.
    /// </summary>
    /// <returns>The squared length of the vector.</returns>
    public readonly float LengthSquared() => LengthSquared(this);

    /// <summary>
    /// Calculates the squared length (magnitude) of a vector.
    /// </summary>
    /// <param name="value">The vector to calculate the squared length of.</param>
    /// <returns>The squared length of the vector.</returns>
    public static float LengthSquared(in Vect2 value)
    {
        return value.X * value.X + value.Y * value.Y;
    }
    #endregion

    #region Min
    /// <summary>
    /// Returns a vector with the smaller components from this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>A vector containing the minimum components from both vectors.</returns>
    public readonly Vect2 Min(in Vect2 other) => Min(this, other);

    /// <summary>
    /// Returns a vector with the smaller components from two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>A vector containing the minimum components from both vectors.</returns>
    public static Vect2 Min(in Vect2 a, in Vect2 b)
        => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));
    #endregion

    #region Max
    /// <summary>
    /// Returns a vector with the larger components from this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>A vector containing the maximum components from both vectors.</returns>
    public readonly Vect2 Max(in Vect2 other) => Max(this, other);

    /// <summary>
    /// Returns a vector with the larger components from two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>A vector containing the maximum components from both vectors.</returns>
    public static Vect2 Max(in Vect2 a, in Vect2 b)
        => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));
    #endregion

    #region Clamp
    /// <summary>
    /// Clamps the vector components between the specified minimum and maximum vectors.
    /// </summary>
    /// <param name="min">The minimum vector.</param>
    /// <param name="max">The maximum vector.</param>
    /// <returns>A vector with components clamped between min and max.</returns>
    public readonly Vect2 Clamp(in Vect2 min, in Vect2 max) => Clamp(this, min, max);

    /// <summary>
    /// Clamps a vector's components between the specified minimum and maximum vectors.
    /// </summary>
    /// <param name="value">The vector to clamp.</param>
    /// <param name="min">The minimum vector.</param>
    /// <param name="max">The maximum vector.</param>
    /// <returns>A vector with components clamped between min and max.</returns>
    public static Vect2 Clamp(in Vect2 value, in Vect2 min, in Vect2 max)
        => new(Math.Clamp(value.X, min.X, max.X), Math.Clamp(value.Y, min.Y, max.Y));
    #endregion

    #region Ceiling
    /// <summary>
    /// Returns the smallest integer values greater than or equal to each component.
    /// </summary>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded up to the nearest specified precision.</returns>
    public readonly Vect2 Ceiling(int digits = 0) => Ceiling(this, digits);

    /// <summary>
    /// Returns the smallest integer values greater than or equal to each component.
    /// </summary>
    /// <param name="value">The vector to ceiling.</param>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded up to the nearest specified precision.</returns>
    public static Vect2 Ceiling(in Vect2 value, int digits = 0)
    {
        if (digits < 0 || digits > 6)
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be between 0 and 6.");

        float multiplier = MathF.Pow(10, digits);
        return new(MathF.Ceiling(value.X * multiplier) / multiplier,
                   MathF.Ceiling(value.Y * multiplier) / multiplier);
    }
    #endregion

    #region Floor
    /// <summary>
    /// Returns the largest integer values less than or equal to each component.
    /// </summary>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded down to the nearest specified precision.</returns>
    public readonly Vect2 Floor(int digits = 0) => Floor(this, digits);

    /// <summary>
    /// Returns the largest integer values less than or equal to each component.
    /// </summary>
    /// <param name="value">The vector to floor.</param>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded down to the nearest specified precision.</returns>
    public static Vect2 Floor(in Vect2 value, int digits = 0)
    {
        if (digits < 0 || digits > 6)
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be between 0 and 6.");

        float multiplier = MathF.Pow(10, digits);
        return new(MathF.Floor(value.X * multiplier) / multiplier,
                   MathF.Floor(value.Y * multiplier) / multiplier);
    }
    #endregion

    #region Round
    /// <summary>
    /// Rounds each component to the nearest integer value.
    /// </summary>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded to the nearest specified precision.</returns>
    public readonly Vect2 Round(int digits = 0) => Round(this, digits);

    /// <summary>
    /// Rounds each component of a vector to the nearest integer value.
    /// </summary>
    /// <param name="value">The vector to round.</param>
    /// <param name="digits">The number of decimal places to round to (0-6).</param>
    /// <returns>A vector with each component rounded to the nearest specified precision.</returns>
    public static Vect2 Round(in Vect2 value, int digits = 0)
    {
        if (digits < 0 || digits > 6)
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be between 0 and 6.");
        return new(MathF.Round(value.X, digits), MathF.Round(value.Y, digits));
    }
    #endregion

    #region SmoothStep
    /// <summary>
    /// Performs a smooth Hermite interpolation between this vector and a target vector.
    /// </summary>
    /// <param name="target">The target vector.</param>
    /// <param name="t">The interpolation factor between 0 and 1.</param>
    /// <returns>The smoothly interpolated vector.</returns>
    public readonly Vect2 SmoothStep(in Vect2 target, float t)
        => SmoothStep(this, target, t);

    /// <summary>
    /// Performs a smooth Hermite interpolation between two vectors.
    /// </summary>
    /// <param name="a">The starting vector.</param>
    /// <param name="b">The ending vector.</param>
    /// <param name="t">The interpolation factor between 0 and 1.</param>
    /// <returns>The smoothly interpolated vector.</returns>
    public static Vect2 SmoothStep(in Vect2 a, in Vect2 b, float t) => new(
        MathHelper.SmoothStep(a.X, b.X, t), MathHelper.SmoothStep(a.Y, b.Y, t));
    #endregion

    #region Wrap
    /// <summary>
    /// Wraps the vector components between the specified minimum and maximum values.
    /// </summary>
    /// <param name="min">The minimum values.</param>
    /// <param name="max">The maximum values.</param>
    /// <returns>A vector with components wrapped between min and max.</returns>
    public readonly Vect2 Wrap(in Vect2 min, in Vect2 max) => Wrap(this, min, max);

    /// <summary>
    /// Wraps a vector's components between the specified minimum and maximum values.
    /// </summary>
    /// <param name="value">The vector to wrap.</param>
    /// <param name="min">The minimum values.</param>
    /// <param name="max">The maximum values.</param>
    /// <returns>A vector with components wrapped between min and max.</returns>
    public static Vect2 Wrap(in Vect2 value, in Vect2 min, in Vect2 max)
        => new(MathHelper.Wrap(value.X, min.X, max.X), MathHelper.Wrap(value.Y, min.Y, max.Y));
    #endregion

    #region Snap
    /// <summary>
    /// Snaps the vector components to the nearest multiple of the specified grid size.
    /// </summary>
    /// <param name="gridSize">The size of the grid to snap to.</param>
    /// <returns>A vector snapped to the grid.</returns>
    public readonly Vect2 Snap(float gridSize) => Snap(this, gridSize);

    /// <summary>
    /// Snaps a vector's components to the nearest multiple of the specified grid size.
    /// </summary>
    /// <param name="value">The vector to snap.</param>
    /// <param name="gridSize">The size of the grid to snap to.</param>
    /// <returns>A vector snapped to the grid.</returns>
    public static Vect2 Snap(in Vect2 value, float gridSize)
        => new(MathHelper.Snap(value.X, gridSize), MathHelper.Snap(value.Y, gridSize));
    #endregion

    #region Center
    /// <summary>
    /// Calculates the center point between this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <param name="clamped">Whether to clamp the center to the bounds of the two vectors.</param>
    /// <returns>The center point between the vectors.</returns>
    public readonly Vect2 Center(in Vect2 other, bool clamped = false)
        => Center(this, other, clamped);

    /// <summary>
    /// Calculates the center point between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <param name="clamped">Whether to clamp the center to the bounds of the two vectors.</param>
    /// <returns>The center point between the vectors.</returns>
    public static Vect2 Center(in Vect2 a, in Vect2 b, bool clamped = false)
        => new(MathHelper.Center(a.X, b.X, clamped), MathHelper.Center(a.Y, b.Y, clamped));
    #endregion

    #region Normalize
    /// <summary>
    /// Returns a normalized version of the vector with a length of 1.
    /// </summary>
    /// <returns>A vector with the same direction but a length of 1.</returns>
    public readonly Vect2 Normalized() => Normalize(this);

    /// <summary>
    /// Normalizes a vector to have a length of 1.
    /// </summary>
    /// <param name="value">The vector to normalize.</param>
    /// <returns>A vector with the same direction but a length of 1.</returns>
    public static Vect2 Normalize(in Vect2 value)
    {
        float lenSq = LengthSquared(value);
        if (lenSq < MathHelper.Epsilon * MathHelper.Epsilon)
            return Zero;
        return value / MathF.Sqrt(lenSq);
    }
    #endregion

    #region Dot
    /// <summary>
    /// Calculates the dot product between this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public readonly float Dot(in Vect2 other) => Dot(this, other);

    /// <summary>
    /// Calculates the dot product between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static float Dot(in Vect2 a, in Vect2 b)
        => a.X * b.X + a.Y * b.Y;
    #endregion

    #region Cross (returns scalar Z for 2D)
    /// <summary>
    /// Calculates the 2D cross product (scalar) between this vector and another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The scalar cross product of the two vectors.</returns>
    public readonly float Cross(in Vect2 other) => Cross(this, other);

    /// <summary>
    /// Calculates the 2D cross product (scalar) between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The scalar cross product of the two vectors.</returns>
    public static float Cross(in Vect2 a, in Vect2 b)
        => a.X * b.Y - a.Y * b.X;
    #endregion

    #region AngleTo
    /// <summary>
    /// Calculates the angle in radians from this vector to another vector.
    /// </summary>
    /// <param name="other">The other vector.</param>
    /// <returns>The angle in radians between the vectors.</returns>
    public readonly float AngleTo(in Vect2 other) => AngleTo(this, other);

    /// <summary>
    /// Calculates the angle in radians from one vector to another vector.
    /// </summary>
    /// <param name="from">The starting vector.</param>
    /// <param name="to">The ending vector.</param>
    /// <returns>The angle in radians between the vectors.</returns>
    public static float AngleTo(in Vect2 from, in Vect2 to)
        => MathF.Atan2(to.Y - from.Y, to.X - from.X);
    #endregion

    #region Lerp
    /// <summary>
    /// Linearly interpolates between this vector and a target vector.
    /// </summary>
    /// <param name="target">The target vector.</param>
    /// <param name="t">The interpolation factor between 0 and 1.</param>
    /// <returns>The interpolated vector.</returns>
    public readonly Vect2 Lerp(in Vect2 target, float t)
        => Lerp(this, target, t);

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    /// <param name="a">The starting vector.</param>
    /// <param name="b">The ending vector.</param>
    /// <param name="t">The interpolation factor between 0 and 1.</param>
    /// <returns>The interpolated vector.</returns>
    public static Vect2 Lerp(in Vect2 a, in Vect2 b, float t)
        => new(MathHelper.Lerp(a.X, b.X, t), MathHelper.Lerp(a.Y, b.Y, t));
    #endregion

    #region MoveTo
    /// <summary>
    /// Moves this vector towards a target vector by a maximum distance.
    /// </summary>
    /// <param name="target">The target vector.</param>
    /// <param name="maxDistance">The maximum distance to move.</param>
    /// <returns>The new vector position after moving towards the target.</returns>
    public readonly Vect2 MoveTowards(in Vect2 target, float maxDistance)
        => MoveTowards(this, target, maxDistance);

    /// <summary>
    /// Moves a vector towards a target vector by a maximum distance.
    /// </summary>
    /// <param name="current">The current position.</param>
    /// <param name="target">The target position.</param>
    /// <param name="maxDistance">The maximum distance to move.</param>
    /// <returns>The new vector position after moving towards the target.</returns>
    public static Vect2 MoveTowards(in Vect2 current, in Vect2 target, float maxDistance)
    {
        float distSq = DistanceSquared(current, target);

        if (distSq < MathHelper.Epsilon * MathHelper.Epsilon || distSq <= maxDistance * maxDistance)
            return target;

        return current + (target - current) / MathF.Sqrt(distSq) * maxDistance;
    }
    #endregion

    #region Abs
    /// <summary>
    /// Returns a vector with the absolute values of each component.
    /// </summary>
    /// <returns>A vector with positive component values.</returns>
    public readonly Vect2 Abs() => Abs(this);

    /// <summary>
    /// Returns a vector with the absolute values of each component.
    /// </summary>
    /// <param name="value">The vector to take the absolute value of.</param>
    /// <returns>A vector with positive component values.</returns>
    public static Vect2 Abs(in Vect2 value)
        => new(MathF.Abs(value.X), MathF.Abs(value.Y));
    #endregion

    #region Sign
    /// <summary>
    /// Returns a vector with the sign of each component (-1, 0, or 1).
    /// </summary>
    /// <returns>A vector containing the sign of each component.</returns>
    public readonly Vect2 Sign() => Sign(this);

    /// <summary>
    /// Returns a vector with the sign of each component (-1, 0, or 1).
    /// </summary>
    /// <param name="value">The vector to get the sign of.</param>
    /// <returns>A vector containing the sign of each component.</returns>
    public static Vect2 Sign(in Vect2 value)
        => new(MathF.Sign(value.X), MathF.Sign(value.Y));
    #endregion

    #region Reflect
    /// <summary>
    /// Reflects this vector off a surface with the specified normal.
    /// </summary>
    /// <param name="normal">The normal vector of the surface to reflect off of.</param>
    /// <returns>The reflected vector.</returns>
    public readonly Vect2 Reflect(in Vect2 normal) => Reflect(this, normal);

    /// <summary>
    /// Reflects a vector off a surface with the specified normal.
    /// </summary>
    /// <param name="value">The vector to reflect.</param>
    /// <param name="normal">The normal vector of the surface to reflect off of.</param>
    /// <returns>The reflected vector.</returns>
    public static Vect2 Reflect(in Vect2 value, in Vect2 normal)
        => value - 2f * Dot(value, normal) * normal;
    #endregion

    #region Rotate
    /// <summary>
    /// Rotates the vector by the specified angle in radians.
    /// </summary>
    /// <param name="radians">The angle in radians to rotate the vector by.</param>
    /// <returns>The rotated vector.</returns>
    public readonly Vect2 Rotate(float radians) => Rotate(this, radians);

    /// <summary>
    /// Rotates a vector by the specified angle in radians.
    /// </summary>
    /// <param name="value">The vector to rotate.</param>
    /// <param name="radians">The angle in radians to rotate the vector by.</param>
    /// <returns>The rotated vector.</returns>
    public static Vect2 Rotate(in Vect2 value, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new(value.X * cos - value.Y * sin, value.X * sin + value.Y * cos);
    }
    #endregion

    #region Perpendicular (90° clockwise)
    /// <summary>
    /// Returns a vector perpendicular to this vector (rotated 90° clockwise).
    /// </summary>
    /// <returns>A perpendicular vector.</returns>
    public readonly Vect2 Perpendicular() => Perpendicular(this);

    /// <summary>
    /// Returns a vector perpendicular to the specified vector (rotated 90° clockwise).
    /// </summary>
    /// <param name="value">The vector to get the perpendicular of.</param>
    /// <returns>A perpendicular vector.</returns>
    public static Vect2 Perpendicular(in Vect2 value)
        => new(value.Y, -value.X);
    #endregion

    #region IEquatable
    /// <summary>
    /// Determines whether the current vector is equal to another vector.
    /// </summary>
    public readonly bool Equals(Vect2 other)
        => MathHelper.AlmostEquals(X, other.X, MathHelper.Epsilon)
        && MathHelper.AlmostEquals(Y, other.Y, MathHelper.Epsilon);

    /// <summary>
    /// Determines whether the current vector is equal to the specified object.
    /// </summary>
    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Vect2 value && Equals(value);

    /// <summary>
    /// Returns the hash code for the current vector.
    /// </summary>
    public readonly override int GetHashCode()
    {
        int x = (int)MathF.Round(X / MathHelper.Epsilon);
        int y = (int)MathF.Round(Y / MathHelper.Epsilon);
        return HashCode.Combine(x, y);
    }

    /// <summary>
    /// Returns a string representation of the current vector.
    /// </summary>
    public readonly override string ToString()
        => $"Vect2({X}, {Y})";
    #endregion
}