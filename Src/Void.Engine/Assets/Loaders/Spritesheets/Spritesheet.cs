using System.Text.Json;

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
    }

    public void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        if (_entries.Count == 0)
        {
            var root = JsonDocument.Parse(Data).RootElement;

            if (!root.TryGetProperty("meta", out var jMeta))
                throw new InvalidOperationException($"Unable to find spritesheet metadata");
            if (jMeta.TryGetProperty("slices", out var jSlices))
                throw new InvalidOperationException($"Unable to find spritesheet slices");

            _entries.EnsureCapacity(jSlices.GetArrayLength());
            foreach (var item in jSlices.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var jName))
                    throw new InvalidOperationException($"Unable to find spritesheet name");
                if (!item.TryGetProperty("keys", out var jKeys))
                    throw new InvalidOperationException($"Unable to find spritesheet keys");

                var keyItem = jKeys[0];
                var hash = HashHelper.Cache32(jName.GetString().Intern());

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





    public Rect2 GetBounds(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Bounds.IsEmpty)
            throw new InvalidOperationException("Bounds is empty");

        return value.Bounds;
    }

    public Rect2 GetPatch(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Patch.IsEmpty)
            throw new InvalidOperationException("Patch is empty");

        return value.Patch;
    }
    public Vect2 GetPivot(string name)
    {
        var hash = HashHelper.Cache32(name.Intern());
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Pivot.IsZero)
            throw new InvalidOperationException("Pivot is empty");

        return value.Pivot;
    }
}
