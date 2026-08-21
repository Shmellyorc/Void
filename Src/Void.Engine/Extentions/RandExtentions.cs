// ============================================================================
//  RandExtensions.cs
// ============================================================================
//  Extension methods for FastRandom providing convenient random operations
//  including element selection, direction generation, color creation, and
//  common random distributions.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

/// <summary>
/// Provides extension methods for <see cref="FastRandom"/> with convenient
/// random operations including element selection, direction generation,
/// color creation, and common random distributions.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RandExtensions"/> class provides a comprehensive set of
/// extension methods for the <see cref="FastRandom"/> class, making common
/// random operations more intuitive and expressive.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Random element selection from arrays, lists, and sequences</description></item>
///   <item><description>4-way and 8-way random direction generation</description></item>
///   <item><description>Random color generation (full, pastel, dark)</description></item>
///   <item><description>Random enum selection with caching</description></item>
///   <item><description>Sign and dice rolling</description></item>
///   <item><description>Random points in a circle</description></item>
///   <item><description>Probability checks (chance)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var rng = FastRandom.Shared;
/// 
/// // Random element selection
/// var items = new[] { "a", "b", "c" };
/// string selected = rng.Choice(items);
/// 
/// // Random direction
/// Vect2 dir4 = rng.RandomDirection(); // Up, Right, Down, Left
/// Vect2 dir8 = rng.RandomDirection8Way(); // 8 directions incl. diagonals
/// 
/// // Random colors
/// Color color = rng.RandomColor();
/// Color pastel = rng.RandomPastelColor();
/// Color dark = rng.RandomDarkColor();
/// 
/// // Random enum
/// MyEnum value = rng.RandomEnum&lt;MyEnum&gt;();
/// 
/// // Sign
/// int sign = rng.NextSign(); // 1 or -1
/// 
/// // Shuffle a list
/// rng.Shuffle(myList);
/// 
/// // Dice roll
/// int sum = rng.RollDice(3, 6); // 3d6
/// 
/// // Random point in circle
/// Vect2 point = rng.RandomPointInCircle(5f);
/// 
/// // Random angle
/// float angle = rng.RandomAngle();
/// 
/// // Probability check
/// if (rng.Chance(0.25f)) // 25% chance
///     // Do something
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe as long as the <see cref="FastRandom"/>
/// instance is used in a thread-safe manner.
/// </para>
/// </remarks>
public static class RandExtensions
{
    private static readonly Dictionary<Type, Array> _enumCache = [];

    /// <summary>
    /// Selects a random element from an array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="items">The array to select from.</param>
    /// <returns>A random element from the array.</returns>
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
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="list">The list to select from.</param>
    /// <returns>A random element from the list.</returns>
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
    /// Selects a random element from a sequence using reservoir sampling.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="source">The sequence to select from.</param>
    /// <returns>A random element from the sequence.</returns>
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
    /// Generates a random 4-way direction (Up, Right, Down, Left).
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random 4-way direction vector.</returns>
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
    /// Generates a random 8-way direction including diagonals.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random 8-way direction vector.</returns>
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
    /// Generates a random RGB color with full value range.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random color with components between 0 and 255.</returns>
    public static Color RandomColor(this FastRandom rng)
        => new(rng.Next(256), rng.Next(256), rng.Next(256));

    /// <summary>
    /// Generates a random pastel color with light values.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random pastel color with components between 128 and 255.</returns>
    public static Color RandomPastelColor(this FastRandom rng)
        => new(rng.Next(128, 256), rng.Next(128, 256), rng.Next(128, 256));

    /// <summary>
    /// Generates a random dark color with low values.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random dark color with components between 0 and 127.</returns>
    public static Color RandomDarkColor(this FastRandom rng)
        => new(rng.Next(128), rng.Next(128), rng.Next(128));

    /// <summary>
    /// Generates a random value from the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random enum value.</returns>
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
    /// Generates a random sign (1 or -1).
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>1 or -1 with equal probability.</returns>
    public static int NextSign(this FastRandom rng) => rng.NextBoolean() ? 1 : -1;

    /// <summary>
    /// Shuffles a list using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
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
    /// Rolls dice and returns the sum.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="diceCount">The number of dice to roll.</param>
    /// <param name="sides">The number of sides on each die.</param>
    /// <returns>The sum of all dice rolls.</returns>
    public static int RollDice(this FastRandom rng, int diceCount, int sides)
    {
        int sum = 0;

        for (int i = 0; i < diceCount; i++)
            sum += rng.RangeInt(1, sides);

        return sum;
    }

    /// <summary>
    /// Generates a random point uniformly within a unit circle.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random point within a unit circle.</returns>
    public static Vect2 RandomPointInCircle(this FastRandom rng)
    {
        float angle = rng.RangeFloat(0f, MathF.PI * 2f);
        float radius = MathF.Sqrt(rng.NextFloat());

        return new Vect2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }

    /// <summary>
    /// Generates a random point uniformly within a circle of the specified radius.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="maxRadius">The maximum radius of the circle.</param>
    /// <returns>A random point within the circle.</returns>
    public static Vect2 RandomPointInCircle(this FastRandom rng, float maxRadius)
        => rng.RandomPointInCircle() * maxRadius;

    /// <summary>
    /// Generates a random angle in radians.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random angle between 0 and 2π.</returns>
    public static float RandomAngle(this FastRandom rng)
        => rng.RangeFloat(0f, MathF.PI * 2f);

    /// <summary>
    /// Generates a random angle in degrees.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <returns>A random angle between 0 and 360.</returns>
    public static float RandomAngleDegrees(this FastRandom rng)
        => rng.RangeFloat(0f, 360f);

    /// <summary>
    /// Returns true with the specified probability.
    /// </summary>
    /// <param name="rng">The random generator to use.</param>
    /// <param name="probability">The probability between 0 and 1.</param>
    /// <returns><see langword="true"/> with the specified probability; otherwise, <see langword="false"/>.</returns>
    public static bool Chance(this FastRandom rng, float probability)
        => rng.NextFloat() < probability;
}