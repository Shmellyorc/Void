// ============================================================================
//  Rect2.cs
// ============================================================================
//  2D axis-aligned rectangle with position, size, collision helpers,
//  interpolation, movement, and common geometric operations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Helpers;

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 2D axis-aligned rectangle defined by a position and size.
/// </summary>
/// <remarks>
/// <para>
/// The rectangle uses a top-left position with positive X extending right and
/// positive Y extending downward.
/// </para>
/// <para>
/// Point, rectangle, and circle collision tests delegate to
/// <see cref="CollisionHelper"/>.
/// </para>
/// </remarks>
public struct Rect2 : IEquatable<Rect2>
{
    #region Fields
    private Vect2 _position, _size;
    private static readonly Rect2 _rectEmpty = new(0, 0, 0, 0);
    #endregion

    #region Properties
    /// <summary>Gets or sets the X coordinate of the rectangle position.</summary>
    public float X { get => _position.X; set => _position.X = value; }

    /// <summary>Gets or sets the Y coordinate of the rectangle position.</summary>
    public float Y { get => _position.Y; set => _position.Y = value; }

    /// <summary>Gets or sets the rectangle width.</summary>
    public float Width { get => _size.X; set => _size.X = value; }

    /// <summary>Gets or sets the rectangle height.</summary>
    public float Height { get => _size.Y; set => _size.Y = value; }

    /// <summary>Gets or sets the top-left position.</summary>
    public Vect2 Position { get => _position; set => _position = value; }

    /// <summary>Gets or sets the width and height.</summary>
    public Vect2 Size { get => _size; set => _size = value; }

    /// <summary>Gets a rectangle at the origin with zero width and height.</summary>
    public static Rect2 Empty => _rectEmpty;

    /// <summary>Gets the Y coordinate of the top edge.</summary>
    public readonly float Top => _position.Y;

    /// <summary>Gets the X coordinate of the left edge.</summary>
    public readonly float Left => _position.X;

    /// <summary>Gets the X coordinate of the right edge.</summary>
    public readonly float Right => _position.X + _size.X;

    /// <summary>Gets the Y coordinate of the bottom edge.</summary>
    public readonly float Bottom => _position.Y + _size.Y;

    /// <summary>Gets the center point.</summary>
    public readonly Vect2 Center => _position + _size * 0.5f;

    /// <summary>Gets the top-left corner.</summary>
    public readonly Vect2 TopLeft => _position;

    /// <summary>Gets the top-right corner.</summary>
    public readonly Vect2 TopRight => new(Right, Top);

    /// <summary>Gets the bottom-left corner.</summary>
    public readonly Vect2 BottomLeft => new(Left, Bottom);

    /// <summary>Gets the bottom-right corner.</summary>
    public readonly Vect2 BottomRight => new(Right, Bottom);

    /// <summary>
    /// Gets whether both width and height are approximately zero.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="MathHelper.Epsilon"/> for both size components.
    /// </remarks>
    public readonly bool IsEmpty
        => MathHelper.AlmostZero(_size.X, MathHelper.Epsilon) && MathHelper.AlmostZero(_size.Y, MathHelper.Epsilon);
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a rectangle from a top-left position and size.
    /// </summary>
    /// <param name="position">The top-left position.</param>
    /// <param name="size">The width and height.</param>
    public Rect2(Vect2 position, Vect2 size)
    {
        _position = position;
        _size = size;
    }

    /// <summary>
    /// Creates a rectangle from position and size components.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Rect2(float x, float y, float width, float height)
        : this(new(x, y), new(width, height)) { }
    #endregion

    #region Contains
    /// <summary>
    /// Determines whether this rectangle contains a point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> if the point is contained; otherwise, <see langword="false"/>.</returns>
    public readonly bool Contains(Vect2 point) => Contains(this, point);

    /// <summary>
    /// Determines whether a rectangle contains a point.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> if the point is contained; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(in Rect2 rect, in Vect2 point)
        => CollisionHelper.PointRect(point, rect);

    /// <summary>
    /// Determines whether this rectangle fully contains another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to test.</param>
    /// <returns><see langword="true"/> if the rectangle is fully contained; otherwise, <see langword="false"/>.</returns>
    public readonly bool Contains(in Rect2 other) => Contains(this, other);

    /// <summary>
    /// Determines whether one rectangle fully contains another rectangle.
    /// </summary>
    /// <param name="a">The outer rectangle.</param>
    /// <param name="b">The rectangle to test.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> fully contains <paramref name="b"/>; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(in Rect2 a, in Rect2 b)
        => CollisionHelper.RectContainsRect(a, b);
    #endregion

    #region Intersects
    /// <summary>
    /// Determines whether this rectangle intersects another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to test.</param>
    /// <returns><see langword="true"/> if the rectangles intersect; otherwise, <see langword="false"/>.</returns>
    public readonly bool Intersects(in Rect2 other) => Intersects(this, other);

    /// <summary>
    /// Determines whether two rectangles intersect.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns><see langword="true"/> if the rectangles intersect; otherwise, <see langword="false"/>.</returns>
    public static bool Intersects(in Rect2 a, in Rect2 b)
        => CollisionHelper.RectRect(a, b);

    /// <summary>
    /// Determines whether this rectangle intersects a circle.
    /// </summary>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The circle radius.</param>
    /// <returns><see langword="true"/> if the rectangle and circle intersect; otherwise, <see langword="false"/>.</returns>
    public readonly bool Intersects(in Vect2 center, float radius) => Intersects(this, center, radius);

    /// <summary>
    /// Determines whether a rectangle intersects a circle.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The circle radius.</param>
    /// <returns><see langword="true"/> if the rectangle and circle intersect; otherwise, <see langword="false"/>.</returns>
    public static bool Intersects(in Rect2 rect, in Vect2 center, float radius)
        => CollisionHelper.RectCircle(rect, center, radius);
    #endregion

    #region Intersection
    /// <summary>
    /// Returns the overlapping area between this rectangle and another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to intersect.</param>
    /// <returns>The overlapping rectangle, or <see cref="Empty"/> when no positive-area overlap exists.</returns>
    public readonly Rect2 Intersection(in Rect2 other) => Intersection(this, other);

    /// <summary>
    /// Returns the overlapping area between two rectangles.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns>The overlapping rectangle, or <see cref="Empty"/> when no positive-area overlap exists.</returns>
    public static Rect2 Intersection(in Rect2 a, in Rect2 b)
    {
        float left = MathF.Max(a.Left, b.Left);
        float top = MathF.Max(a.Top, b.Top);
        float right = MathF.Min(a.Right, b.Right);
        float bottom = MathF.Min(a.Bottom, b.Bottom);

        if (left >= right || top >= bottom)
            return Empty;

        return new(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Returns the rectangular overlap between this rectangle and a circle.
    /// </summary>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The circle radius.</param>
    /// <returns>The overlapping rectangle, or <see cref="Empty"/> when there is no positive-area overlap.</returns>
    public readonly Rect2 Intersection(Vect2 center, float radius) => Intersection(this, center, radius);

    /// <summary>
    /// Returns the rectangular overlap between a rectangle and a circle.
    /// </summary>
    /// <param name="rect">The rectangle to intersect.</param>
    /// <param name="center">The circle center.</param>
    /// <param name="radius">The circle radius.</param>
    /// <returns>The overlapping rectangle, or <see cref="Empty"/> when there is no positive-area overlap.</returns>
    public static Rect2 Intersection(in Rect2 rect, Vect2 center, float radius)
    {
        Vect2 clamped = center.Clamp(rect.TopLeft, rect.BottomRight);

        if (Vect2.DistanceSquared(center, clamped) > radius * radius)
            return Empty;

        float left = MathF.Max(rect.Left, center.X - radius);
        float top = MathF.Max(rect.Top, center.Y - radius);
        float right = MathF.Min(rect.Right, center.X + radius);
        float bottom = MathF.Min(rect.Bottom, center.Y + radius);

        if (left >= right || top >= bottom)
            return Empty;

        return new(left, top, right - left, bottom - top);
    }
    #endregion

    #region Union
    /// <summary>
    /// Returns the smallest rectangle containing this rectangle and another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to include.</param>
    /// <returns>The union of both rectangles.</returns>
    public readonly Rect2 Union(in Rect2 other) => Union(this, other);

    /// <summary>
    /// Returns the smallest rectangle containing two rectangles.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns>The union of both rectangles.</returns>
    public static Rect2 Union(in Rect2 a, in Rect2 b)
    {
        float left = MathF.Min(a.Left, b.Left);
        float top = MathF.Min(a.Top, b.Top);
        float right = MathF.Max(a.Right, b.Right);
        float bottom = MathF.Max(a.Bottom, b.Bottom);

        return new(left, top, right - left, bottom - top);
    }
    #endregion

    #region Inflate
    /// <summary>
    /// Expands this rectangle equally on all sides.
    /// </summary>
    /// <param name="amount">The amount added to each side.</param>
    /// <returns>The inflated rectangle.</returns>
    public readonly Rect2 Inflate(float amount) => Inflate(this, amount);

    /// <summary>
    /// Expands a rectangle equally on all sides.
    /// </summary>
    /// <param name="rect">The rectangle to inflate.</param>
    /// <param name="amount">The amount added to each side.</param>
    /// <returns>The inflated rectangle.</returns>
    public static Rect2 Inflate(in Rect2 rect, float amount)
        => new(rect._position - new Vect2(amount), rect._size + new Vect2(amount * 2f));

    /// <summary>
    /// Expands this rectangle by separate horizontal and vertical amounts.
    /// </summary>
    /// <param name="horizontal">The amount added to the left and right sides.</param>
    /// <param name="vertical">The amount added to the top and bottom sides.</param>
    /// <returns>The inflated rectangle.</returns>
    public readonly Rect2 Inflate(float horizontal, float vertical) => Inflate(this, horizontal, vertical);

    /// <summary>
    /// Expands a rectangle by separate horizontal and vertical amounts.
    /// </summary>
    /// <param name="rect">The rectangle to inflate.</param>
    /// <param name="horizontal">The amount added to the left and right sides.</param>
    /// <param name="vertical">The amount added to the top and bottom sides.</param>
    /// <returns>The inflated rectangle.</returns>
    public static Rect2 Inflate(in Rect2 rect, float horizontal, float vertical)
        => new(rect._position - new Vect2(horizontal, vertical),
               rect._size + new Vect2(horizontal * 2f, vertical * 2f));
    #endregion


    #region Lerp
    /// <summary>
    /// Linearly interpolates this rectangle toward a target rectangle.
    /// </summary>
    /// <param name="target">The target rectangle.</param>
    /// <param name="t">The interpolation amount passed to <see cref="Vect2.Lerp(Vect2, Vect2, float)"/>.</param>
    /// <returns>The interpolated rectangle.</returns>
    public readonly Rect2 Lerp(Rect2 target, float t) => Lerp(this, target, t);

    /// <summary>
    /// Linearly interpolates between two rectangles.
    /// </summary>
    /// <param name="a">The starting rectangle.</param>
    /// <param name="b">The target rectangle.</param>
    /// <param name="t">The interpolation amount passed to <see cref="Vect2.Lerp(Vect2, Vect2, float)"/>.</param>
    /// <returns>The interpolated rectangle.</returns>
    public static Rect2 Lerp(Rect2 a, Rect2 b, float t)
    {
        return new Rect2(
            Vect2.Lerp(a._position, b._position, t),
            Vect2.Lerp(a._size, b._size, t)
        );
    }
    #endregion

    #region SmoothStep
    /// <summary>
    /// Smoothly interpolates this rectangle toward a target rectangle.
    /// </summary>
    /// <param name="target">The target rectangle.</param>
    /// <param name="t">The interpolation amount passed to <see cref="Vect2.SmoothStep(Vect2, Vect2, float)"/>.</param>
    /// <returns>The smoothly interpolated rectangle.</returns>
    public readonly Rect2 SmoothStep(Rect2 target, float t) => SmoothStep(this, target, t);

    /// <summary>
    /// Smoothly interpolates between two rectangles.
    /// </summary>
    /// <param name="a">The starting rectangle.</param>
    /// <param name="b">The target rectangle.</param>
    /// <param name="t">The interpolation amount passed to <see cref="Vect2.SmoothStep(Vect2, Vect2, float)"/>.</param>
    /// <returns>The smoothly interpolated rectangle.</returns>
    public static Rect2 SmoothStep(Rect2 a, Rect2 b, float t)
    {
        return new Rect2(
            Vect2.SmoothStep(a._position, b._position, t),
            Vect2.SmoothStep(a._size, b._size, t)
        );
    }
    #endregion


    #region Offset
    /// <summary>
    /// Returns this rectangle translated by a vector.
    /// </summary>
    /// <param name="offset">The translation amount.</param>
    /// <returns>The translated rectangle.</returns>
    public readonly Rect2 Offset(Vect2 offset) => Offset(this, offset);

    /// <summary>
    /// Returns a rectangle translated by a vector.
    /// </summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="offset">The translation amount.</param>
    /// <returns>The translated rectangle.</returns>
    public static Rect2 Offset(in Rect2 rect, in Vect2 offset)
        => new(rect._position + offset, rect._size);

    /// <summary>
    /// Returns this rectangle translated by X and Y offsets.
    /// </summary>
    /// <param name="x">The X offset.</param>
    /// <param name="y">The Y offset.</param>
    /// <returns>The translated rectangle.</returns>
    public readonly Rect2 Offset(float x, float y) => Offset(this, x, y);

    /// <summary>
    /// Returns a rectangle translated by X and Y offsets.
    /// </summary>
    /// <param name="rect">The rectangle to translate.</param>
    /// <param name="x">The X offset.</param>
    /// <param name="y">The Y offset.</param>
    /// <returns>The translated rectangle.</returns>
    public static Rect2 Offset(in Rect2 rect, float x, float y)
        => new(rect._position.X + x, rect._position.Y + y, rect._size.X, rect._size.Y);
    #endregion

    #region Move
    /// <summary>
    /// Returns this rectangle at a new position while preserving its size.
    /// </summary>
    /// <param name="newPosition">The new top-left position.</param>
    /// <returns>The moved rectangle.</returns>
    public readonly Rect2 Move(Vect2 newPosition)
        => Move(this, newPosition);

    /// <summary>
    /// Returns a rectangle at a new position while preserving its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="newPosition">The new top-left position.</param>
    /// <returns>The moved rectangle.</returns>
    public static Rect2 Move(in Rect2 rect, in Vect2 newPosition)
        => new(newPosition, rect._size);

    /// <summary>
    /// Returns this rectangle at a new X and Y position while preserving its size.
    /// </summary>
    /// <param name="x">The new X coordinate.</param>
    /// <param name="y">The new Y coordinate.</param>
    /// <returns>The moved rectangle.</returns>
    public readonly Rect2 Move(float x, float y) => Move(this, x, y);

    /// <summary>
    /// Returns a rectangle at a new X and Y position while preserving its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="x">The new X coordinate.</param>
    /// <param name="y">The new Y coordinate.</param>
    /// <returns>The moved rectangle.</returns>
    public static Rect2 Move(in Rect2 rect, float x, float y)
        => new(x, y, rect._size.X, rect._size.Y);
    #endregion

    #region Area
    /// <summary>
    /// Returns the rectangle area.
    /// </summary>
    /// <returns>Width multiplied by height.</returns>
    public readonly float Area() => Area(this);

    /// <summary>
    /// Returns the area of a rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to measure.</param>
    /// <returns>Width multiplied by height.</returns>
    public static float Area(in Rect2 rect) => rect._size.X * rect._size.Y;
    #endregion


    #region Damp
    /// <summary>
    /// Smoothly damps this rectangle toward a target rectangle.
    /// </summary>
    /// <param name="target">The target rectangle.</param>
    /// <param name="smoothing">The smoothing factor.</param>
    /// <param name="dt">Delta time in seconds.</param>
    /// <returns>The damped rectangle.</returns>
    public readonly Rect2 Damp(Rect2 target, float smoothing, float dt)
        => Damp(this, target, smoothing, dt);

    /// <summary>
    /// Smoothly damps one rectangle toward another.
    /// </summary>
    /// <param name="a">The starting rectangle.</param>
    /// <param name="b">The target rectangle.</param>
    /// <param name="smoothing">The smoothing factor.</param>
    /// <param name="dt">Delta time in seconds.</param>
    /// <returns>The damped rectangle.</returns>
    public static Rect2 Damp(Rect2 a, Rect2 b, float smoothing, float dt) => new(
        Vect2.Damp(a.Position, b.Position, smoothing, dt),
        Vect2.Damp(a.Size, b.Size, smoothing, dt)
    );
    #endregion


    #region Operators
    /// <summary>Determines whether two rectangles are equal.</summary>
    public static bool operator ==(in Rect2 a, in Rect2 b) => a.Equals(b);

    /// <summary>Determines whether two rectangles are not equal.</summary>
    public static bool operator !=(in Rect2 a, in Rect2 b) => !a.Equals(b);
    #endregion

    #region IEquatable
    /// <summary>
    /// Determines whether this rectangle is equal to another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to compare.</param>
    /// <returns><see langword="true"/> if position and size are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(Rect2 other)
        => _position.Equals(other._position) && _size.Equals(other._size);

    /// <summary>
    /// Determines whether this rectangle is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is an equal <see cref="Rect2"/>; otherwise, <see langword="false"/>.</returns>
    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Rect2 value && Equals(value);

    /// <summary>
    /// Returns the hash code for this rectangle.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public readonly override int GetHashCode()
        => HashCode.Combine(_position.GetHashCode(), _size.GetHashCode());

    /// <summary>
    /// Returns the position and size of this rectangle.
    /// </summary>
    /// <returns>A string in <c>Rect2(X, Y, Width, Height)</c> form.</returns>
    public readonly override string ToString()
        => $"Rect2({_position.X}, {_position.Y}, {_size.X}, {_size.Y})";
    #endregion
}
