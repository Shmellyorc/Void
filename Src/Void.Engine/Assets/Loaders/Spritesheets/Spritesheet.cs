namespace Void.Engine.Assets.Loaders.Spritesheets;

public sealed class Spritesheet : IAsset
{
    private readonly Dictionary<uint, SpritesheetEntry> _entries = [];

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public AssetType Type { get; }
    public bool IsValid { get; private set; }
    public DateTime LastAccessTime { get; private set; }

    public Spritesheet(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    public void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        if (_entries.IsEmpty())
        {
            var root = JsonDocument.Parse(Data).RootElement;

            if (!root.TryGetProperty("meta", out var jMeta))
                throw new InvalidOperationException($"Unable to find spritesheet metadata");
            if (!jMeta.TryGetProperty("slices", out var jSlices))
                throw new InvalidOperationException($"Unable to find spritesheet slices");

            _entries.EnsureCapacity(jSlices.GetArrayLength());
            foreach (var item in jSlices.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var jName))
                    throw new InvalidOperationException($"Unable to find spritesheet name");
                if (!item.TryGetProperty("keys", out var jKeys))
                    throw new InvalidOperationException($"Unable to find spritesheet keys");

                var keyItem = jKeys[0];
                var name = jName.GetString().Intern();
                var hash = HashHelper.Cache32(name);

                if (_entries.ContainsKey(hash))
                {
                    System.Console.WriteLine($"Spritesheet entry '{name}' already exists, skipping this one.");
                    continue;
                }

                var bounds = Rect2.Empty;
                var patch = Rect2.Empty;
                var pivot = Vect2.Zero;

                if (keyItem.TryGetProperty("bounds", out var jBounds))
                {
                    bounds = new Rect2(
                        jBounds.GetProperty("x").GetInt32(),
                        jBounds.GetProperty("y").GetInt32(),
                        jBounds.GetProperty("w").GetInt32(),
                        jBounds.GetProperty("h").GetInt32()
                    );
                }

                if (keyItem.TryGetProperty("center", out var jCenter))
                {
                    patch = new Rect2(
                        jCenter.GetProperty("x").GetInt32(),
                        jCenter.GetProperty("y").GetInt32(),
                        jCenter.GetProperty("w").GetInt32(),
                        jCenter.GetProperty("h").GetInt32()
                    );
                }

                if (keyItem.TryGetProperty("pivot", out var jPivot))
                {
                    pivot = new Vect2(
                        jPivot.GetProperty("x").GetInt32(),
                        jPivot.GetProperty("y").GetInt32()
                    );
                }

                _entries[hash] = new SpritesheetEntry(bounds, patch, pivot);
            }
        }

        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    public void Unload() => IsValid = false;

    public void Dispose()
    {
        _entries.Clear();

        GC.SuppressFinalize(this);
    }



    #region GetBounds
    public IReadOnlyList<Rect2> GetBounds(params string[] names)
    {
        if (names.IsEmpty())
            return Array.Empty<Rect2>();

        var result = new List<Rect2>(names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            if (!TryGetBounds(names[i], out var item))
                continue;

            result.Add(item);
        }

        return result;
    }
    public Rect2 GetBounds(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Bounds.IsEmpty)
            throw new InvalidOperationException("Bounds is empty");

        LastAccessTime = DateTime.Now;

        return value.Bounds;
    }
    public bool TryGetBounds(string name, out Rect2 value)
    {
        try
        {
            value = GetBounds(name);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    #endregion



    #region GetPatch
    public IReadOnlyList<Rect2> GetPatches(params string[] names)
    {
        if (names.IsEmpty())
            return Array.Empty<Rect2>();

        var result = new List<Rect2>(names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            if (!TryGetPatch(names[i], out var item))
                continue;

            result.Add(item);
        }

        return result;
    }
    public Rect2 GetPatch(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Patch.IsEmpty)
            throw new InvalidOperationException("Patch is empty");

        LastAccessTime = DateTime.Now;

        return value.Patch;
    }
    public bool TryGetPatch(string name, out Rect2 value)
    {
        try
        {
            value = GetPatch(name);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    #endregion



    #region GetPivot
    public IReadOnlyList<Vect2> GetPivots(params string[] names)
    {
        if (names.IsEmpty())
            return Array.Empty<Vect2>();

        var result = new List<Vect2>(names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            if (!TryGetPivot(names[i], out var item))
                continue;

            result.Add(item);
        }

        return result;
    }
    public Vect2 GetPivot(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Pivot.IsZero)
            throw new InvalidOperationException("Pivot is empty");

        LastAccessTime = DateTime.Now;

        return value.Pivot;
    }
    public bool TryGetPivot(string name, out Vect2 value)
    {
        try
        {
            value = GetPivot(name);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
    #endregion
}
