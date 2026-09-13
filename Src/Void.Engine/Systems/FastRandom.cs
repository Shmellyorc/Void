// ============================================================================
//  FastRandom.cs
// ============================================================================
//  High-performance thread-local random number generator using Xorshift128.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Provides a fast pseudo-random number generator using the Xorshift128 algorithm.
/// </summary>
/// <remarks>
/// <para>
/// <c>Next*</c> methods follow <see cref="System.Random"/>-style range semantics:
/// the minimum is inclusive and the maximum is exclusive. The <c>Range*</c>
/// methods are VOID convenience methods whose minimum and maximum are both inclusive.
/// </para>
/// <para>
/// <see cref="Shared"/> returns one generator per thread, avoiding synchronization
/// between callers on different threads. Explicitly seeded instances remain useful
/// when a repeatable sequence is required.
/// </para>
/// <code>
/// int value = FastRandom.Shared.Next(0, 100);       // 0..99
/// float unit = FastRandom.Shared.NextFloat();       // 0 &lt;= value &lt; 1
/// int inclusive = FastRandom.Shared.RangeInt(0, 5); // 0..5
/// </code>
/// </remarks>
public sealed class FastRandom
{
    private const int Y = 0x2B5B9F51;
    private const int Z = 0x4F59A821;
    private const int W = 0x6F5B9D5B;

    private const ulong UInt32Range = 1UL << 32;
    private const float FloatExclusiveScale = 1f / 16777216f; // 2^24
    private const float FloatInclusiveScale = 1f / 16777215f; // 2^24 - 1

    private uint _x, _y, _z, _w;

    private static readonly ThreadLocal<FastRandom> _threadLocal = new(() => new FastRandom());

    /// <summary>
    /// Gets a thread-local shared generator for the current thread.
    /// </summary>
    public static FastRandom Shared => _threadLocal.Value;

    /// <summary>
    /// Initializes a generator with the specified deterministic seed.
    /// </summary>
    /// <param name="seed">Seed used to initialize the generator state.</param>
    public FastRandom(int seed)
    {
        var s = (uint)seed;

        _x = s;
        _y = Y;
        _z = Z;
        _w = W;

        for (int i = 0; i < 10; i++)
            NextUInt();
    }

    /// <summary>
    /// Initializes a generator using a seed derived from time and the current managed thread.
    /// </summary>
    public FastRandom() : this(GenerateSeed()) { }

    /// <summary>
    /// Returns <see langword="true"/> or <see langword="false"/> with approximately equal probability.
    /// </summary>
    public bool NextBoolean()
    {
        return (NextUInt() & 1) == 0;
    }

    /// <summary>
    /// Returns a non-negative integer from zero inclusive to <see cref="int.MaxValue"/> exclusive.
    /// </summary>
    public int Next()
    {
        var rtn = NextUInt() & 0x7FFFFFFF;
        if (rtn == 0x7FFFFFFF)
            return Next();
        return (int)rtn;
    }

    /// <summary>
    /// Returns a non-negative integer from zero inclusive to <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="maxValue">
    /// Exclusive upper bound. Zero is allowed and returns zero.
    /// </param>
    /// <returns>
    /// A value greater than or equal to zero and less than <paramref name="maxValue"/>,
    /// or zero when <paramref name="maxValue"/> is zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is negative.
    /// </exception>
    public int Next(int maxValue)
    {
        if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to zero");

        if (maxValue == 0)
            return 0;

        return (int)NextUIntBounded((ulong)maxValue);
    }

    /// <summary>
    /// Returns an integer from <paramref name="minValue"/> inclusive to
    /// <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="minValue">Inclusive lower bound.</param>
    /// <param name="maxValue">Exclusive upper bound.</param>
    /// <returns>
    /// A value greater than or equal to <paramref name="minValue"/> and less than
    /// <paramref name="maxValue"/>. When both bounds are equal, that value is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.
    /// </exception>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to minValue");

        ulong range = (ulong)((long)maxValue - minValue);
        if (range == 0)
            return minValue;

        return (int)((long)minValue + NextUIntBounded(range));
    }

    /// <summary>
    /// Returns a double from zero inclusive to one exclusive.
    /// </summary>
    public double NextDouble()
    {
        return NextUInt() / 4294967296.0;
    }

    /// <summary>
    /// Returns a double from zero inclusive to <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="maxValue">
    /// Exclusive upper bound. Zero is allowed and returns zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is negative.
    /// </exception>
    public double NextDouble(double maxValue)
    {
        if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to zero");

        if (maxValue == 0)
            return 0;

        double result = NextDouble() * maxValue;
        return result < maxValue ? result : Math.BitDecrement(maxValue);
    }

    /// <summary>
    /// Returns a double from <paramref name="minValue"/> inclusive to
    /// <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="minValue">Inclusive lower bound.</param>
    /// <param name="maxValue">Exclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.
    /// </exception>
    public double NextDouble(double minValue, double maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to minValue");

        if (minValue == maxValue)
            return minValue;

        double result = NextDouble() * (maxValue - minValue) + minValue;
        return result < maxValue ? result : Math.BitDecrement(maxValue);
    }

    /// <summary>
    /// Returns a float from zero inclusive to one exclusive.
    /// </summary>
    public float NextFloat()
    {
        return (NextUInt() >> 8) * FloatExclusiveScale;
    }

    /// <summary>
    /// Returns a float from zero inclusive to <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="maxValue">
    /// Exclusive upper bound. Zero is allowed and returns zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is negative.
    /// </exception>
    public float NextFloat(float maxValue)
    {
        if (maxValue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to zero");

        if (maxValue == 0)
            return 0;

        float result = NextFloat() * maxValue;
        return result < maxValue ? result : MathF.BitDecrement(maxValue);
    }

    /// <summary>
    /// Returns a float from <paramref name="minValue"/> inclusive to
    /// <paramref name="maxValue"/> exclusive.
    /// </summary>
    /// <param name="minValue">Inclusive lower bound.</param>
    /// <param name="maxValue">Exclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.
    /// </exception>
    public float NextFloat(float minValue, float maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than or equal to minValue");

        if (minValue == maxValue)
            return minValue;

        float result = NextFloat() * (maxValue - minValue) + minValue;
        return result < maxValue ? result : MathF.BitDecrement(maxValue);
    }

    /// <summary>
    /// Returns an integer from <paramref name="min"/> through <paramref name="max"/>,
    /// including both bounds.
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="max"/> is less than <paramref name="min"/>.
    /// </exception>
    public int RangeInt(int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater than or equal to min");

        ulong range = (ulong)((long)max - min) + 1UL;
        return (int)((long)min + NextUIntBounded(range));
    }

    /// <summary>
    /// Returns a float from <paramref name="min"/> through <paramref name="max"/>,
    /// including both bounds.
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="max"/> is less than <paramref name="min"/>.
    /// </exception>
    public float RangeFloat(float min, float max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater than or equal to min");

        if (min == max)
            return min;

        float unit = (NextUInt() >> 8) * FloatInclusiveScale;
        return unit * (max - min) + min;
    }

    /// <summary>
    /// Returns a double from <paramref name="min"/> through <paramref name="max"/>,
    /// including both bounds.
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="max"/> is less than <paramref name="min"/>.
    /// </exception>
    public double RangeDouble(double min, double max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater than or equal to min");

        if (min == max)
            return min;

        double unit = NextUInt() / (double)uint.MaxValue;
        return unit * (max - min) + min;
    }

    private static int GenerateSeed()
    {
        return (int)(Environment.TickCount ^ Environment.CurrentManagedThreadId ^ (uint)DateTime.Now.Ticks);
    }

    private uint NextUIntBounded(ulong upperExclusive)
    {
        if (upperExclusive == 0 || upperExclusive > UInt32Range)
            throw new ArgumentOutOfRangeException(nameof(upperExclusive));

        if (upperExclusive == UInt32Range)
            return NextUInt();

        ulong limit = UInt32Range - (UInt32Range % upperExclusive);

        uint value;
        do
        {
            value = NextUInt();
        }
        while ((ulong)value >= limit);

        return (uint)((ulong)value % upperExclusive);
    }

    private uint NextUInt()
    {
        uint t = (_x ^ (_x << 11));
        _x = _y;
        _y = _z;
        _z = _w;
        return _w = (_w ^ (_w >> 19)) ^ (t ^ (t >> 8));
    }
}
