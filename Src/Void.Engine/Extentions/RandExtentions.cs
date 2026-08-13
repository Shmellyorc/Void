namespace System;

public static class RandExtensions
{
    private static readonly Dictionary<Type, Array> _enumCache = [];

    public static T Choice<T>(this FastRandom rng, T[] items)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        if (items == null || items.Length == 0)
            throw new ArgumentException("Must provide at least one item.", nameof(items));

        return items[rng.Next(items.Length)];
    }

    public static T Choice<T>(this FastRandom rng, IReadOnlyList<T> list)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));
        if (list == null || list.Count == 0)
            throw new ArgumentException("Must provide at least one item.", nameof(list));

        return list[rng.Next(list.Count)];
    }

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

        T selected = default;
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

    public static Color RandomColor(this FastRandom rng)
        => new(rng.Next(256), rng.Next(256), rng.Next(256));

    public static Color RandomPastelColor(this FastRandom rng)
        => new(rng.Next(128, 256), rng.Next(128, 256), rng.Next(128, 256));

    public static Color RandomDarkColor(this FastRandom rng)
        => new(rng.Next(128), rng.Next(128), rng.Next(128));

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

    public static int NextSign(this FastRandom rng) => rng.NextBoolean() ? 1 : -1;

    public static void Shuffle<T>(this FastRandom rng, IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static int RollDice(this FastRandom rng, int diceCount, int sides)
    {
        int sum = 0;

        for (int i = 0; i < diceCount; i++)
            sum += rng.RangeInt(1, sides);

        return sum;
    }

    public static Vect2 RandomPointInCircle(this FastRandom rng)
    {
        float angle = rng.RangeFloat(0f, MathF.PI * 2f);
        float radius = MathF.Sqrt(rng.NextFloat());

        return new Vect2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
    }

    public static Vect2 RandomPointInCircle(this FastRandom rng, float maxRadius)
        => rng.RandomPointInCircle() * maxRadius;

    public static float RandomAngle(this FastRandom rng)
        => rng.RangeFloat(0f, MathF.PI * 2f);

    public static float RandomAngleDegrees(this FastRandom rng)
        => rng.RangeFloat(0f, 360f);

    public static bool Chance(this FastRandom rng, float probability)
        => rng.NextFloat() < probability;
}