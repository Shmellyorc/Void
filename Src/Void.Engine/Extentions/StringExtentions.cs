namespace System;

public static class StringExtensions
{
    private static readonly Dictionary<Enum, string> _enumStringCache = new();
    private static readonly Lock _enumCacheLock = new();

    public static bool IsEmpty(this string v) => string.IsNullOrWhiteSpace(v);
    public static bool IsNotEmpty(this string v) => !IsEmpty(v);
    public static bool IsInteger(this string v) => long.TryParse(v, out _);
    public static bool IsDecimal(this string v) => decimal.TryParse(v, out _);
    public static bool IsNumeric(this string v) => double.TryParse(v, out _);

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

    public static string TrimToLength(this string v, int maxLength)
    {
        if (string.IsNullOrEmpty(v) || v.Length <= maxLength)
            return v;

        return v.Substring(0, maxLength);
    }

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

    public static string RemoveWhitespace(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        return new string(v.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static string[] SplitAndTrim(this string v, char separator = ',')
    {
        if (string.IsNullOrEmpty(v))
            return [];

        return v.Split(separator)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    public static string Reverse(this string v)
    {
        if (string.IsNullOrEmpty(v))
            return v;

        char[] chars = v.ToCharArray();
        Array.Reverse(chars);

        return new string(chars);
    }

    public static string Take(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(0, count);
    }

    public static string Last(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return string.Empty;

        return v.Length <= count ? v : v.Substring(v.Length - count);
    }

    public static string RemoveEnd(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(0, v.Length - count);
    }

    public static string RemoveStart(this string v, int count)
    {
        if (string.IsNullOrEmpty(v) || count <= 0)
            return v;

        return v.Length <= count ? string.Empty : v.Substring(count);
    }

    public static string JoinToString<T>(this IEnumerable<T> values, string separator = ", ")
    {
        if (values == null)
            return string.Empty;

        return string.Join(separator, values);
    }
}
