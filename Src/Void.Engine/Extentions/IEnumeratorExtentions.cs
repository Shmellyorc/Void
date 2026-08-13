namespace System;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/> to simplify common sequence operations.
/// </summary>
public static class IEnumerableExtensions
{
    public static bool IsEmpty<T>(this IEnumerable<T> source)
        => source == null || !source.Any();

    public static bool IsNotEmpty<T>(this IEnumerable<T> source)
        => !source.IsEmpty();

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null || action == null)
            return;

        foreach (var item in source)
            action(item);
    }

    public static T Random<T>(this IEnumerable<T> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var list = source as IList<T> ?? source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Collection is empty.");

        return list[FastRandom.Shared.RangeInt(0, list.Count)];
    }

    public static T Random<T>(this IEnumerable<T> source, FastRandom random)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        var list = source as IList<T> ?? source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Collection is empty.");

        return list[random.RangeInt(0, list.Count)];
    }

    public static T SafeElementAt<T>(this IEnumerable<T> source, int index)
    {
        if (source == null || index < 0)
            return default;

        if (source is IList<T> list)
            return index < list.Count ? list[index] : default;

        int current = 0;
        foreach (var item in source)
        {
            if (current == index)
                return item;
            current++;
        }

        return default;
    }

    public static int IndexOf<T>(this IEnumerable<T> source, T item)
    {
        if (source == null)
            return -1;

        int index = 0;
        foreach (var element in source)
        {
            if (EqualityComparer<T>.Default.Equals(element, item))
                return index;
            index++;
        }

        return -1;
    }

    public static (List<T> matches, List<T> nonMatches) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var matches = new List<T>();
        var nonMatches = new List<T>();

        if (source == null || predicate == null)
            return (matches, nonMatches);

        foreach (var item in source)
        {
            if (predicate(item))
                matches.Add(item);
            else
                nonMatches.Add(item);
        }

        return (matches, nonMatches);
    }

    public static List<T> Shuffle<T>(this IEnumerable<T> source)
    {
        if (source == null)
            return [];

        var list = source.ToList();
        var random = FastRandom.Shared;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.RangeInt(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
        => source?.Where(x => x != null) ?? Enumerable.Empty<T>();

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        if (source == null)
            yield break;

        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
                yield return item;
        }
    }

    public static List<T> RandomSample<T>(this IEnumerable<T> source, int count)
    {
        if (source == null)
            return [];

        return source.Shuffle().Take(count).ToList();
    }

    public static bool AllDistinct<T>(this IEnumerable<T> source)
    {
        if (source == null)
            return true;

        var seen = new HashSet<T>();
        foreach (var item in source)
        {
            if (!seen.Add(item))
                return false;
        }

        return true;
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        if (source == null)
            yield break;

        foreach (var item in source)
        {
            if (item.HasValue)
                yield return item.Value;
        }
    }
}
