// ============================================================================
//  Rect2.cs
// ============================================================================
//  2D axis-aligned rectangle structure with position, size, and common
//  geometric operations including containment, intersection, union, and
//  transformation methods.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 2D axis-aligned rectangle defined by a position and size.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Rect2"/> structure provides comprehensive geometric operations
/// for axis-aligned rectangles including containment testing, intersection
/// detection, union operations, and various transformations. It is used
/// extensively for collision detection, viewport calculations, UI layout,
/// and spatial partitioning.
/// </para>
/// <para>
/// All operations assume a coordinate system where positive X extends to the
/// right and positive Y extends downward.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Create a rectangle
/// var rect = new Rect2(10, 20, 100, 50);
/// 
/// // Check if a point is inside
/// bool contains = rect.Contains(new Vect2(50, 30));
/// 
/// // Find intersection with another rectangle
/// var other = new Rect2(30, 10, 80, 40);
/// var intersection = rect.Intersection(other);
/// 
/// // Inflate the rectangle
/// var inflated = rect.Inflate(10f);
/// </code>
/// </para>
/// </remarks>
public struct Rect2 : IEquatable<Rect2>
{
    #region Fields
    private Vect2 _position, _size;
    private static readonly Rect2 _rectEmpty = new(0, 0, 0, 0);
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the X-coordinate of the rectangle's position.
    /// </summary>
    public float X { get => _position.X; set => _position.X = value; }

    /// <summary>
    /// Gets or sets the Y-coordinate of the rectangle's position.
    /// </summary>
    public float Y { get => _position.Y; set => _position.Y = value; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public float Width { get => _size.X; set => _size.X = value; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public float Height { get => _size.Y; set => _size.Y = value; }

    /// <summary>
    /// Gets or sets the position (top-left corner) of the rectangle.
    /// </summary>
    public Vect2 Position { get => _position; set => _position = value; }

    /// <summary>
    /// Gets or sets the size (width and height) of the rectangle.
    /// </summary>
    public Vect2 Size { get => _size; set => _size = value; }

    /// <summary>
    /// Gets an empty rectangle with position (0,0) and size (0,0).
    /// </summary>
    public static Rect2 Empty => _rectEmpty;

    /// <summary>
    /// Gets the Y-coordinate of the top edge of the rectangle.
    /// </summary>
    public readonly float Top => _position.Y;

    /// <summary>
    /// Gets the X-coordinate of the left edge of the rectangle.
    /// </summary>
    public readonly float Left => _position.X;

    /// <summary>
    /// Gets the X-coordinate of the right edge of the rectangle.
    /// </summary>
    public readonly float Right => _position.X + _size.X;

    /// <summary>
    /// Gets the Y-coordinate of the bottom edge of the rectangle.
    /// </summary>
    public readonly float Bottom => _position.Y + _size.Y;

    /// <summary>
    /// Gets the center point of the rectangle.
    /// </summary>
    public readonly Vect2 Center => _position + _size * 0.5f;

    /// <summary>
    /// Gets the top-left corner of the rectangle.
    /// </summary>
    public readonly Vect2 TopLeft => _position;

    /// <summary>
    /// Gets the top-right corner of the rectangle.
    /// </summary>
    public readonly Vect2 TopRight => new(Right, Top);

    /// <summary>
    /// Gets the bottom-left corner of the rectangle.
    /// </summary>
    public readonly Vect2 BottomLeft => new(Left, Bottom);

    /// <summary>
    /// Gets the bottom-right corner of the rectangle.
    /// </summary>
    public readonly Vect2 BottomRight => new(Right, Bottom);

    /// <summary>
    /// Gets a value indicating whether the rectangle has zero size.
    /// </summary>
    public readonly bool IsEmpty
        => MathHelper.AlmostZero(_size.X, MathHelper.Epsilon) && MathHelper.AlmostZero(_size.Y, MathHelper.Epsilon);
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="Rect2"/> structure with the specified position and size.
    /// </summary>
    /// <param name="position">The position (top-left corner) of the rectangle.</param>
    /// <param name="size">The size (width and height) of the rectangle.</param>
    public Rect2(Vect2 position, Vect2 size)
    {
        _position = position;
        _size = size;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rect2"/> structure with the specified component values.
    /// </summary>
    /// <param name="x">The X-coordinate of the position.</param>
    /// <param name="y">The Y-coordinate of the position.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    public Rect2(float x, float y, float width, float height)
        : this(new(x, y), new(width, height)) { }
    #endregion

    #region Contains
    /// <summary>
    /// Determines whether the rectangle contains the specified point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> if the point is inside the rectangle; otherwise, <see langword="false"/>.</returns>
    public readonly bool Contains(Vect2 point) => Contains(this, point);

    /// <summary>
    /// Determines whether the specified rectangle contains the specified point.
    /// </summary>
    /// <param name="rect">The rectangle to test against.</param>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> if the point is inside the rectangle; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(in Rect2 rect, in Vect2 point)
        => point.X >= rect.Left && point.X <= rect.Right &&
           point.Y >= rect.Top && point.Y <= rect.Bottom;

    /// <summary>
    /// Determines whether this rectangle fully contains the specified rectangle.
    /// </summary>
    /// <param name="other">The rectangle to test.</param>
    /// <returns><see langword="true"/> if this rectangle fully contains the other; otherwise, <see langword="false"/>.</returns>
    public readonly bool Contains(in Rect2 other) => Contains(this, other);

    /// <summary>
    /// Determines whether one rectangle fully contains another rectangle.
    /// </summary>
    /// <param name="a">The outer rectangle.</param>
    /// <param name="b">The inner rectangle to test.</param>
    /// <returns><see langword="true"/> if rectangle a fully contains rectangle b; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(in Rect2 a, in Rect2 b)
        => b.Left >= a.Left && b.Right <= a.Right &&
           b.Top >= a.Top && b.Bottom <= a.Bottom;
    #endregion

    #region Intersects
    /// <summary>
    /// Determines whether this rectangle intersects with another rectangle.
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
        => a.Left < b.Right && a.Right > b.Left &&
           a.Top < b.Bottom && a.Bottom > b.Top;

    /// <summary>
    /// Determines whether this rectangle intersects with a circle defined by its center and radius.
    /// </summary>
    /// <param name="center">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the rectangle and circle intersect; otherwise, <see langword="false"/>.</returns>
    public readonly bool Intersects(in Vect2 center, float radius) => Intersects(this, center, radius);

    /// <summary>
    /// Determines whether a rectangle intersects with a circle defined by its center and radius.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <param name="center">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the rectangle and circle intersect; otherwise, <see langword="false"/>.</returns>
    public static bool Intersects(in Rect2 rect, in Vect2 center, float radius)
    {
        Vect2 closest = center.Clamp(rect.TopLeft, rect.BottomRight);
        return Vect2.DistanceSquared(center, closest) <= radius * radius;
    }
    #endregion

    #region Intersection
    /// <summary>
    /// Gets the intersection rectangle between this rectangle and another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to intersect with.</param>
    /// <returns>The intersection rectangle, or <see cref="Empty"/> if there is no intersection.</returns>
    public readonly Rect2 Intersection(in Rect2 other) => Intersection(this, other);

    /// <summary>
    /// Gets the intersection rectangle between two rectangles.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns>The intersection rectangle, or <see cref="Empty"/> if there is no intersection.</returns>
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
    /// Gets the intersection rectangle between this rectangle and a circle.
    /// </summary>
    /// <param name="center">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>The intersection rectangle, or <see cref="Empty"/> if there is no intersection.</returns>
    public readonly Rect2 Intersection(Vect2 center, float radius) => Intersection(this, center, radius);

    /// <summary>
    /// Gets the intersection rectangle between a rectangle and a circle.
    /// </summary>
    /// <param name="rect">The rectangle to intersect with.</param>
    /// <param name="center">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>The intersection rectangle, or <see cref="Empty"/> if there is no intersection.</returns>
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
    /// Gets the smallest rectangle that contains both this rectangle and another rectangle.
    /// </summary>
    /// <param name="other">The rectangle to combine with.</param>
    /// <returns>The union rectangle that contains both rectangles.</returns>
    public readonly Rect2 Union(in Rect2 other) => Union(this, other);

    /// <summary>
    /// Gets the smallest rectangle that contains two rectangles.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns>The union rectangle that contains both rectangles.</returns>
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
    /// Expands the rectangle by the specified amount on all sides.
    /// </summary>
    /// <param name="amount">The amount to expand in all directions.</param>
    /// <returns>The inflated rectangle.</returns>
    public readonly Rect2 Inflate(float amount) => Inflate(this, amount);

    /// <summary>
    /// Expands a rectangle by the specified amount on all sides.
    /// </summary>
    /// <param name="rect">The rectangle to inflate.</param>
    /// <param name="amount">The amount to expand in all directions.</param>
    /// <returns>The inflated rectangle.</returns>
    public static Rect2 Inflate(in Rect2 rect, float amount)
        => new(rect._position - new Vect2(amount), rect._size + new Vect2(amount * 2f));

    /// <summary>
    /// Expands the rectangle by different amounts horizontally and vertically.
    /// </summary>
    /// <param name="horizontal">The amount to expand horizontally.</param>
    /// <param name="vertical">The amount to expand vertically.</param>
    /// <returns>The inflated rectangle.</returns>
    public readonly Rect2 Inflate(float horizontal, float vertical) => Inflate(this, horizontal, vertical);

    /// <summary>
    /// Expands a rectangle by different amounts horizontally and vertically.
    /// </summary>
    /// <param name="rect">The rectangle to inflate.</param>
    /// <param name="horizontal">The amount to expand horizontally.</param>
    /// <param name="vertical">The amount to expand vertically.</param>
    /// <returns>The inflated rectangle.</returns>
    public static Rect2 Inflate(in Rect2 rect, float horizontal, float vertical)
        => new(rect._position - new Vect2(horizontal, vertical),
               rect._size + new Vect2(horizontal * 2f, vertical * 2f));
    #endregion

    #region Offset
    /// <summary>
    /// Offsets the rectangle by the specified vector.
    /// </summary>
    /// <param name="offset">The amount to offset the position.</param>
    /// <returns>The offset rectangle.</returns>
    public readonly Rect2 Offset(Vect2 offset) => Offset(this, offset);

    /// <summary>
    /// Offsets a rectangle by the specified vector.
    /// </summary>
    /// <param name="rect">The rectangle to offset.</param>
    /// <param name="offset">The amount to offset the position.</param>
    /// <returns>The offset rectangle.</returns>
    public static Rect2 Offset(in Rect2 rect, in Vect2 offset)
        => new(rect._position + offset, rect._size);

    /// <summary>
    /// Offsets the rectangle by the specified x and y values.
    /// </summary>
    /// <param name="x">The amount to offset the X position.</param>
    /// <param name="y">The amount to offset the Y position.</param>
    /// <returns>The offset rectangle.</returns>
    public readonly Rect2 Offset(float x, float y) => Offset(this, x, y);

    /// <summary>
    /// Offsets a rectangle by the specified x and y values.
    /// </summary>
    /// <param name="rect">The rectangle to offset.</param>
    /// <param name="x">The amount to offset the X position.</param>
    /// <param name="y">The amount to offset the Y position.</param>
    /// <returns>The offset rectangle.</returns>
    public static Rect2 Offset(in Rect2 rect, float x, float y)
        => new(rect._position.X + x, rect._position.Y + y, rect._size.X, rect._size.Y);
    #endregion

    #region Move
    /// <summary>
    /// Moves the rectangle to a new position while maintaining its size.
    /// </summary>
    /// <param name="newPosition">The new position for the rectangle.</param>
    /// <returns>The moved rectangle.</returns>
    public readonly Rect2 Move(Vect2 newPosition)
        => Move(this, newPosition);

    /// <summary>
    /// Moves a rectangle to a new position while maintaining its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="newPosition">The new position for the rectangle.</param>
    /// <returns>The moved rectangle.</returns>
    public static Rect2 Move(in Rect2 rect, in Vect2 newPosition)
        => new(newPosition, rect._size);

    /// <summary>
    /// Moves the rectangle to a new position while maintaining its size.
    /// </summary>
    /// <param name="x">The new X-coordinate for the rectangle.</param>
    /// <param name="y">The new Y-coordinate for the rectangle.</param>
    /// <returns>The moved rectangle.</returns>
    public readonly Rect2 Move(float x, float y) => Move(this, x, y);

    /// <summary>
    /// Moves a rectangle to a new position while maintaining its size.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="x">The new X-coordinate for the rectangle.</param>
    /// <param name="y">The new Y-coordinate for the rectangle.</param>
    /// <returns>The moved rectangle.</returns>
    public static Rect2 Move(in Rect2 rect, float x, float y)
        => new(x, y, rect._size.X, rect._size.Y);
    #endregion

    #region Area
    /// <summary>
    /// Gets the area of the rectangle.
    /// </summary>
    /// <returns>The area (width × height) of the rectangle.</returns>
    public readonly float Area() => Area(this);

    /// <summary>
    /// Gets the area of the specified rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to calculate area for.</param>
    /// <returns>The area (width × height) of the rectangle.</returns>
    public static float Area(in Rect2 rect) => rect._size.X * rect._size.Y;
    #endregion

    #region Operators
    /// <summary>
    /// Determines whether two rectangles are equal.
    /// </summary>
    public static bool operator ==(in Rect2 a, in Rect2 b) => a.Equals(b);

    /// <summary>
    /// Determines whether two rectangles are not equal.
    /// </summary>
    public static bool operator !=(in Rect2 a, in Rect2 b) => !a.Equals(b);

    /// <summary>
    /// Implicitly converts an SFML FloatRect to a Rect2.
    /// </summary>
    public static implicit operator Rect2(in SFFloatRect v) => new(v.Left, v.Top, v.Width, v.Height);

    /// <summary>
    /// Implicitly converts an SFML IntRect to a Rect2.
    /// </summary>
    public static implicit operator Rect2(in SFIntRect v) => new(v.Left, v.Top, v.Width, v.Height);

    /// <summary>
    /// Implicitly converts a Rect2 to an SFML FloatRect.
    /// </summary>
    public static implicit operator SFFloatRect(in Rect2 v) => new(new(v._position.X, v._position.Y), new(v._size.X, v._size.Y));

    /// <summary>
    /// Implicitly converts a Rect2 to an SFML IntRect.
    /// </summary>
    public static implicit operator SFIntRect(in Rect2 v) => new(new((int)v._position.X, (int)v._position.Y), new((int)v._size.X, (int)v._size.Y));
    #endregion

    #region IEquatable
    /// <summary>
    /// Determines whether the current rectangle is equal to another rectangle.
    /// </summary>
    public readonly bool Equals(Rect2 other)
        => _position.Equals(other._position) && _size.Equals(other._size);

    /// <summary>
    /// Determines whether the current rectangle is equal to the specified object.
    /// </summary>
    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Rect2 value && Equals(value);

    /// <summary>
    /// Returns the hash code for the current rectangle.
    /// </summary>
    public readonly override int GetHashCode()
        => HashCode.Combine(_position.GetHashCode(), _size.GetHashCode());

    /// <summary>
    /// Returns a string representation of the current rectangle.
    /// </summary>
    public readonly override string ToString()
        => $"Rect2({_position.X}, {_position.Y}, {_size.X}, {_size.Y})";
    #endregion
}