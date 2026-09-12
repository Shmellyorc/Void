// ============================================================================
//  EnumFlagExtensions.cs
// ============================================================================
//  Extension methods for bitwise operations on flag-based enumerations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides bitwise helpers for enum values used as flags.
/// </summary>
/// <remarks>
/// These methods convert enum values through <see cref="long"/> before applying
/// bitwise operations. Enum values backed by <see cref="ulong"/> must therefore
/// fit within <see cref="long.MaxValue"/>.
/// </remarks>
public static class EnumExtensions
{
    /// <summary>
    /// Returns the value with the specified flag bits set.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The source enum value.</param>
    /// <param name="flag">The flag bits to set.</param>
    /// <returns>The value with the specified bits set.</returns>
    public static T SetFlag<T>(this T value, T flag) where T : Enum
        => (T)Enum.ToObject(typeof(T), Convert.ToInt64(value) | Convert.ToInt64(flag));

    /// <summary>
    /// Returns the value with the specified flag bits cleared.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The source enum value.</param>
    /// <param name="flag">The flag bits to clear.</param>
    /// <returns>The value with the specified bits cleared.</returns>
    public static T ClearFlag<T>(this T value, T flag) where T : Enum
        => (T)Enum.ToObject(typeof(T), Convert.ToInt64(value) & ~Convert.ToInt64(flag));

    /// <summary>
    /// Returns the value with the specified flag bits toggled.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The source enum value.</param>
    /// <param name="flag">The flag bits to toggle.</param>
    /// <returns>The value with the specified bits toggled.</returns>
    public static T ToggleFlag<T>(this T value, T flag) where T : Enum
        => (T)Enum.ToObject(typeof(T), Convert.ToInt64(value) ^ Convert.ToInt64(flag));

    /// <summary>
    /// Determines whether all specified flag bits are set.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to inspect.</param>
    /// <param name="flags">The flag bits that must all be present.</param>
    /// <returns><see langword="true"/> when all bits in <paramref name="flags"/> are set; otherwise, <see langword="false"/>.</returns>
    public static bool HasAllFlags<T>(this T value, T flags) where T : Enum
        => (Convert.ToInt64(value) & Convert.ToInt64(flags)) == Convert.ToInt64(flags);

    /// <summary>
    /// Determines whether any specified flag bit is set.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to inspect.</param>
    /// <param name="flags">The flag bits to test.</param>
    /// <returns><see langword="true"/> when at least one requested bit is set; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnyFlag<T>(this T value, T flags) where T : Enum
        => (Convert.ToInt64(value) & Convert.ToInt64(flags)) != 0;

    /// <summary>
    /// Determines whether the value is exactly equal to the specified flag value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to inspect.</param>
    /// <param name="flag">The value to compare against.</param>
    /// <returns><see langword="true"/> when the underlying values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool HasOnlyFlag<T>(this T value, T flag) where T : Enum
        => Convert.ToInt64(value) == Convert.ToInt64(flag);

    /// <summary>
    /// Determines whether the enum value is zero.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to inspect.</param>
    /// <returns><see langword="true"/> when the underlying value is zero; otherwise, <see langword="false"/>.</returns>
    public static bool HasNoFlags<T>(this T value) where T : Enum
        => Convert.ToInt64(value) == 0;
}
