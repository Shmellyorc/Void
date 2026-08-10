namespace Void.Engine.Systems;

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
    public float X, Y;
    public static Vect2 Zero => _vectZero;
    public static Vect2 One => _vectOne;
    public static Vect2 Up => _vectUp;
    public static Vect2 Down => _vectDown;
    public static Vect2 Left => _vectLeft;
    public static Vect2 Right => _vectRight;

    public readonly bool IsZero
        => MathHelper.AlmostZero(X, MathHelper.Epsilon) && MathHelper.AlmostZero(Y, MathHelper.Epsilon);

    #endregion



    #region Constructors
    public Vect2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Vect2(float value) : this(value, value) { }
    #endregion



    #region Operators
    public static bool operator ==(in Vect2 a, in Vect2 b) => a.Equals(b);
    public static bool operator !=(in Vect2 a, in Vect2 b) => !a.Equals(b);

    public static implicit operator Vect2(in SFVector2f v) => new(v.X, v.Y);
    public static implicit operator Vect2(in SFVector2i v) => new(v.X, v.Y);
    public static implicit operator Vect2(in SFVector2u v) => new(v.X, v.Y);
    public static implicit operator SFVector2f(in Vect2 v) => new(v.X, v.Y);
    public static implicit operator SFVector2i(in Vect2 v) => new((int)v.X, (int)v.Y);
    public static implicit operator SFVector2u(in Vect2 v) => new((uint)v.X, (uint)v.Y);

    public static Vect2 operator +(in Vect2 a, in Vect2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vect2 operator +(in Vect2 a, float b) => new(a.X + b, a.Y + b);
    public static Vect2 operator +(float a, in Vect2 b) => b + a;

    public static Vect2 operator /(in Vect2 a, in Vect2 b) => new(a.X / b.X, a.Y / b.Y);
    public static Vect2 operator /(in Vect2 a, float b) => new(a.X / b, a.Y / b);
    public static Vect2 operator /(float a, in Vect2 b) => b / a;

    public static Vect2 operator *(in Vect2 a, in Vect2 b) => new(a.X * b.X, a.Y * b.Y);
    public static Vect2 operator *(in Vect2 a, float b) => new(a.X * b, a.Y * b);
    public static Vect2 operator *(float a, in Vect2 b) => b * a;

    public static Vect2 operator -(in Vect2 value) => new(-value.X, -value.Y);
    public static Vect2 operator -(in Vect2 a, in Vect2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vect2 operator -(in Vect2 a, float b) => new(a.X - b, a.Y - b);
    public static Vect2 operator -(float a, in Vect2 b) => b - a;
    #endregion



    #region Transform
    public readonly Vect2 Transform(in Camera camera)
        => Transform(this, camera);
    public static Vect2 Transform(in Vect2 mouse, in Camera camera)
        => camera.ScreenToWorld(mouse);
    #endregion



    #region DistanceSquared
    public readonly float DistanceSquared(in Vect2 other) => DistanceSquared(this, other);
    public static float DistanceSquared(in Vect2 a, in Vect2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return LengthSquared(new(dx, dy));
    }
    #endregion



    #region Distance
    public readonly float Distance(in Vect2 other) => Distance(this, other);
    public static float Distance(in Vect2 a, in Vect2 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;

        return Length(new(dx, dy));
    }
    #endregion



    #region Length
    public readonly float Length() => Length(this);
    public static float Length(in Vect2 value)
    {
        return MathF.Sqrt(value.X * value.X + value.Y * value.Y);
    }
    #endregion



    #region LengthSquared
    public readonly float LengthSquared() => LengthSquared(this);
    public static float LengthSquared(in Vect2 value)
    {
        return value.X * value.X + value.Y * value.Y;
    }
    #endregion



    #region Min
    public readonly Vect2 Min(in Vect2 other) => Min(this, other);
    public static Vect2 Min(in Vect2 a, in Vect2 b)
        => new(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y));
    #endregion



    #region Max
    public readonly Vect2 Max(in Vect2 other) => Max(this, other);
    public static Vect2 Max(in Vect2 a, in Vect2 b)
        => new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));
    #endregion



    #region Clamp
    public readonly Vect2 Clamp(in Vect2 min, in Vect2 max) => Clamp(this, min, max);
    public static Vect2 Clamp(in Vect2 value, in Vect2 min, in Vect2 max)
        => new(Math.Clamp(value.X, min.X, max.X), Math.Clamp(value.Y, min.Y, max.Y));
    #endregion



    #region Ceiling
    public readonly Vect2 Ceiling(int digits = 0) => Ceiling(this, digits);
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
    public readonly Vect2 Floor(int digits = 0) => Floor(this, digits);
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
    public readonly Vect2 Round(int digits = 0) => Round(this, digits);
    public static Vect2 Round(in Vect2 value, int digits = 0)
    {
        if (digits < 0 || digits > 6)
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be between 0 and 6.");
        return new(MathF.Round(value.X, digits), MathF.Round(value.Y, digits));
    }
    #endregion



    #region Normalize
    public readonly Vect2 Normalized() => Normalize(this);
    public static Vect2 Normalize(in Vect2 value)
    {
        float lenSq = LengthSquared(value);
        if (lenSq < MathHelper.Epsilon * MathHelper.Epsilon)
            return Zero;
        return value / MathF.Sqrt(lenSq);
    }
    #endregion


    #region Dot
    public readonly float Dot(in Vect2 other) => Dot(this, other);
    public static float Dot(in Vect2 a, in Vect2 b)
        => a.X * b.X + a.Y * b.Y;
    #endregion



    #region Cross (returns scalar Z for 2D)
    public readonly float Cross(in Vect2 other) => Cross(this, other);
    public static float Cross(in Vect2 a, in Vect2 b)
        => a.X * b.Y - a.Y * b.X;
    #endregion



    #region AngleTo
    public readonly float AngleTo(in Vect2 other) => AngleTo(this, other);
    public static float AngleTo(in Vect2 from, in Vect2 to)
        => MathF.Atan2(to.Y - from.Y, to.X - from.X);
    #endregion



    #region Lerp
    public readonly Vect2 Lerp(in Vect2 target, float t)
        => Lerp(this, target, t);
    public static Vect2 Lerp(in Vect2 a, in Vect2 b, float t)
        => new(MathHelper.Lerp(a.X, b.X, t), MathHelper.Lerp(a.Y, b.Y, t));
    #endregion


    #region MoveTo
    public readonly Vect2 MoveTowards(in Vect2 target, float maxDistance)
        => MoveTowards(this, target, maxDistance);
    public static Vect2 MoveTowards(in Vect2 current, in Vect2 target, float maxDistance)
    {
        float distSq = DistanceSquared(current, target);

        if (distSq < MathHelper.Epsilon * MathHelper.Epsilon || distSq <= maxDistance * maxDistance)
            return target;

        return current + (target - current) / MathF.Sqrt(distSq) * maxDistance;
    }
    #endregion



    #region Abs
    public readonly Vect2 Abs() => Abs(this);
    public static Vect2 Abs(in Vect2 value)
        => new(MathF.Abs(value.X), MathF.Abs(value.Y));
    #endregion



    #region Sign
    public readonly Vect2 Sign() => Sign(this);
    public static Vect2 Sign(in Vect2 value)
        => new(MathF.Sign(value.X), MathF.Sign(value.Y));
    #endregion



    #region Reflect
    public readonly Vect2 Reflect(in Vect2 normal) => Reflect(this, normal);
    public static Vect2 Reflect(in Vect2 value, in Vect2 normal)
        => value - 2f * Dot(value, normal) * normal;
    #endregion



    #region Rotate
    public readonly Vect2 Rotate(float radians) => Rotate(this, radians);
    public static Vect2 Rotate(in Vect2 value, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new(value.X * cos - value.Y * sin, value.X * sin + value.Y * cos);
    }
    #endregion



    #region Perpendicular (90° clockwise)
    public readonly Vect2 Perpendicular() => Perpendicular(this);
    public static Vect2 Perpendicular(in Vect2 value)
        => new(value.Y, -value.X);
    #endregion



    #region IEquatable
    public readonly bool Equals(Vect2 other)
        => MathHelper.AlmostEquals(X, other.X, MathHelper.Epsilon)
        && MathHelper.AlmostEquals(Y, other.Y, MathHelper.Epsilon);

    public readonly override bool Equals([NotNullWhen(true)] object obj)
        => obj is Vect2 value && Equals(value);

    public readonly override int GetHashCode()
    {
        int x = (int)MathF.Round(X / MathHelper.Epsilon);
        int y = (int)MathF.Round(Y / MathHelper.Epsilon);
        return HashCode.Combine(x, y);
    }

    public readonly override string ToString()
        => $"Vect2({X}, {Y})";
    #endregion
}
