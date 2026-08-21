// ============================================================================
//  EnumExtensions.cs
// ============================================================================
//  Extension methods for bitwise operations on flag-based enumerations.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides extension methods for bitwise operations on flag-based enumerations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EnumExtensions"/> class provides a set of extension methods
/// for working with flag-based enumerations, allowing for intuitive bitwise
/// operations without the need for explicit casting.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Set, clear, and toggle flags</description></item>
///   <item><description>Check for all, any, or specific flags</description></item>
///   <item><description>Check if an enum has only a specific flag</description></item>
///   <item><description>Check if an enum has no flags set</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// [Flags]
/// public enum MyFlags
/// {
///     None = 0,
///     Option1 = 1 &lt;&lt; 0,
///     Option2 = 1 &lt;&lt; 1,
///     Option3 = 1 &lt;&lt; 2
/// }
/// 
/// var flags = MyFlags.Option1;
/// 
/// // Set a flag
/// flags = flags.SetFlag(MyFlags.Option2);
/// 
/// // Clear a flag
/// flags = flags.ClearFlag(MyFlags.Option1);
/// 
/// // Toggle a flag
/// flags = flags.ToggleFlag(MyFlags.Option3);
/// 
/// // Check flags
/// bool hasAll = flags.HasAllFlags(MyFlags.Option2 | MyFlags.Option3);
/// bool hasAny = flags.HasAnyFlag(MyFlags.Option1 | MyFlags.Option2);
/// bool hasOnly = flags.HasOnlyFlag(MyFlags.Option3);
/// bool hasNone = flags.HasNoFlags();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are thread-safe as they operate on value types.
/// </para>
/// </remarks>
public static class EnumExtensions
{
    /// <summary>
    /// Adds the specified flag to the enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to modify.</param>
    /// <param name="flag">The flag to add.</param>
    /// <returns>The enum value with the flag set.</returns>
    public static T SetFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value | (int)(object)flag);

    /// <summary>
    /// Removes the specified flag from the enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to modify.</param>
    /// <param name="flag">The flag to remove.</param>
    /// <returns>The enum value with the flag cleared.</returns>
    public static T ClearFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value & ~(int)(object)flag);

    /// <summary>
    /// Toggles the specified flag on the enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to modify.</param>
    /// <param name="flag">The flag to toggle.</param>
    /// <returns>The enum value with the flag toggled.</returns>
    public static T ToggleFlag<T>(this T value, T flag) where T : Enum
        => (T)(object)((int)(object)value ^ (int)(object)flag);

    /// <summary>
    /// Determines whether all specified flags are set on the enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <param name="flags">The flags to check for.</param>
    /// <returns><see langword="true"/> if all specified flags are set; otherwise, <see langword="false"/>.</returns>
    public static bool HasAllFlags<T>(this T value, T flags) where T : Enum
        => ((int)(object)value & (int)(object)flags) == (int)(object)flags;

    /// <summary>
    /// Determines whether any of the specified flags are set on the enum value.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <param name="flags">The flags to check for.</param>
    /// <returns><see langword="true"/> if any specified flag is set; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnyFlag<T>(this T value, T flags) where T : Enum
        => ((int)(object)value & (int)(object)flags) != 0;

    /// <summary>
    /// Determines whether the enum value has exactly the specified flag and no others.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <param name="flag">The flag to check for.</param>
    /// <returns><see langword="true"/> if the enum value has exactly the specified flag; otherwise, <see langword="false"/>.</returns>
    public static bool HasOnlyFlag<T>(this T value, T flag) where T : Enum
        => (int)(object)value == (int)(object)flag;

    /// <summary>
    /// Determines whether the enum value has no flags set (value is zero).
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The enum value to check.</param>
    /// <returns><see langword="true"/> if the enum value is zero; otherwise, <see langword="false"/>.</returns>
    public static bool HasNoFlags<T>(this T value) where T : Enum
        => (int)(object)value == 0;
}