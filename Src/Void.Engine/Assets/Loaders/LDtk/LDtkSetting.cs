// ============================================================================
//  LDtkSetting.cs
// ============================================================================
//  Typed access helpers for LDtk field-instance values.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Wraps a parsed LDtk field value and provides typed lookup helpers for setting dictionaries.
/// </summary>
/// <remarks>
/// <para>
/// Level and entity field dictionaries are keyed by VOID's hash of the original
/// LDtk field name. The static helpers let game code continue using the readable
/// field name while validating the expected value type.
/// </para>
/// <code>
/// if (LDtkSetting.TryGetIntSetting(level.Settings, "Difficulty", out int difficulty))
/// {
///     // Use difficulty.
/// }
///
/// IReadOnlyList&lt;Vect2&gt; points =
///     LDtkSetting.GetPointArraySetting(level.Settings, "Waypoints");
/// </code>
/// </remarks>
public class LDtkSetting(object value)
{
    /// <summary>
    /// Gets the parsed field value.
    /// </summary>
    public object Value { get; } = value;

    /// <summary>
    /// Casts the stored value to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <returns>The stored value cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidCastException">Thrown when the stored value cannot be cast to <typeparamref name="T"/>.</exception>
    public T ValueAs<T>() => (T)Value;

    /// <summary>
    /// Determines whether a setting dictionary contains the specified LDtk field name.
    /// </summary>
    /// <param name="settings">The settings dictionary to inspect.</param>
    /// <param name="name">The original LDtk field name.</param>
    /// <returns><see langword="true"/> when the setting exists; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
        => settings.ContainsKey(HashHelper.Cache32(name));

    /// <summary>
    /// Gets a boolean setting by field name.
    /// </summary>
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
    /// Attempts to get a boolean setting by field name.
    /// </summary>
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
    /// Gets an integer setting by field name.
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
    /// Attempts to get an integer setting by field name.
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
    /// Gets a floating-point setting by field name.
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
    /// Attempts to get a floating-point setting by field name.
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
    /// Gets a point setting by field name.
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
    /// Attempts to get a point setting by field name.
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
    /// Gets a color setting by field name.
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
    /// Attempts to get a color setting by field name.
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
    /// Gets a string setting by field name.
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
    /// Attempts to get a string setting by field name.
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
    /// Gets a file-path setting by field name.
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
    /// Attempts to get a file-path setting by field name.
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
    /// Gets a tile-reference setting by field name.
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
    /// Attempts to get a tile-reference setting by field name.
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
    /// Gets an entity-reference setting by field name.
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
    /// Attempts to get an entity-reference setting by field name.
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
    /// Gets an enum setting by parsing its stored LDtk string value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse.</typeparam>
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
    /// Attempts to get an enum setting by parsing its stored LDtk string value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse.</typeparam>
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
    /// Gets a boolean-array setting by field name.
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
    /// Attempts to get a boolean-array setting by field name.
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
    /// Gets an integer-array setting by field name.
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
    /// Attempts to get an integer-array setting by field name.
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
    /// Gets a floating-point-array setting by field name.
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
    /// Attempts to get a floating-point-array setting by field name.
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
    /// Gets a point-array setting by field name.
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
    /// Attempts to get a point-array setting by field name.
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
    /// Gets a color-array setting by field name.
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
    /// Attempts to get a color-array setting by field name.
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
    /// Gets a string-array setting by field name.
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
    /// Attempts to get a string-array setting by field name.
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
    /// Gets a file-path-array setting by field name.
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
    /// Attempts to get a file-path-array setting by field name.
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
    /// Gets a tile-reference-array setting by field name.
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
    /// Attempts to get a tile-reference-array setting by field name.
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
    /// Gets an entity-reference-array setting by field name.
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
    /// Attempts to get an entity-reference-array setting by field name.
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
    /// Gets an enum-array setting by parsing its stored LDtk string values.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse.</typeparam>
    /// <remarks>
    /// Values that cannot be parsed as <typeparamref name="TEnum"/> are skipped.
    /// </remarks>
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
    /// Attempts to get and parse an enum-array setting by field name.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse.</typeparam>
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
/// Wraps a Boolean LDtk field value.
/// </summary>
public sealed class LDtkBoolSettings(bool value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of Boolean LDtk field values.
/// </summary>
public sealed class LDtkBoolArraySettings(List<bool> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk color field value.
/// </summary>
public sealed class LDtkColorSettings(Color value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk color field values.
/// </summary>
public sealed class LDtkColorArraySettings(List<Color> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk entity-reference field value.
/// </summary>
public sealed class LDtkEntityRefSettings(LDtkEntityRef value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk entity-reference field values.
/// </summary>
public sealed class LDtkEntityRefArraySettings(List<LDtkEntityRef> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk enum field value as its string identifier.
/// </summary>
public sealed class LDtkEnumSettings(string value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk enum field values as string identifiers.
/// </summary>
public sealed class LDtkEnumArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk file-path field value.
/// </summary>
public sealed class LDtkFilePathSettings(string value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk file-path field values.
/// </summary>
public sealed class LDtkFilePathArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// Wraps a floating-point LDtk field value.
/// </summary>
public sealed class LDtkFloatSettings(float value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of floating-point LDtk field values.
/// </summary>
public sealed class LDtkFloatArraySettings(List<float> value) : LDtkSetting(value);

/// <summary>
/// Wraps an integer LDtk field value.
/// </summary>
public sealed class LDtkIntSettings(int value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of integer LDtk field values.
/// </summary>
public sealed class LDtkIntArraySettings(List<int> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk point field value.
/// </summary>
public sealed class LDtkPointSettings(Vect2 value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk point field values.
/// </summary>
public sealed class LDtkPointArraySettings(List<Vect2> value) : LDtkSetting(value);

/// <summary>
/// Wraps a string LDtk field value.
/// </summary>
public sealed class LDtkStringSettings(string value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of string LDtk field values.
/// </summary>
public sealed class LDtkStringArraySettings(List<string> value) : LDtkSetting(value);

/// <summary>
/// Wraps an LDtk tile-reference field value.
/// </summary>
public sealed class LDtkTileSettings(LDtkTile value) : LDtkSetting(value);

/// <summary>
/// Wraps an array of LDtk tile-reference field values.
/// </summary>
public sealed class LDtkTileArraySettings(List<LDtkTile> value) : LDtkSetting(value);
