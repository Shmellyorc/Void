namespace Snap.Engine.Helpers;

public static class JsonHelper
{
    public static T GetPropertyOrDefault<T>(this JsonElement parent, string propName, T defaultValue = default!)
    {
        if (parent.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!parent.TryGetProperty(propName, out var child))
            return defaultValue;
        if (child.ValueKind == JsonValueKind.Null || child.ValueKind == JsonValueKind.Undefined)
            return defaultValue;

        var targetType = typeof(T);
        return targetType switch
        {
            Type t when t == typeof(string) => child.ValueKind == JsonValueKind.String ? (T)(object)child.GetString()! : defaultValue,
            Type t when t == typeof(int) => child.TryGetInt32(out var iValue) ? (T)(object)iValue : defaultValue,
            Type t when t == typeof(uint) => child.TryGetUInt32(out var uiValue) ? (T)(object)uiValue : defaultValue,
            Type t when t == typeof(float) => child.TryGetSingle(out var fValue) ? (T)(object)fValue : defaultValue,
            Type t when t == typeof(bool) =>
                child.ValueKind == JsonValueKind.True || child.ValueKind == JsonValueKind.False
                    ? (T)(object)child.GetBoolean()
                    : defaultValue,
            _ => throw new ArgumentException($"{nameof(GetPropertyOrDefault)}<{targetType.Name}> is not supported")
        };
    }

    public static T GetElementOrDefault<T>(this JsonElement parent, T defaultValue = default!)
    {
        if (parent.ValueKind == JsonValueKind.Null || parent.ValueKind == JsonValueKind.Undefined)
            return defaultValue;

        var targetType = typeof(T);
        return targetType switch
        {
            Type t when t == typeof(string) => parent.ValueKind == JsonValueKind.String ? (T)(object)parent.GetString()! : defaultValue,
            Type t when t == typeof(int) => parent.TryGetInt32(out var iValue) ? (T)(object)iValue : defaultValue,
            Type t when t == typeof(uint) => parent.TryGetUInt32(out var uiValue) ? (T)(object)uiValue : defaultValue,
            Type t when t == typeof(float) => parent.TryGetSingle(out var fValue) ? (T)(object)fValue : defaultValue,
            Type t when t == typeof(bool) =>
                parent.ValueKind == JsonValueKind.True || parent.ValueKind == JsonValueKind.False
                    ? (T)(object)parent.GetBoolean()
                    : defaultValue,
            _ => throw new ArgumentException($"{nameof(GetElementOrDefault)}<{targetType.Name}> is not supported")
        };
    }

    internal static Vect2 GetPosition(this JsonElement parent, string propName)
    {
        if (parent.ValueKind != JsonValueKind.Object)
            return Vect2.Zero;
        if (!parent.TryGetProperty(propName, out var child))
            return Vect2.Zero;
        if (child.ValueKind != JsonValueKind.Array)
            return Vect2.Zero;

        var elements = child.EnumerateArray().ToArray();
        if (elements.Length < 2)
            return Vect2.Zero;

        return new Vect2(elements[0].GetSingle(), elements[1].GetSingle());
    }

    internal static Dictionary<uint, LDtkSetting> GetSettings(JsonElement e)
    {
        var result = new Dictionary<uint, LDtkSetting>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var name = t.GetPropertyOrDefault("__identifier", string.Empty);
            var type = t.GetPropertyOrDefault("__type", string.Empty);
            var value = t.GetProperty("__value");

            if (name.IsEmpty())
                throw new Exception("Map setting has a null name.");
            if (type.IsEmpty())
                throw new Exception($"Map setting has a null type from '{name}'.");

            result[HashHelper.Cache32(name)] = type switch
            {
                // Single items:
                var x when x.StartsWith("Int") =>
                    new LDtkIntSettings(value.GetElementOrDefault<int>()),
                var x when x.StartsWith("Float") =>
                    new LDtkFloatSettings(value.GetElementOrDefault<float>()),
                var x when x.StartsWith("Bool") =>
                    new LDtkBoolSettings(value.GetElementOrDefault<bool>()),
                var x when x.StartsWith("String") =>
                    new LDtkStringSettings(value.GetElementOrDefault(string.Empty)),
                var x when x.StartsWith("Color") =>
                    new LDtkColorSettings(new Color(value.GetElementOrDefault("#ffffff"))),
                var x when x.StartsWith("LocalEnum.") =>
                    new LDtkEnumSettings(value.GetElementOrDefault(string.Empty)),
                var x when x.StartsWith("FilePath") =>
                    new LDtkFilePathSettings(value.GetElementOrDefault(string.Empty)),
                var x when x.StartsWith("Tile") =>
                    new LDtkTileSettings(LDtkTile.Process(value)),
                var x when x.StartsWith("EntityRef") =>
                    new LDtkEntityRefSettings(LDtkEntityRef.Process(value)),
                var x when x.StartsWith("Point") => new LDtkPointSettings(new Vect2(
                        value.GetPropertyOrDefault<int>("cx"), value.GetPropertyOrDefault<int>("cy"))),

                // Array Items:
                var x when x.StartsWith("Array<Int") => new LDtkIntArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault<int>())]),
                var x when x.StartsWith("Array<Float") => new LDtkFloatArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault<float>())]),
                var x when x.StartsWith("Array<Bool") => new LDtkBoolArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault<bool>())]),
                var x when x.StartsWith("Array<String") => new LDtkStringArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault(string.Empty))]),
                var x when x.StartsWith("Array<Color") => new LDtkColorArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => new Color(x.GetElementOrDefault("#ffffff")))]),
                var x when x.StartsWith("Array<LocalEnum.") => new LDtkEnumArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault(string.Empty))]),
                var x when x.StartsWith("Array<FilePath") => new LDtkEnumArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => x.GetElementOrDefault(string.Empty))]),
                var x when x.StartsWith("Array<Tile") => new LDtkTileArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(LDtkTile.Process)]),
                var x when x.StartsWith("Array<EntityRef") => new LDtkEntityRefArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(LDtkEntityRef.Process)]),
                var x when x.StartsWith("Array<Point") => new LDtkPointArraySettings([.. value.EnumerateArray()
                    .Where(x => x.ValueKind != JsonValueKind.Null)
                    .Select(x => new Vect2(
                        x.GetPropertyOrDefault<int>("cx"), x.GetPropertyOrDefault<int>("cy")))]),

                _ => throw new Exception($"Unable to process a map setting from '{name}' with type '{type}'.")
            };
        }

        return result;
    }
}