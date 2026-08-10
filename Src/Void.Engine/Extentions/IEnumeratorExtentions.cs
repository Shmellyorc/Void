namespace System;

public static class IEnumeratorExtentions
{
    public static bool IsEmpty<T>(this IEnumerable<T> values)
        => values == null || !values.Any();
}
