namespace Void.Engine.Assets.Loaders.LDtk;

public sealed class LDtkBoolSettings(bool value) : LDtkSetting(value);
public sealed class LDtkBoolArraySettings(List<bool> value) : LDtkSetting(value);
public sealed class LDtkColorSettings(Color value) : LDtkSetting(value);
public sealed class LDtkColorArraySettings(List<Color> value) : LDtkSetting(value);
public sealed class LDtkEntityRefSettings(LDtkEntityRef value) : LDtkSetting(value);
public sealed class LDtkEntityRefArraySettings(List<LDtkEntityRef> value) : LDtkSetting(value);
public sealed class LDtkEnumSettings(string value) : LDtkSetting(value);
public sealed class LDtkEnumArraySettings(List<string> value) : LDtkSetting(value);
public sealed class LDtkFilePathSettings(string value) : LDtkSetting(value);
public sealed class LDtkFilePathArraySettings(List<string> value) : LDtkSetting(value);
public sealed class LDtkFloatSettings(float value) : LDtkSetting(value);
public sealed class LDtkFloatArraySettings(List<float> value) : LDtkSetting(value);
public sealed class LDtkIntSettings(int value) : LDtkSetting(value);
public sealed class LDtkIntArraySettings(List<int> value) : LDtkSetting(value);
public sealed class LDtkPointSettings(Vect2 value) : LDtkSetting(value);
public sealed class LDtkPointArraySettings(List<Vect2> value) : LDtkSetting(value);
public sealed class LDtkStringSettings(string value) : LDtkSetting(value);
public sealed class LDtkStringArraySettings(List<string> value) : LDtkSetting(value);
public sealed class LDtkTileSettings(LDtkTile value) : LDtkSetting(value);
public sealed class LDtkTileArraySettings(List<LDtkTile> value) : LDtkSetting(value);

public class LDtkSetting(object value)
{
    public object Value { get; } = value;

    public T ValueAs<T>() => (T)Value;

    public static bool Contains(IReadOnlyDictionary<uint, LDtkSetting> settings, string name)
        => settings.ContainsKey(HashHelper.Cache32(name));

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

    public static bool TryGetBoolArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<bool> setting)
    {
        try
        {
            setting = GetBoolArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetIntArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<int> setting)
    {
        try
        {
            setting = GetIntArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetFloatArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<float> setting)
    {
        try
        {
            setting = GetFloatArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetPointArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<Vect2> setting)
    {
        try
        {
            setting = GetPointArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetColorArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<Color> setting)
    {
        try
        {
            setting = GetColorArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetStringArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<string> setting)
    {
        try
        {
            setting = GetStringArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetFilePathArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<string> setting)
    {
        try
        {
            setting = GetFilePathArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetTileArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<LDtkTile> setting)
    {
        try
        {
            setting = GetTileArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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

    public static bool TryGetEntityRefArraySetting(IReadOnlyDictionary<uint, LDtkSetting> settings, string name, out IReadOnlyList<LDtkEntityRef> setting)
    {
        try
        {
            setting = GetEntityRefArraySetting(settings, name);
            return true;
        }
        catch
        {
            setting = default;
            return false;
        }
    }

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
            setting = default;
            return false;
        }
    }
}