namespace Void.Engine.Systems;

public struct Rect2 : IEquatable<Rect2>
{
    #region Fields
    private Vect2 _position, _size;
    private static readonly Rect2 _rectEmpty = new(0, 0, 0, 0);
    #endregion



    #region Properties
    public float X { get => _position.X; set => _position.X = value; }
    public float Y { get => _position.Y; set => _position.Y = value; }
    public float Width { get => _size.X; set => _size.X = value; }
    public float Height { get => _size.Y; set => _size.Y = value; }
    public Vect2 Position { get => _position; set => _position = value; }
    public Vect2 Size { get => _size; set => _size = value; }

    public static Rect2 Empty => _rectEmpty;

    public readonly float Top => _position.Y;
    public readonly float Left => _position.X;
    public readonly float Right => _position.X + _size.X;
    public readonly float Bottom => _position.Y + _size.Y;
    public readonly Vect2 Center => _position + _size * 0.5f;
    public readonly Vect2 TopLeft => _position;
    public readonly Vect2 TopRight => new(Right, Top);
    public readonly Vect2 BottomLeft => new(Left, Bottom);
    public readonly Vect2 BottomRight => new(Right, Bottom);
    public readonly bool IsEmpty
        => MathHelper.AlmostZero(_size.X, MathHelper.Epsilon) && MathHelper.AlmostZero(_size.Y, MathHelper.Epsilon);

    #endregion



    #region Constructor
    public Rect2(Vect2 position, Vect2 size)
    {
        _position = position;
        _size = size;
    }

    public Rect2(float x, float y, float width, float height)
        : this(new(x, y), new(width, height)) { }
    #endregion



    #region Contains
    public readonly bool Contains(Vect2 point) => Contains(this, point);
    public static bool Contains(in Rect2 rect, in Vect2 point)
        => point.X >= rect.Left && point.X <= rect.Right &&
           point.Y >= rect.Top && point.Y <= rect.Bottom;

    public readonly bool Contains(in Rect2 other) => Contains(this, other);
    public static bool Contains(in Rect2 a, in Rect2 b)
        => b.Left >= a.Left && b.Right <= a.Right &&
           b.Top >= a.Top && b.Bottom <= a.Bottom;
    #endregion



    #region Intersects
    public readonly bool Intersects(in Rect2 other) => Intersects(this, other);
    public static bool Intersects(in Rect2 a, in Rect2 b)
        => a.Left < b.Right && a.Right > b.Left &&
           a.Top < b.Bottom && a.Bottom > b.Top;
    public readonly bool Intersects(in Vect2 center, float radius) => Intersects(this, center, radius);
    public static bool Intersects(in Rect2 rect, in Vect2 center, float radius)
    {
        Vect2 closest = center.Clamp(rect.TopLeft, rect.BottomRight);
        return Vect2.DistanceSquared(center, closest) <= radius * radius;
    }
    #endregion



    #region Intersection
    public readonly Rect2 Intersection(in Rect2 other) => Intersection(this, other);
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

    public readonly Rect2 Intersection(Vect2 center, float radius) => Intersection(this, center, radius);
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
    public readonly Rect2 Union(in Rect2 other) => Union(this, other);
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
    public readonly Rect2 Inflate(float amount) => Inflate(this, amount);
    public static Rect2 Inflate(in Rect2 rect, float amount)
        => new(rect._position - new Vect2(amount), rect._size + new Vect2(amount * 2f));
    public readonly Rect2 Inflate(float horizontal, float vertical) => Inflate(this, horizontal, vertical);
    public static Rect2 Inflate(in Rect2 rect, float horizontal, float vertical)
        => new(rect._position - new Vect2(horizontal, vertical),
               rect._size + new Vect2(horizontal * 2f, vertical * 2f));
    #endregion



    #region Offset
    public readonly Rect2 Offset(Vect2 offset) => Offset(this, offset);
    public static Rect2 Offset(in Rect2 rect, in Vect2 offset)
        => new(rect._position + offset, rect._size);

    public readonly Rect2 Offset(float x, float y) => Offset(this, x, y);
    public static Rect2 Offset(in Rect2 rect, float x, float y)
        => new(rect._position.X + x, rect._position.Y + y, rect._size.X, rect._size.Y);
    #endregion



    #region Move
    public readonly Rect2 Move(Vect2 newPosition)
        => Move(this, newPosition);
    public static Rect2 Move(in Rect2 rect, in Vect2 newPosition)
        => new(newPosition, rect._size);

    public readonly Rect2 Move(float x, float y) => Move(this, x, y);
    public static Rect2 Move(in Rect2 rect, float x, float y)
        => new(x, y, rect._size.X, rect._size.Y);
    #endregion



    #region Area
    public readonly float Area() => Area(this);
    public static float Area(in Rect2 rect) => rect._size.X * rect._size.Y;
    #endregion



    #region Operators
    public static bool operator ==(in Rect2 a, in Rect2 b) => a.Equals(b);
    public static bool operator !=(in Rect2 a, in Rect2 b) => !a.Equals(b);

    public static implicit operator Rect2(in SFFloatRect v) => new(v.Left, v.Top, v.Width, v.Height);
    public static implicit operator Rect2(in SFIntRect v) => new(v.Left, v.Top, v.Width, v.Height);
    public static implicit operator SFFloatRect(in Rect2 v) => new(v._position.X, v._position.Y, v._size.X, v._size.Y);
    public static implicit operator SFIntRect(in Rect2 v) => new((int)v._position.X, (int)v._position.Y, (int)v._size.X, (int)v._size.Y);
    #endregion



    #region IEquatable
    public readonly bool Equals(Rect2 other)
        => _position.Equals(other._position) && _size.Equals(other._size);

    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Rect2 value && Equals(value);

    public readonly override int GetHashCode()
        => HashCode.Combine(_position.GetHashCode(), _size.GetHashCode());

    public readonly override string ToString()
        => $"Rect2({_position.X}, {_position.Y}, {_size.X}, {_size.Y})";
    #endregion
}