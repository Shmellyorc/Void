// ============================================================================
//  Spritesheet.cs
// ============================================================================
//  Spritesheet asset that parses named bounds, patches, and pivots from a
//  JSON spritesheet definition.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.Spritesheets;

/// <summary>
/// Provides named sprite bounds, nine-patch regions, and pivots loaded from a
/// spritesheet definition.
/// </summary>
/// <remarks>
/// <para>
/// Spritesheets are loaded through <see cref="AssetManager"/> and parsed from
/// the slice data in the JSON <c>meta.slices</c> array. Each slice uses its
/// first key as the source of bounds, center, and pivot data.
/// </para>
/// <para>
/// Parsed entries are retained when <see cref="Unload"/> is called, allowing a
/// later <see cref="Load"/> to reactivate the asset without reparsing the JSON.
/// </para>
/// <code>
/// var sheet = AssetManager.Instance.Load&lt;Spritesheet&gt;("Sprites/player.sheet");
///
/// Rect2 idle = sheet.GetBound("idle");
///
/// if (sheet.TryGetPatch("panel", out Rect2 patch))
/// {
///     // Use patch for nine-patch rendering.
/// }
/// </code>
/// </remarks>
public sealed class Spritesheet : IAsset
{
    private readonly Dictionary<uint, SpritesheetEntry> _entries = [];

    /// <summary>
    /// Gets the unique asset identifier.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag associated with this spritesheet.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the original JSON data used to build the spritesheet entries.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the asset classification.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets whether the spritesheet is currently loaded.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the last time this spritesheet was successfully loaded or queried.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Creates a spritesheet asset from encoded JSON data.
    /// </summary>
    /// <param name="id">The unique asset identifier.</param>
    /// <param name="data">The JSON spritesheet data.</param>
    /// <param name="tag">The normalized path or tag associated with the asset.</param>
    public Spritesheet(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Loads the spritesheet and parses its entries when they have not already
    /// been created.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required spritesheet metadata is missing.
    /// </exception>
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
                var name = jName.GetString();
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
                    int x = jCenter.GetProperty("x").GetInt32();
                    int y = jCenter.GetProperty("y").GetInt32();
                    int w = jCenter.GetProperty("w").GetInt32();
                    int h = jCenter.GetProperty("h").GetInt32();

                    patch = new Rect2(x, y, bounds.Width - x - w, bounds.Height - y - h);
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

    /// <summary>
    /// Marks the spritesheet as unloaded while retaining its parsed entries.
    /// </summary>
    public void Unload() => IsValid = false;

    /// <summary>
    /// Clears all parsed spritesheet entries.
    /// </summary>
    public void Dispose()
    {
        _entries.Clear();

        GC.SuppressFinalize(this);
    }

    #region GetBounds
    /// <summary>
    /// Gets bounds for each requested sprite that has valid bounds data.
    /// </summary>
    /// <param name="names">The sprite names to query.</param>
    /// <returns>
    /// The bounds found for the requested names, in request order. Missing or
    /// empty entries are skipped.
    /// </returns>
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

    /// <summary>
    /// Gets the source bounds for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The sprite bounds.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the sprite does not exist or its bounds are empty.
    /// </exception>
    public Rect2 GetBound(string name)
    {
        var hash = HashHelper.Cache32(name);
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Bounds.IsEmpty)
            throw new InvalidOperationException("Bounds is empty");

        LastAccessTime = DateTime.Now;

        return value.Bounds;
    }

    /// <summary>
    /// Attempts to get the source bounds for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">Receives the sprite bounds when found.</param>
    /// <returns>
    /// <see langword="true"/> when the sprite exists and has non-empty bounds;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetBounds(string name, out Rect2 value)
    {
        try
        {
            value = GetBound(name);
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
    /// <summary>
    /// Gets nine-patch regions for each requested sprite that has valid patch data.
    /// </summary>
    /// <param name="names">The sprite names to query.</param>
    /// <returns>
    /// The patch regions found for the requested names, in request order. Missing
    /// or empty entries are skipped.
    /// </returns>
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

    /// <summary>
    /// Gets the nine-patch region for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The sprite patch region.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the sprite does not exist or its patch region is empty.
    /// </exception>
    public Rect2 GetPatch(string name)
    {
        var hash = HashHelper.Cache32(name);
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Patch.IsEmpty)
            throw new InvalidOperationException("Patch is empty");

        LastAccessTime = DateTime.Now;

        return value.Patch;
    }

    /// <summary>
    /// Attempts to get the nine-patch region for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">Receives the patch region when found.</param>
    /// <returns>
    /// <see langword="true"/> when the sprite exists and has a non-empty patch;
    /// otherwise, <see langword="false"/>.
    /// </returns>
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
    /// <summary>
    /// Gets pivots for each requested sprite that has a nonzero pivot.
    /// </summary>
    /// <param name="names">The sprite names to query.</param>
    /// <returns>
    /// The pivots found for the requested names, in request order. Missing entries
    /// and zero pivots are skipped.
    /// </returns>
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

    /// <summary>
    /// Gets the pivot for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The sprite pivot.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the sprite does not exist or its pivot is <see cref="Vect2.Zero"/>.
    /// </exception>
    public Vect2 GetPivot(string name)
    {
        var hash = HashHelper.Cache32(name);
        if (!_entries.TryGetValue(hash, out var value))
            throw new InvalidOperationException($"'{name}' doesnt exist.");
        if (value.Pivot.IsZero)
            throw new InvalidOperationException("Pivot is empty");

        LastAccessTime = DateTime.Now;

        return value.Pivot;
    }

    /// <summary>
    /// Attempts to get the pivot for a named sprite.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">Receives the pivot when found.</param>
    /// <returns>
    /// <see langword="true"/> when the sprite exists and has a nonzero pivot;
    /// otherwise, <see langword="false"/>.
    /// </returns>
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
