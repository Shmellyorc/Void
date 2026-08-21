// ============================================================================
//  StringExtensions.cs
// ============================================================================
//  Extension methods for string operations including validation, parsing,
//  manipulation, and enumeration caching.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for string operations including validation,
/// parsing, manipulation, and enumeration caching.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="StringExtensions"/> class provides a comprehensive set of
/// extension methods for <see cref="string"/> values, making common string
/// operations more intuitive and readable.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Empty and whitespace checks</description></item>
///   <item><description>Numeric validation (integer, decimal, numeric)</description></item>
///   <item><description>Enum to string conversion with caching</description></item>
///   <item><description>String trimming and truncation</description></item>
///   <item><description>Character and substring counting</description></item>
///   <item><description>Pattern matching (starts/ends/contains with any/all)</description></item>
///   <item><description>Whitespace removal and string splitting</description></item>
///   <item><description>String reversal and take/last operations</description></item>
///   <item><description>Collection joining</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// string text = "Hello World";
/// 
/// // Empty checks
/// bool empty = text.IsEmpty(); // false
/// bool notEmpty = text.IsNotEmpty(); // true
/// 
/// // Numeric checks
/// bool isInt = "123".IsInteger(); // true
/// bool isDecimal = "12.34".IsDecimal(); // true
/// bool isNumeric = "12.34".IsNumeric(); // true
/// 
/// // Enum to string
/// string enumStr = MyEnum.Value.ToEnumString(); // "Namespace.MyEnum.Value"
/// 
/// // Trimming
/// string trimmed = "Hello World".TrimToLength(5); // "Hello"
/// 
/// // Counting
/// int count = "Hello World".CountChar('l'); // 3
/// int subCount = "Hello Hello".CountSubstring("Hello"); // 2
/// 
/// // Pattern matching
/// bool starts = "Hello World".StartsWithAny("He", "Wo"); // true
/// bool ends = "Hello World".EndsWithAny("rld", "ld"); // true
/// bool contains = "Hello World".ContainsAny("ell", "xyz"); // true
/// bool containsAll = "Hello World".ContainsAll("Hello", "World"); // true
/// 
/// // Whitespace removal
/// string noSpace = "Hello World".RemoveWhitespace(); // "HelloWorld"
/// 
/// // Split and trim
/// string[] parts = "one, two, three".SplitAndTrim(','); // ["one", "two", "three"]
/// 
/// // Reverse
/// string reversed = "Hello".Reverse(); // "olleH"
/// 
/// // Take and last
/// string first = "Hello World".Take(5); // "Hello"
/// string last = "Hello World".Last(5); // "World"
/// 
/// // Remove from ends
/// string removedEnd = "Hello World".RemoveEnd(6); // "Hello"
/// string removedStart = "Hello World".RemoveStart(6); // "World"
/// 
/// // Join collection
/// string joined = new[] { "a", "b", "c" }.JoinToString(", "); // "a, b, c"
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. The enum cache uses locks for synchronization.
/// </para>
/// </remarks>
public static class StringExtensions
{
    private static readonly Dictionary<Enum, string> _enumStringCache = new();
    private static readonly Lock _enumCacheLock = new();

    /// <summary>
    /// Determines whether the string is null, empty, or consists only of whitespace.
    /// </summary>
    public static bool IsEmpty(this string v) => string.IsNullOrWhiteSpace(v);

    /// <summary>
    /// Determines whether the string is not null, not empty, and not whitespace.
    /// </summary>
    public static bool IsNotEmpty(this string v) => !IsEmpty(v);

    /// <summary>
    /// Determines whether the string represents a valid integer.
    /// </summary>
    public static bool IsInteger(this string v) => long.TryParse(v, out _);

    /// <summary>
    /// Determines whether the string represents a valid decimal number.
    /// </summary>
    public static bool IsDecimal(this string v) => decimal.TryParse(v, out _);

    /// <summary>
    /// Determines whether the string represents a valid numeric value.
    /// </summary>
    public static bool IsNumeric(this string v) => double.TryParse(v, out _);

    /// <summary>
    /// Converts an enum to its fully qualified string representation with caching.
    /// </summary>
    /// <param name="v">The enum value to convert.</param>
    /// <returns>The fully qualified string representation of the enum.</returns>
    public static string ToEnumString(this Enum v)
    {
        lock (_enumCacheLock)
        {
            if (!_enumStringCache.TryGetValue(v, out var result))
            {
                result = $"{v.GetType().FullName}.{v}";
                _enumStringCache[v] = result;
            }
            return result;
        }
    }

    /// <summary>
    /// Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="v">The string to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated string, or the original if shorter.</returns>
    public static string TrimToLength(this string v, int maxLength)
    {
        if (string.IsNullOrEmpty(v) || v.Length <= maxLength)
            return v;

        return v.Substring(0, maxLength);
    }

    /// <summary>
    /// Counts the number of occurrences of a character in the string.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="target">The character to count.</param>
    /// <returns>The number of occurrences.</returns>
    public static int CountChar(this string input, char target)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        int count = 0;

        foreach (char c in input)
        {
            if (c == target)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Counts the number of occurrences of a substring in the string.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="target">The substring to count.</param>
    /// <returns>The number of occurrences.</returns>
    public static int CountSubstring(this string input, string target)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target))
            return 0;

        int count = 0, index = 0;

        while ((index = input.IndexOf(target, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += target.Length;
        }
        return count;
    }

    /// <summary>
    /// Determines whether the string starts with any of the specified values.
    /// </summary>
    /// <param name="v">The string to check.</param>
    /// <param name="values">The values to check for.</param>
    /// <returns><see langword="true"/> if the string starts with any of the values; otherwise, <see langword="false"/>.</returns>
    public static bool StartsWithAny(this string v, params string[] values)
    {
        if (string.IsNullOrEmpty(v) || values == null)
            return false;

        foreach (var value in values)
        {
            if (v.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the string ends with any of the specified values.
    /// </summary>
    /// <param name="v">The string to check.</param>
    /// <param name="values">The values to check for.</param>
    /// <returns><see langword="true"/> if the string ends with any of the values; otherwise, <see langword="false"/>.</returns>
    public static bool EndsWithAny(this string v, params string[] values)
    {
        if (string.IsNullOrEmpty(v) || values == null)
            return false;

        foreach (var value in values)
        {
            if (v.EndsWith(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the string contains any of the specified values.
    /// </summary>
    /// <param name="v">The string to check.</param>
    /// <param name="values">The values to check for.</param>
    /// <returns><see langword="true"/> if the string contains any of the values; otherwise, <see langword="false"/>.</returns>
    public static bool ContainsAny(this string v, params string[] values)
    {
        if (string.IsNullOrEmpty(v) || values == null)
            return false;

        foreach (var value in values)
        {
            if (v.Contains(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether the string contains all of the specified values.
    /// </summary>
    /// <param name="v">The string to check.</param>
    /// <param name="values">The values to check for.</param>
    /// <returns><see langword="true"/> if the string contains all of the values; otherwise, <see langword="false"/>.</returns>
    public static bool ContainsAll(this string v, params string[] values)
    {
        if (string.IsNullOrEmpty(v) || values == null)
            return false;

        foreach (var value in values)
        {
            if (!v.Contains(value, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Removes all whitespace characters from the string.
    /// </summary>
    /// <param name="v">The string to process.</param>
    /// <returns>The string with all whitespace removed.</returns>
    public static string RemoveWhitespace(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        return new string(v.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// Splits the string by a separator, trims each part, and removes empty entries.
    /// </summary>
    /// <param name="v">The string to split.</param>
    /// <param name="separator">The separator character.</param>
    /// <returns>An array of trimmed, non-empty parts.</returns>
    public static string[] SplitAndTrim(this string v, char separator = ',')
    {
        if (string.IsNullOrEmpty(v))
            return [];

        return v.Split(separator)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    /// <summary>
    /// Reverses the string.
    /// </summary>
    /// <param name="v">The string to reverse.</param>
    /// <returns>The reversed string.</returns>
    public static string Reverse(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        char[] chars = v.ToCharArray();
        Array.Reverse(chars);

        return new string(chars);
    }

    /// <summary>
    /// Takes the first n characters from the string.
    /// </summary>
    /// <param name="v">The string to take from.</param>
    /// <param name="count">The number of characters to take.</param>
    /// <returns>The first n characters, or the full string if shorter.</returns>
    public static string Take(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(0, count);
    }

    /// <summary>
    /// Takes the last n characters from the string.
    /// </summary>
    /// <param name="v">The string to take from.</param>
    /// <param name="count">The number of characters to take.</param>
    /// <returns>The last n characters, or the full string if shorter.</returns>
    public static string Last(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(v.Length - count);
    }

    /// <summary>
    /// Removes the last n characters from the string.
    /// </summary>
    /// <param name="v">The string to remove from.</param>
    /// <param name="count">The number of characters to remove.</param>
    /// <returns>The string with the last n characters removed.</returns>
    public static string RemoveEnd(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(0, v.Length - count);
    }

    /// <summary>
    /// Removes the first n characters from the string.
    /// </summary>
    /// <param name="v">The string to remove from.</param>
    /// <param name="count">The number of characters to remove.</param>
    /// <returns>The string with the first n characters removed.</returns>
    public static string RemoveStart(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(count);
    }

    /// <summary>
    /// Joins the elements of a collection into a string using the specified separator.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="values">The collection to join.</param>
    /// <param name="separator">The separator string.</param>
    /// <returns>The joined string.</returns>
    public static string JoinToString<T>(this IEnumerable<T> values, string separator = ", ")
    {
        if (values == null)
            return string.Empty;

        return string.Join(separator, values);
    }
}