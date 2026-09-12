// ============================================================================
//  IEnumerableExtensions.cs
// ============================================================================
//  Convenience helpers for common IEnumerable<T> operations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides convenience operations for <see cref="IEnumerable{T}"/> sequences.
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    /// Determines whether the sequence is <see langword="null"/> or contains no elements.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to inspect.</param>
    /// <returns><see langword="true"/> when the sequence is null or empty; otherwise, <see langword="false"/>.</returns>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
        => source == null || !source.Any();

    /// <summary>
    /// Determines whether the sequence is non-null and contains at least one element.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to inspect.</param>
    /// <returns><see langword="true"/> when the sequence contains an element; otherwise, <see langword="false"/>.</returns>
    public static bool IsNotEmpty<T>(this IEnumerable<T> source)
        => !source.IsEmpty();

    /// <summary>
    /// Invokes an action for each element in the sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to enumerate.</param>
    /// <param name="action">The action invoked for each element.</param>
    /// <remarks>
    /// If either argument is <see langword="null"/>, this method returns without doing anything.
    /// </remarks>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null || action == null)
            return;

        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Selects a random element using <see cref="FastRandom.Shared"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to select from.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the sequence contains no elements.</exception>
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
    /// Selects a random element using the supplied random generator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to select from.</param>
    /// <param name="random">The random generator to use.</param>
    /// <returns>A randomly selected element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="random"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the sequence contains no elements.</exception>
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
    /// Returns the element at an index, or the default value when the source or index is invalid.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to access.</param>
    /// <param name="index">The zero-based index to retrieve.</param>
    /// <returns>The element at the requested index, or <see langword="default"/> when it cannot be retrieved.</returns>
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
    /// Finds the zero-based index of the first element equal to <paramref name="item"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="item">The item to locate.</param>
    /// <returns>The first matching index, or -1 when no match is found or the sequence is null.</returns>
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
    /// Splits a sequence into elements that match a predicate and elements that do not.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="predicate">The predicate used to classify each element.</param>
    /// <returns>Two lists containing matching and nonmatching elements in their original order.</returns>
    /// <remarks>
    /// If <paramref name="source"/> or <paramref name="predicate"/> is null, both returned lists are empty.
    /// </remarks>
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
    /// Returns a shuffled copy of the sequence using <see cref="FastRandom.Shared"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to shuffle.</param>
    /// <returns>A shuffled list, or an empty list when <paramref name="source"/> is null.</returns>
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
    /// Filters null values from a sequence of reference types.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>A sequence containing only non-null elements.</returns>
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
        => source?.Where(x => x != null) ?? Enumerable.Empty<T>();

    /// <summary>
    /// Returns the first element encountered for each unique key.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <param name="keySelector">The function used to obtain each element's key.</param>
    /// <returns>A lazily evaluated sequence containing one element per unique key.</returns>
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
    /// Returns up to <paramref name="count"/> randomly ordered elements from the sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to sample.</param>
    /// <param name="count">The maximum number of elements to return.</param>
    /// <returns>A random sample without replacement, or an empty list when the source is null.</returns>
    public static List<T> RandomSample<T>(this IEnumerable<T> source, int count)
    {
        if (source == null)
            return [];

        return source.Shuffle().Take(count).ToList();
    }

    /// <summary>
    /// Determines whether every element in the sequence is unique according to the default equality comparer.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to inspect.</param>
    /// <returns><see langword="true"/> when all elements are unique or the source is null; otherwise, <see langword="false"/>.</returns>
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
    /// Filters null values from a sequence of nullable value types and unwraps the remaining values.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>A lazily evaluated sequence containing the non-null values.</returns>
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
