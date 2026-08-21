// ============================================================================
//  LDtkSetting.cs
// ============================================================================
//  Strongly-typed setting system for LDtk field instances with support for
//  primitive types, arrays, and complex LDtk data types.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Strongly-typed wrapper for an LDtk setting value.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkSetting"/> class provides a unified container for all
/// LDtk field instance values with strong typing and convenient access methods.
/// Each setting type has a corresponding sealed class that inherits from this
/// base class.
/// </para>
/// <para>
/// <b>Supported Setting Types:</b>
/// <list type="bullet">
///   <item><description><see cref="LDtkBoolSettings"/> - Boolean values</description></item>
///   <item><description><see cref="LDtkIntSettings"/> - Integer values</description></item>
///   <item><description><see cref="LDtkFloatSettings"/> - Float values</description></item>
///   <item><description><see cref="LDtkStringSettings"/> - String values</description></item>
///   <item><description><see cref="LDtkColorSettings"/> - Color values</description></item>
///   <item><description><see cref="LDtkPointSettings"/> - Vect2 point values</description></item>
///   <item><description><see cref="LDtkTileSettings"/> - Tile references</description></item>
///   <item><description><see cref="LDtkEntityRefSettings"/> - Entity references</description></item>
///   <item><description><see cref="LDtkEnumSettings"/> - Enum values (stored as strings)</description></item>
///   <item><description><see cref="LDtkFilePathSettings"/> - File path values</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Array Variants:</b>
/// All setting types have corresponding array variants:
/// <list type="bullet">
///   <item><description><see cref="LDtkBoolArraySettings"/> - List of bool</description></item>
///   <item><description><see cref="LDtkIntArraySettings"/> - List of int</description></item>
///   <item><description><see cref="LDtkFloatArraySettings"/> - List of float</description></item>
///   <item><description><see cref="LDtkStringArraySettings"/> - List of string</description></item>
///   <item><description><see cref="LDtkColorArraySettings"/> - List of Color</description></item>
///   <item><description><see cref="LDtkPointArraySettings"/> - List of Vect2</description></item>
///   <item><description><see cref="LDtkTileArraySettings"/> - List of LDtkTile</description></item>
///   <item><description><see cref="LDtkEntityRefArraySettings"/> - List of LDtkEntityRef</description></item>
///   <item><description><see cref="LDtkEnumArraySettings"/> - List of enum values (stored as strings)</description></item>
///   <item><description><see cref="LDtkFilePathArraySettings"/> - List of file paths</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var settings = level.Settings;
/// 
/// // Get a bool setting
/// if (LDtkSetting.Contains(settings, "IsActive"))
/// {
///     bool isActive = LDtkSetting.GetBoolSetting(settings, "IsActive");
/// }
/// 
/// // Get with Try pattern
/// if (LDtkSetting.TryGetIntSetting(settings, "Health", out int health))
/// {
///     // Use health value
/// }
/// 
/// // Get an enum setting
/// var enumValue = LDtkSetting.GetEnumSetting&lt;MyEnum&gt;(settings, "Type");
/// 
/// // Get an array setting
/// var points = LDtkSetting.GetPointArraySetting(settings, "Waypoints");
/// 
/// // Strongly-typed access from the setting object itself
/// var setting = new LDtkIntSettings(42);
/// int value = setting.ValueAs&lt;int&gt;();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe when used in a read-only manner.
/// </para>
/// </remarks>
public class LDtkSetting(object value)
{
    /// <summary>
    /// Gets the raw value of the setting.
    /// </summary>
    public object Value { get; } = value;

    /// <summary>
    /// Gets the value cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <returns>The value cast to type T.</returns>
    public T ValueAs<T>() => (T)Value;

    /// <summary>
    /// Determines whether a setting with the specified name exists.
    /// </summary>
    /// <param name="settings">The settings dictionary to check.</param>
    /// <param name="name">The name of the setting.</param>
    /// <returns><see langword="true"/> if the setting exists; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
        => settings.ContainsKey(HashHelper.Cache32(name));

    /// <summary>
    /// Gets a boolean setting by name.
    /// </summary>
    /// <param name="settings">The settings dictionary.</param>
    /// <param name="name">The name of the setting.</param>
    /// <returns>The boolean value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the setting is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown when the setting is not a boolean.</exception>
    public static bool GetBoolSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not bool)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(bool)}'.");

        return result.ValueAs<bool>();
    }

    /// <summary>
    /// Attempts to get a boolean setting by name.
    /// </summary>
    /// <param name="settings">The settings dictionary.</param>
    /// <param name="name">The name of the setting.</param>
    /// <param name="setting">When this method returns, contains the setting value if successful.</param>
    /// <returns><see langword="true"/> if the setting was found and is a boolean; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetBoolSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out bool setting)
    {
        try
        {
            setting = GetBoolSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets an integer setting by name.
    /// </summary>
    public static int GetIntSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not int)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(int)}'.");

        return result.ValueAs<int>();
    }

    /// <summary>
    /// Attempts to get an integer setting by name.
    /// </summary>
    public static bool TryGetIntSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out int setting)
    {
        try
        {
            setting = GetIntSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a float setting by name.
    /// </summary>
    public static float GetFloatSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not float)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(float)}'.");

        return result.ValueAs<float>();
    }

    /// <summary>
    /// Attempts to get a float setting by name.
    /// </summary>
    public static bool TryGetFloatSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out float setting)
    {
        try
        {
            setting = GetFloatSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a point (Vect2) setting by name.
    /// </summary>
    public static Vect2 GetPointSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not Vect2)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(Vect2)}'.");

        return result.ValueAs<Vect2>();
    }

    /// <summary>
    /// Attempts to get a point (Vect2) setting by name.
    /// </summary>
    public static bool TryGetPointSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out Vect2 setting)
    {
        try
        {
            setting = GetPointSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a color setting by name.
    /// </summary>
    public static Color GetColorSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not Color)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(Color)}'.");

        return result.ValueAs<Color>();
    }

    /// <summary>
    /// Attempts to get a color setting by name.
    /// </summary>
    public static bool TryGetColorSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out Color setting)
    {
        try
        {
            setting = GetColorSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a string setting by name.
    /// </summary>
    public static string GetStringSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not string)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(string)}'.");

        return result.ValueAs<string>();
    }

    /// <summary>
    /// Attempts to get a string setting by name.
    /// </summary>
    public static bool TryGetStringSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out string setting)
    {
        try
        {
            setting = GetStringSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a file path setting by name.
    /// </summary>
    public static string GetFilePathSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not string)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(string)}'.");

        return result.ValueAs<string>();
    }

    /// <summary>
    /// Attempts to get a file path setting by name.
    /// </summary>
    public static bool TryGetFilePathSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out string setting)
    {
        try
        {
            setting = GetFilePathSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a tile setting by name.
    /// </summary>
    public static LDtkTile GetTileSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not LDtkTile)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(LDtkTile)}'.");

        return result.ValueAs<LDtkTile>();
    }

    /// <summary>
    /// Attempts to get a tile setting by name.
    /// </summary>
    public static bool TryGetTileSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out LDtkTile setting)
    {
        try
        {
            setting = GetTileSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets an entity reference setting by name.
    /// </summary>
    public static LDtkEntityRef GetEntityRefSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not LDtkEntityRef)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(LDtkEntityRef)}'.");

        return result.ValueAs<LDtkEntityRef>();
    }

    /// <summary>
    /// Attempts to get an entity reference setting by name.
    /// </summary>
    public static bool TryGetEntityRefSetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out LDtkEntityRef setting)
    {
        try
        {
            setting = GetEntityRefSetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets an enum setting by name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    public static TEnum GetEnumSetting<TEnum>(IReadOnlyDictionary<uint, LDtkSetting> settings, string name) where TEnum : Enum
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not string)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(TEnum)}'.");

        return (TEnum)Enum.Parse(typeof(TEnum), result.ValueAs<string>(), true);
    }

    /// <summary>
    /// Attempts to get an enum setting by name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    public static bool TryGetEnumSetting<TEnum>(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out TEnum setting)
        where TEnum : Enum
    {
        try
        {
            setting = GetEnumSetting<TEnum>(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a boolean array setting by name.
    /// </summary>
    public static IReadOnlyList<bool> GetBoolArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<bool>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<bool>)}'.");

        return result.ValueAs<List<bool>>();
    }

    /// <summary>
    /// Attempts to get a boolean array setting by name.
    /// </summary>
    public static bool TryGetBoolArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<bool> setting)
    {
        try
        {
            setting = GetBoolArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets an integer array setting by name.
    /// </summary>
    public static IReadOnlyList<int> GetIntArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<int>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<int>)}'.");

        return result.ValueAs<List<int>>();
    }

    /// <summary>
    /// Attempts to get an integer array setting by name.
    /// </summary>
    public static bool TryGetIntArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<int> setting)
    {
        try
        {
            setting = GetIntArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a float array setting by name.
    /// </summary>
    public static IReadOnlyList<float> GetFloatArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<float>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<float>)}'.");

        return result.ValueAs<List<float>>();
    }

    /// <summary>
    /// Attempts to get a float array setting by name.
    /// </summary>
    public static bool TryGetFloatArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<float> setting)
    {
        try
        {
            setting = GetFloatArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a point (Vect2) array setting by name.
    /// </summary>
    public static IReadOnlyList<Vect2> GetPointArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<Vect2>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<Vect2>)}'.");

        return result.ValueAs<List<Vect2>>();
    }

    /// <summary>
    /// Attempts to get a point (Vect2) array setting by name.
    /// </summary>
    public static bool TryGetPointArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<Vect2> setting)
    {
        try
        {
            setting = GetPointArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a color array setting by name.
    /// </summary>
    public static IReadOnlyList<Color> GetColorArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<Color>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<Color>)}'.");

        return result.ValueAs<List<Color>>();
    }

    /// <summary>
    /// Attempts to get a color array setting by name.
    /// </summary>
    public static bool TryGetColorArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<Color> setting)
    {
        try
        {
            setting = GetColorArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a string array setting by name.
    /// </summary>
    public static IReadOnlyList<string> GetStringArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<string>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<string>)}'.");

        return result.ValueAs<List<string>>();
    }

    /// <summary>
    /// Attempts to get a string array setting by name.
    /// </summary>
    public static bool TryGetStringArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<string> setting)
    {
        try
        {
            setting = GetStringArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a file path array setting by name.
    /// </summary>
    public static IReadOnlyList<string> GetFilePathArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<string>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<string>)}'.");

        return result.ValueAs<List<string>>();
    }

    /// <summary>
    /// Attempts to get a file path array setting by name.
    /// </summary>
    public static bool TryGetFilePathArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<string> setting)
    {
        try
        {
            setting = GetFilePathArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets a tile array setting by name.
    /// </summary>
    public static IReadOnlyList<LDtkTile> GetTileArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<LDtkTile>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<LDtkTile>)}'.");

        return result.ValueAs<List<LDtkTile>>();
    }

    /// <summary>
    /// Attempts to get a tile array setting by name.
    /// </summary>
    public static bool TryGetTileArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<LDtkTile> setting)
    {
        try
        {
            setting = GetTileArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets an entity reference array setting by name.
    /// </summary>
    public static IReadOnlyList<LDtkEntityRef> GetEntityRefArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<LDtkEntityRef>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<LDtkEntityRef>)}'.");

        return result.ValueAs<List<LDtkEntityRef>>();
    }

    /// <summary>
    /// Attempts to get an entity reference array setting by name.
    /// </summary>
    public static bool TryGetEntityRefArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<LDtkEntityRef> setting)
    {
        try
        {
            setting = GetEntityRefArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets an enum array setting by name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    public static IReadOnlyList<TEnum> GetEnumArraySetting<TEnum>(IReadOnlyDictionary<uint, LDtkSetting> settings, string name) where TEnum : Enum
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name));
        if (!settings.TryGetValue(HashHelper.Cache32(name), out var result))
            throw new KeyNotFoundException($"Unable to find setting with the name '{name}'.");
        if (result.Value is not List<string>)
            throw new InvalidCastException($"Setting '{name}' is '{result.Value.GetType()}', expected '{typeof(List<TEnum>)}'.");

        var items = result.ValueAs<List<string>>();
        var enumResult = new List<TEnum>(items.Count);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!Enum.TryParse(typeof(TEnum), item, true, out var @enum))
                continue;

            enumResult.Add((TEnum)@enum);
        }

        return enumResult;
    }

    /// <summary>
    /// Attempts to get an enum array setting by name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    public static bool TryGetEnumArraySetting<TEnum>(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<TEnum> setting)
        where TEnum : Enum
    {
        try
        {
            setting = GetEnumArraySetting<TEnum>(settings, name);
            return true;
        }
        catch
        {
            setting = default!;
            return false;
        }
    }
}

/// <summary>
/// Boolean setting value.
/// </summary>
public sealed class LDtkBoolSettings(bool value) : LDtkSetting(value);

/// <summary>
/// Boolean array setting value.
/// </summary>
public sealed class LDtkBoolArraySettings(List<bool> value) : LDtkSetting(value);

/// <summary>
/// Color setting value.
/// </summary>
public sealed class LDtkColorSettings(Color value) : LDtkSetting(value);

/// <summary>
/// Color array setting value.
/// </summary>
public sealed class LDtkColorArraySettings(List<Color> value) : LDtkSetting(value);

/// <summary>
/// Entity reference setting value.
/// </summary>
public sealed class LDtkEntityRefSettings(LDtkEntityRef value) : LDtkSetting(value);

/// <summary>
/// Entity reference array setting value.
/// </summary>
public sealed class LDtkEntityRefArraySettings(List<LDtkEntityRef> value) : LDtkSetting(value);

/// <summary>
/// Enum setting value.
/// </summary>
public sealed class LDtkEnumSettings(string value) : LDtkSetting(value);

/// <summary>
/// Enum array setting value.
/// </summary>
public sealed class LDtkEnumArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// File path setting value.
/// </summary>
public sealed class LDtkFilePathSettings(string value) : LDtkSetting(value);

/// <summary>
/// File path array setting value.
/// </summary>
public sealed class LDtkFilePathArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// Float setting value.
/// </summary>
public sealed class LDtkFloatSettings(float value) : LDtkSetting(value);

/// <summary>
/// Float array setting value.
/// </summary>
public sealed class LDtkFloatArraySettings(List<float> value) : LDtkSetting(value);

/// <summary>
/// Integer setting value.
/// </summary>
public sealed class LDtkIntSettings(int value) : LDtkSetting(value);

/// <summary>
/// Integer array setting value.
/// </summary>
public sealed class LDtkIntArraySettings(List<int> value) : LDtkSetting(value);

/// <summary>
/// Point (Vect2) setting value.
/// </summary>
public sealed class LDtkPointSettings(Vect2 value) : LDtkSetting(value);

/// <summary>
/// Point (Vect2) array setting value.
/// </summary>
public sealed class LDtkPointArraySettings(List<Vect2> value) : LDtkSetting(value);

/// <summary>
/// String setting value.
/// </summary>
public sealed class LDtkStringSettings(string value) : LDtkSetting(value);

/// <summary>
/// String array setting value.
/// </summary>
public sealed class LDtkStringArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// Tile setting value.
/// </summary>
public sealed class LDtkTileSettings(LDtkTile value) : LDtkSetting(value);

/// <summary>
/// Tile array setting value.
/// </summary>
public sealed class LDtkTileArraySettings(List<LDtkTile> value) : LDtkSetting(value);