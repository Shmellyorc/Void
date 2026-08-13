namespace System;

public static class EnumExtensions
{
    /// <summary>
    /// Adds a flag (or flags) to the enum value.
    /// </summary>
    public static T SetFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value | (int)(object)flag);

    /// <summary>
    /// Removes a flag (or flags) from the enum value.
    /// </summary>
    public static T ClearFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value & ~(int)(object)flag);

    /// <summary>
    /// Toggles a flag on/off. If it's set, it's removed. If not set, it's added.
    /// </summary>
    public static T ToggleFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value ^ (int)(object)flag);

    /// <summary>
    /// Checks if all given flags are set.
    /// </summary>
    public static bool HasAllFlags<T>(this T value, T flags) where T : Enum
        => ((int)(object)value & (int)(object)flags) == (int)(object)flags;

    /// <summary>
    /// Checks if any of the given flags are set.
    /// </summary>
    public static bool HasAnyFlag<T>(this T value, T flags) where T : Enum
        => ((int)(object)value & (int)(object)flags) != 0;

    /// <summary>
    /// Checks if the enum has exactly the given flag and nothing else.
    /// </summary>
    public static bool HasOnlyFlag<T>(this T value, T flag) where T : Enum
        => (int)(object)value == (int)(object)flag;

    /// <summary>
    /// Returns true if the enum has no flags set (value is zero).
    /// </summary>
    public static bool HasNoFlags<T>(this T value) where T : Enum
        => (int)(object)value == 0;
}
