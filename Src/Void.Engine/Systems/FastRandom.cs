// ============================================================================
//  FastRandom.cs
// ============================================================================
//  High-performance thread-safe random number generator using a 128-bit
//  Xorshift algorithm. Provides per-thread instances and a shared instance
//  for convenient access.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Provides a high-performance, thread-safe random number generator using the
/// Xorshift128 algorithm for fast, high-quality pseudo-random numbers.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is significantly faster than <see cref="System.Random"/>
/// and provides per-thread instances via <see cref="Shared"/> to avoid
/// contention in multi-threaded scenarios.
/// </para>
/// <para>
/// The generator supports all standard random operations including integers,
/// floating-point values, booleans, and ranged values. Each instance maintains
/// its own state for deterministic sequences when seeded.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Use the shared per-thread instance
/// int randomValue = FastRandom.Shared.Next(0, 100);
/// float randomFloat = FastRandom.Shared.NextFloat(0f, 1f);
/// 
/// // Or create a seeded instance for deterministic results
/// var random = new FastRandom(12345);
/// int deterministicValue = random.Next();
/// </code>
/// </para>
/// </remarks>
public sealed class FastRandom
{
    private const int Y = 0x2B5B9F51;
    private const int Z = 0x4F59A821;
    private const int W = 0x6F5B9D5B;

    private uint _x, _y, _z, _w;

    private static readonly ThreadLocal<FastRandom> _threadLocal = new(() => new FastRandom());

    /// <summary>
    /// Gets a shared <see cref="FastRandom"/> instance for the current thread.
    /// </summary>
    /// <value>
    /// A thread-local <see cref="FastRandom"/> instance that is safe to use
    /// without synchronization in multi-threaded code.
    /// </value>
    /// <remarks>
    /// Each thread gets its own independent random number generator instance
    /// with a unique seed, making this property ideal for concurrent scenarios
    /// where multiple threads need random numbers.
    /// </remarks>
    public static FastRandom Shared => _threadLocal.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastRandom"/> class with
    /// the specified seed value.
    /// </summary>
    /// <param name="seed">The seed value that determines the sequence of random numbers.</param>
    /// <remarks>
    /// A deterministic sequence of random numbers is generated when the same
    /// seed is used, which is useful for reproducible results in testing or
    /// procedural generation.
    /// </remarks>
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
    /// Initializes a new instance of the <see cref="FastRandom"/> class with
    /// a seed derived from the current system time and thread ID.
    /// </summary>
    public FastRandom() : this(GenerateSeed()) { }

    /// <summary>
    /// Generates a random boolean value.
    /// </summary>
    /// <returns><see langword="true"/> or <see langword="false"/> with approximately equal probability.</returns>
    public bool NextBoolean()
    {
        return (NextUInt() & 1) == 0;
    }

    /// <summary>
    /// Generates a random non-negative integer.
    /// </summary>
    /// <returns>A random integer between 0 and <see cref="int.MaxValue"/> - 1.</returns>
    public int Next()
    {
        var rtn = NextUInt() & 0x7FFFFFFF;
        if (rtn == 0x7FFFFFFF)
            return Next();
        return (int)rtn;
    }

    /// <summary>
    /// Generates a random integer between 0 (inclusive) and the specified maximum (exclusive).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than zero.</param>
    /// <returns>A random integer between 0 and <paramref name="maxValue"/> - 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than or equal to zero.</exception>
    public int Next(int maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than zero");

        return (int)((NextUInt() & 0x7FFFFFFF) % maxValue);
    }

    /// <summary>
    /// Generates a random integer between the specified minimum (inclusive) and maximum (exclusive).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than or equal to <paramref name="minValue"/>.</param>
    /// <returns>A random integer between <paramref name="minValue"/> and <paramref name="maxValue"/> - 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater or equal to minValue");
        var range = (long)maxValue - minValue;
        if (range <= 0)
            return minValue;
        return (int)((NextUInt() & 0x7FFFFFFF) % range) + minValue;
    }

    /// <summary>
    /// Generates a random double-precision floating-point number between 0 (inclusive) and 1 (exclusive).
    /// </summary>
    /// <returns>A random double between 0.0 and 1.0.</returns>
    public double NextDouble()
    {
        return (NextUInt() & 0x7FFFFFFF) / (double)0x7FFFFFFF;
    }

    /// <summary>
    /// Generates a random double-precision floating-point number between 0 (inclusive) and the specified maximum (exclusive).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than zero.</param>
    /// <returns>A random double between 0.0 and <paramref name="maxValue"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than or equal to zero.</exception>
    public double NextDouble(double maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than zero");
        return NextDouble() * maxValue;
    }

    /// <summary>
    /// Generates a random double-precision floating-point number between the specified minimum (inclusive) and maximum (exclusive).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than or equal to <paramref name="minValue"/>.</param>
    /// <returns>A random double between <paramref name="minValue"/> and <paramref name="maxValue"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
    public double NextDouble(double minValue, double maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater or equal to minValue");
        var range = maxValue - minValue;
        if (range <= 0)
            return maxValue;
        return NextDouble() * range + minValue;
    }

    /// <summary>
    /// Generates a random single-precision floating-point number between 0 (inclusive) and 1 (exclusive).
    /// </summary>
    /// <returns>A random float between 0.0f and 1.0f.</returns>
    public float NextFloat()
    {
        return (float)((NextUInt() & 0x7FFFFFFF) / (double)0x7FFFFFFF);
    }

    /// <summary>
    /// Generates a random single-precision floating-point number between 0 (inclusive) and the specified maximum (exclusive).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than zero.</param>
    /// <returns>A random float between 0.0f and <paramref name="maxValue"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than or equal to zero.</exception>
    public float NextFloat(float maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than zero");

        return NextFloat() * maxValue;
    }

    /// <summary>
    /// Generates a random single-precision floating-point number between the specified minimum (inclusive) and maximum (exclusive).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound. Must be greater than or equal to <paramref name="minValue"/>.</param>
    /// <returns>A random float between <paramref name="minValue"/> and <paramref name="maxValue"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
    public float NextFloat(float minValue, float maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater or equal to minValue");
        var range = maxValue - minValue;
        if (range <= 0)
            return minValue;
        return NextFloat() * range + minValue;
    }

    /// <summary>
    /// Generates a random integer between the specified minimum (inclusive) and maximum (inclusive).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound. Must be greater than or equal to <paramref name="min"/>.</param>
    /// <returns>A random integer between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    /// <remarks>
    /// This method differs from <see cref="Next(int, int)"/> in that the upper bound
    /// is inclusive, making it useful for array index ranges where both bounds are valid.
    /// </remarks>
    public int RangeInt(int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;

        return (int)((NextUInt() & 0x7FFFFFFF) % ((long)max - min + 1)) + min;
    }

    /// <summary>
    /// Generates a random single-precision floating-point number between the specified minimum (inclusive) and maximum (inclusive).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound. Must be greater than or equal to <paramref name="min"/>.</param>
    /// <returns>A random float between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public float RangeFloat(float min, float max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;
        return NextFloat() * (max - min) + min;
    }

    /// <summary>
    /// Generates a random double-precision floating-point number between the specified minimum (inclusive) and maximum (inclusive).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound. Must be greater than or equal to <paramref name="min"/>.</param>
    /// <returns>A random double between <paramref name="min"/> and <paramref name="max"/> inclusive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public double RangeDouble(double min, double max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;
        return NextDouble() * (max - min) + min;
    }

    private static int GenerateSeed()
    {
        return (int)(Environment.TickCount ^ Environment.CurrentManagedThreadId ^ (uint)DateTime.Now.Ticks);
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