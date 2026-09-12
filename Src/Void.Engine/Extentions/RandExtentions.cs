// ============================================================================
//  RandExtensions.cs
// ============================================================================
//  Convenience helpers built on FastRandom.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides common selection, direction, color, and probability helpers for <see cref="FastRandom"/>.
/// </summary>
public static class RandExtensions
{
    private static readonly Dictionary<Type, Array> _enumCache = [];

    /// <summary>
    /// Selects a random element from an array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="items">The array to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="items"/> is null or empty.</exception>
    public static T Choice<T>(this FastRandom rng, T[] items)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        if (items == null || items.Length == 0)
            throw new ArgumentException("Must provide at least one item.", nameof(items));

        return items[rng.Next(items.Length)];
    }

    /// <summary>
    /// Selects a random element from a read-only list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="list">The list to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="list"/> is null or empty.</exception>
    public static T Choice<T>(this FastRandom rng, IReadOnlyList<T> list)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        if (list == null || list.Count == 0)
            throw new ArgumentException("Must provide at least one item.", nameof(list));

        return list[rng.Next(list.Count)];
    }

    /// <summary>
    /// Selects a random element from a sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="source">The sequence to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <remarks>
    /// Lists are indexed directly. Other sequence types are sampled in one pass using reservoir sampling.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> or <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> contains no elements.</exception>
    public static T Choice<T>(this FastRandom rng, IEnumerable<T> source)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (source is IList<T> list)
        {
            if (list.Count == 0)
                throw new ArgumentException("Sequence contains no elements.", nameof(source));
            return list[rng.Next(list.Count)];
        }

        T selected = default!;
        int count = 0;
        foreach (var item in source)
        {
            count++;
            if (rng.Next(count) == 0)
                selected = item;
        }

        if (count == 0)
            throw new ArgumentException("Sequence contains no elements.", nameof(source));
        return selected;
    }

    /// <summary>
    /// Returns one of the four cardinal direction vectors with equal probability.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>Up, right, down, or left.</returns>
    public static Vect2 RandomDirection(this FastRandom rng)
    {
        return rng.Next(4) switch
        {
            0 => Vect2.Up,
            1 => Vect2.Right,
            2 => Vect2.Down,
            _ => Vect2.Left
        };
    }

    /// <summary>
    /// Returns one of eight cardinal or diagonal direction vectors with equal probability.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A cardinal direction or the sum of two adjacent cardinal directions.</returns>
    /// <remarks>Diagonal results are not normalized.</remarks>
    public static Vect2 RandomDirection8Way(this FastRandom rng)
    {
        return rng.Next(8) switch
        {
            0 => Vect2.Up,
            1 => Vect2.Up + Vect2.Right,
            2 => Vect2.Right,
            3 => Vect2.Right + Vect2.Down,
            4 => Vect2.Down,
            5 => Vect2.Down + Vect2.Left,
            6 => Vect2.Left,
            _ => Vect2.Left + Vect2.Up
        };
    }

    /// <summary>
    /// Creates a random RGB color using the full 0 through 255 component range.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random color.</returns>
    public static Color RandomColor(this FastRandom rng)
        => new(rng.Next(256), rng.Next(256), rng.Next(256));

    /// <summary>
    /// Creates a random light RGB color with components from 128 through 255.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random pastel-range color.</returns>
    public static Color RandomPastelColor(this FastRandom rng)
        => new(rng.Next(128, 256), rng.Next(128, 256), rng.Next(128, 256));

    /// <summary>
    /// Creates a random dark RGB color with components from 0 through 127.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random dark-range color.</returns>
    public static Color RandomDarkColor(this FastRandom rng)
        => new(rng.Next(128), rng.Next(128), rng.Next(128));

    /// <summary>
    /// Selects one declared value from an enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A randomly selected declared enum value.</returns>
    /// <remarks>Declared values are cached by enum type after the first call.</remarks>
    public static TEnum RandomEnum<TEnum>(this FastRandom rng) where TEnum : struct, Enum
    {
        var type = typeof(TEnum);

        if (!_enumCache.TryGetValue(type, out var values))
        {
            values = Enum.GetValues(type);
            _enumCache[type] = values;
        }

        return (TEnum)values.GetValue(rng.Next(values.Length));
    }

    /// <summary>
    /// Returns either 1 or -1 with equal probability.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>1 or -1.</returns>
    public static int NextSign(this FastRandom rng) => rng.NextBoolean() ? 1 : -1;

    /// <summary>
    /// Shuffles a list in place using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="list">The list to shuffle.</param>
    public static void Shuffle<T>(this FastRandom rng, IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Rolls a number of equally sided dice and returns their sum.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="diceCount">The number of dice to roll.</param>
    /// <param name="sides">The highest face value on each die.</param>
    /// <returns>The sum of the generated rolls.</returns>
    public static int RollDice(this FastRandom rng, int diceCount, int sides)
    {
        int sum = 0;

        for (int i = 0; i < diceCount; i++)
            sum += rng.RangeInt(1, sides);

        return sum;
    }

    /// <summary>
    /// Generates a point uniformly by area within the unit circle.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random point whose distance from the origin is at most 1.</returns>
    public static Vect2 RandomPointInCircle(this FastRandom rng)
    {
        float angle = rng.RangeFloat(0f, MathF.PI * 2f);
        float radius = MathF.Sqrt(rng.NextFloat());

        return new Vect2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }

    /// <summary>
    /// Generates a point uniformly by area within a circle of the specified radius.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="maxRadius">The radius applied to the generated unit-circle point.</param>
    /// <returns>A random point scaled by <paramref name="maxRadius"/>.</returns>
    public static Vect2 RandomPointInCircle(this FastRandom rng, float maxRadius)
        => rng.RandomPointInCircle() * maxRadius;

    /// <summary>
    /// Generates an angle in radians from 0 up to 2 pi.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random angle in radians.</returns>
    public static float RandomAngle(this FastRandom rng)
        => rng.RangeFloat(0f, MathF.PI * 2f);

    /// <summary>
    /// Generates an angle in degrees from 0 up to 360.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random angle in degrees.</returns>
    public static float RandomAngleDegrees(this FastRandom rng)
        => rng.RangeFloat(0f, 360f);

    /// <summary>
    /// Tests a probability against the generator's next floating-point sample.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="probability">The comparison threshold. The value is not clamped.</param>
    /// <returns><see langword="true"/> when the next random sample is less than <paramref name="probability"/>.</returns>
    public static bool Chance(this FastRandom rng, float probability)
        => rng.NextFloat() < probability;
}
