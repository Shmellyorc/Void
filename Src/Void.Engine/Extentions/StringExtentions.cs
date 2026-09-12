// ============================================================================
//  StringExtensions.cs
// ============================================================================
//  Common string validation, parsing, matching, and manipulation helpers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides common validation, parsing, matching, formatting, and manipulation helpers for strings.
/// </summary>
public static class StringExtensions
{
    private static readonly Dictionary<Enum, string> _enumStringCache = new();
    private static readonly Lock _enumCacheLock = new();

    /// <summary>
    /// Determines whether a string is null, empty, or consists only of whitespace.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <returns><see langword="true"/> when the string is null, empty, or whitespace; otherwise, <see langword="false"/>.</returns>
    public static bool IsEmpty(this string v) => string.IsNullOrWhiteSpace(v);

    /// <summary>
    /// Determines whether a string contains at least one non-whitespace character.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <returns><see langword="true"/> when the string is not null, empty, or whitespace; otherwise, <see langword="false"/>.</returns>
    public static bool IsNotEmpty(this string v) => !IsEmpty(v);

    /// <summary>
    /// Determines whether the string can be parsed as a <see cref="long"/> using the current culture.
    /// </summary>
    /// <param name="v">The string to test.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool IsInteger(this string v) => long.TryParse(v, out _);

    /// <summary>
    /// Determines whether the string can be parsed as a <see cref="decimal"/> using the current culture.
    /// </summary>
    /// <param name="v">The string to test.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool IsDecimal(this string v) => decimal.TryParse(v, out _);

    /// <summary>
    /// Determines whether the string can be parsed as a <see cref="double"/> using the current culture.
    /// </summary>
    /// <param name="v">The string to test.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool IsNumeric(this string v) => double.TryParse(v, out _);

    /// <summary>
    /// Returns a cached topic-style string containing the enum type's full name and value.
    /// </summary>
    /// <param name="v">The enum value to convert.</param>
    /// <returns>A string in the form <c>Namespace.EnumType.Value</c>.</returns>
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
    /// Truncates a string to at most the requested number of characters.
    /// </summary>
    /// <param name="v">The string to truncate.</param>
    /// <param name="maxLength">The maximum returned length.</param>
    /// <returns>The original string when already short enough; otherwise, its leading <paramref name="maxLength"/> characters.</returns>
    public static string TrimToLength(this string v, int maxLength)
    {
        if (string.IsNullOrEmpty(v) || v.Length <= maxLength)
            return v;

        return v.Substring(0, maxLength);
    }

    /// <summary>
    /// Counts occurrences of a character.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="target">The character to count.</param>
    /// <returns>The number of matching characters, or 0 when the input is null or empty.</returns>
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
    /// Counts non-overlapping ordinal occurrences of a substring.
    /// </summary>
    /// <param name="input">The string to search.</param>
    /// <param name="target">The substring to count.</param>
    /// <returns>The number of non-overlapping matches, or 0 when either string is null or empty.</returns>
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
    /// Determines whether the string starts with any supplied value using an ordinal case-insensitive comparison.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <param name="values">The candidate prefixes.</param>
    /// <returns><see langword="true"/> when any prefix matches; otherwise, <see langword="false"/>.</returns>
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
    /// Determines whether the string ends with any supplied value using an ordinal case-insensitive comparison.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <param name="values">The candidate suffixes.</param>
    /// <returns><see langword="true"/> when any suffix matches; otherwise, <see langword="false"/>.</returns>
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
    /// Determines whether the string contains any supplied value using an ordinal case-insensitive comparison.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <param name="values">The candidate substrings.</param>
    /// <returns><see langword="true"/> when any substring is present; otherwise, <see langword="false"/>.</returns>
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
    /// Determines whether the string contains every supplied value using ordinal case-insensitive comparisons.
    /// </summary>
    /// <param name="v">The string to inspect.</param>
    /// <param name="values">The required substrings.</param>
    /// <returns><see langword="true"/> when every substring is present; otherwise, <see langword="false"/>.</returns>
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
    /// Removes every Unicode whitespace character from a string.
    /// </summary>
    /// <param name="v">The string to process.</param>
    /// <returns>The string with whitespace removed, or the original null or empty value.</returns>
    public static string RemoveWhitespace(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        return new string(v.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// Splits a string by a character, trims each part, and removes empty results.
    /// </summary>
    /// <param name="v">The string to split.</param>
    /// <param name="separator">The separator character.</param>
    /// <returns>The trimmed, non-empty parts.</returns>
    public static string[] SplitAndTrim(this string v, char separator = ',')
    {
        if (string.IsNullOrEmpty(v))
            return [];

        return v.Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    /// <summary>
    /// Splits a string by another string, trims each part, and removes empty results.
    /// </summary>
    /// <param name="v">The string to split.</param>
    /// <param name="separator">The separator string.</param>
    /// <returns>The trimmed, non-empty parts, or an empty array when the source or separator is null or empty.</returns>
    public static string[] SplitAndTrim(this string v, string separator)
    {
        if (string.IsNullOrEmpty(v) || string.IsNullOrEmpty(separator))
            return [];

        if (separator.Length == 1)
            return SplitAndTrim(v, separator[0]);

        return v.Split([separator], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    /// <summary>
    /// Reverses the UTF-16 character sequence in a string.
    /// </summary>
    /// <param name="v">The string to reverse.</param>
    /// <returns>The reversed string, or the original null or empty value.</returns>
    public static string Reverse(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        char[] chars = v.ToCharArray();
        Array.Reverse(chars);

        return new string(chars);
    }

    /// <summary>
    /// Returns up to the first <paramref name="count"/> characters.
    /// </summary>
    /// <param name="v">The source string.</param>
    /// <param name="count">The maximum number of characters to return.</param>
    /// <returns>The requested leading characters, or an empty string when the source is null or empty or the count is nonpositive.</returns>
    public static string Take(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(0, count);
    }

    /// <summary>
    /// Returns up to the last <paramref name="count"/> characters.
    /// </summary>
    /// <param name="v">The source string.</param>
    /// <param name="count">The maximum number of characters to return.</param>
    /// <returns>The requested trailing characters, or an empty string when the source is null or empty or the count is nonpositive.</returns>
    public static string Last(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(v.Length - count);
    }

    /// <summary>
    /// Removes up to the requested number of characters from the end of a string.
    /// </summary>
    /// <param name="v">The source string.</param>
    /// <param name="count">The number of trailing characters to remove.</param>
    /// <returns>The remaining string.</returns>
    public static string RemoveEnd(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(0, v.Length - count);
    }

    /// <summary>
    /// Removes up to the requested number of characters from the start of a string.
    /// </summary>
    /// <param name="v">The source string.</param>
    /// <param name="count">The number of leading characters to remove.</param>
    /// <returns>The remaining string.</returns>
    public static string RemoveStart(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(count);
    }

    /// <summary>
    /// Joins a sequence using the supplied separator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The values to join.</param>
    /// <param name="separator">The text inserted between values.</param>
    /// <returns>The joined string, or an empty string when <paramref name="values"/> is null.</returns>
    public static string JoinToString<T>(this IEnumerable<T> values, string separator = ", ")
    {
        if (values == null)
            return string.Empty;

        return string.Join(separator, values);
    }

    /// <summary>
    /// Parses an integer using the current culture, returning a fallback when parsing fails.
    /// </summary>
    /// <param name="v">The string to parse.</param>
    /// <param name="defaultValue">The value returned when parsing fails.</param>
    /// <returns>The parsed integer or <paramref name="defaultValue"/>.</returns>
    public static int ToInt(this string v, int defaultValue = 0)
        => int.TryParse(v, out int result) ? result : defaultValue;

    /// <summary>
    /// Parses a floating-point value using the current culture, returning a fallback when parsing fails.
    /// </summary>
    /// <param name="v">The string to parse.</param>
    /// <param name="defaultValue">The value returned when parsing fails.</param>
    /// <returns>The parsed value or <paramref name="defaultValue"/>.</returns>
    public static float ToFloat(this string v, float defaultValue = 0f)
        => float.TryParse(v, out float result) ? result : defaultValue;

    /// <summary>
    /// Parses a double-precision value using the current culture, returning a fallback when parsing fails.
    /// </summary>
    /// <param name="v">The string to parse.</param>
    /// <param name="defaultValue">The value returned when parsing fails.</param>
    /// <returns>The parsed value or <paramref name="defaultValue"/>.</returns>
    public static double ToDouble(this string v, double defaultValue = 0.0)
        => double.TryParse(v, out double result) ? result : defaultValue;

    /// <summary>
    /// Parses a decimal value using the current culture, returning a fallback when parsing fails.
    /// </summary>
    /// <param name="v">The string to parse.</param>
    /// <param name="defaultValue">The value returned when parsing fails.</param>
    /// <returns>The parsed value or <paramref name="defaultValue"/>.</returns>
    public static decimal ToDecimal(this string v, decimal defaultValue = 0m)
        => decimal.TryParse(v, out decimal result) ? result : defaultValue;
}
