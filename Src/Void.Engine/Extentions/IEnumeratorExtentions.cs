// ============================================================================
//  IEnumerableExtensions.cs
// ============================================================================
//  Extension methods for IEnumerable<T> to simplify common sequence operations
//  including validation, iteration, random selection, and partitioning.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for <see cref="IEnumerable{T}"/> to simplify common sequence operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IEnumerableExtensions"/> class provides a comprehensive set of
/// extension methods for working with sequences, including validation, iteration,
/// random selection, partitioning, shuffling, and more.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Empty and not-empty checks</description></item>
///   <item><description>ForEach iteration with action</description></item>
///   <item><description>Random element selection with optional random instance</description></item>
///   <item><description>Safe element access with fallback</description></item>
///   <item><description>Index finding and partitioning</description></item>
///   <item><description>Shuffling and random sampling</description></item>
///   <item><description>Distinct by key selector</description></item>
///   <item><description>Null filtering for reference and nullable value types</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var items = new List&lt;int&gt; { 1, 2, 3, 4, 5 };
/// 
/// // Check if empty
/// if (items.IsNotEmpty())
/// {
///     // Iterate with action
///     items.ForEach(x => Console.WriteLine(x));
///     
///     // Get random element
///     int random = items.Random();
///     
///     // Get random with specific random instance
///     int random2 = items.Random(myRandom);
///     
///     // Safe element access
///     int value = items.SafeElementAt(2); // 3
///     int notFound = items.SafeElementAt(10); // default
///     
///     // Find index
///     int index = items.IndexOf(3); // 2
///     
///     // Partition by predicate
///     var (evens, odds) = items.Partition(x => x % 2 == 0);
///     
///     // Shuffle
///     var shuffled = items.Shuffle();
///     
///     // Random sample
///     var sample = items.RandomSample(3);
///     
///     // Distinct by key
///     var distinct = items.DistinctBy(x => x % 2);
///     
///     // Check all distinct
///     bool allDistinct = items.AllDistinct();
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe for reading operations. Modifying
/// operations on mutable collections are not thread-safe.
/// </para>
/// </remarks>
public static class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether the sequence is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence is null or empty; otherwise, <see langword="false"/>.</returns>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
        => source == null || !source.Any();

    /// <summary>
    /// Determines whether the sequence is not null and not empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if the sequence is not null and not empty; otherwise, <see langword="false"/>.</returns>
    public static bool IsNotEmpty<T>(this IEnumerable<T> source)
        => !source.IsEmpty();

    /// <summary>
    /// Performs the specified action on each element of the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to iterate over.</param>
    /// <param name="action">The action to perform on each element.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null || action == null)
            return;

        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Gets a random element from the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to select from.</param>
    /// <returns>A random element from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the sequence is empty.</exception>
    public static T Random<T>(this IEnumerable<T> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var list = source as IList<T> ?? source.ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("Collection is empty.");

        return list[FastRandom.Shared.RangeInt(0, list.Count)];
    }

    /// <summary>
    /// Gets a random element from the sequence using the specified random generator.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to select from.</param>
    /// <param name="random">The random generator to use.</param>
    /// <returns>A random element from the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="random"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the sequence is empty.</exception>
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

    /// <summary>
    /// Safely gets the element at the specified index, returning default if out of range.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to access.</param>
    /// <param name="index">The index of the element to retrieve.</param>
    /// <returns>The element at the specified index, or default if the index is out of range.</returns>
    public static T SafeElementAt<T>(this IEnumerable<T> source, int index)
    {
        if (source == null || index < 0)
            return default!;

        if (source is IList<T> list)
            return index < list.Count ? list[index] : default!;

        int current = 0;
        foreach (var item in source)
        {
            if (current == index)
                return item;
            current++;
        }

        return default!;
    }

    /// <summary>
    /// Finds the index of the first occurrence of the specified item.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="item">The item to find.</param>
    /// <returns>The index of the item, or -1 if not found.</returns>
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

    /// <summary>
    /// Partitions the sequence into two lists based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="predicate">The predicate to determine partition.</param>
    /// <returns>A tuple containing the matches and non-matches lists.</returns>
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

    /// <summary>
    /// Shuffles the sequence using the Fisher-Yates algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to shuffle.</param>
    /// <returns>A shuffled list of the sequence elements.</returns>
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

    /// <summary>
    /// Filters out null elements from a sequence of reference types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>A sequence with all non-null elements.</returns>
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
        => source?.Where(x => x != null) ?? Enumerable.Empty<T>();

    /// <summary>
    /// Returns distinct elements from a sequence based on a key selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="source">The sequence to process.</param>
    /// <param name="keySelector">The function to extract the key from each element.</param>
    /// <returns>A sequence of distinct elements based on the key selector.</returns>
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

    /// <summary>
    /// Gets a random sample of the specified size from the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to sample from.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>A list containing the random sample.</returns>
    public static List<T> RandomSample<T>(this IEnumerable<T> source, int count)
    {
        if (source == null)
            return [];

        return source.Shuffle().Take(count).ToList();
    }

    /// <summary>
    /// Determines whether all elements in the sequence are distinct.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to check.</param>
    /// <returns><see langword="true"/> if all elements are distinct; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Filters out null values from a sequence of nullable value types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>A sequence of non-null values.</returns>
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