namespace System;

public static class StringExtentions
{
    public static bool IsEmpty(this string value)
        => string.IsNullOrWhiteSpace(value);

    public static bool IsNotEmpty(this string value)
        => !IsEmpty(value);

    public static string Intern(this string value)
        => string.Intern(value);

    public static bool IsIntern(this string value)
        => string.IsInterned(value) == value;
}
