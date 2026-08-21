// ============================================================================
//  Spritesheet.cs
// ============================================================================
//  Spritesheet asset that parses and provides access to sprite data from
//  a JSON spritesheet definition.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.Spritesheets;

/// <summary>
/// A spritesheet asset that parses and provides access to sprite data from
/// a JSON spritesheet definition.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Spritesheet"/> class implements <see cref="IAsset"/> and
/// parses spritesheet JSON data to provide access to sprite bounds, patches,
/// and pivots by name. It supports loading from file data through the
/// <see cref="AssetManager"/>.
/// </para>
/// <para>
/// <b>JSON Format:</b>
/// The spritesheet JSON should follow a format with a "meta" object containing
/// a "slices" array. Each slice must have:
/// <list type="bullet">
///   <item><description>"name" - The name of the sprite</description></item>
///   <item><description>"keys" - Array containing at least one key with bounds, center, and pivot data</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a spritesheet through AssetManager
/// var spritesheet = AssetManager.Instance.Load&lt;Spritesheet&gt;("sprites/player.sheet");
/// 
/// // Get a sprite bounds
/// Rect2 bounds = spritesheet.GetBound("walking_01");
/// 
/// // Try get with fallback
/// if (spritesheet.TryGetBounds("walking_01", out var bounds))
/// {
///     // Use bounds
/// }
/// 
/// // Get multiple sprite bounds
/// var boundsList = spritesheet.GetBounds("walking_01", "walking_02", "walking_03");
/// 
/// // Get patch and pivot data
/// Rect2 patch = spritesheet.GetPatch("character");
/// Vect2 pivot = spritesheet.GetPivot("character");
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class Spritesheet : IAsset
{
    private readonly Dictionary<uint, SpritesheetEntry> _entries = [];

    /// <summary>
    /// Gets the unique identifier of the spritesheet.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the spritesheet.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the raw spritesheet data bytes.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the asset type.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the spritesheet is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the last access time of the spritesheet for eviction tracking.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spritesheet"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the asset.</param>
    /// <param name="data">The raw spritesheet data bytes.</param>
    /// <param name="tag">The normalized path or tag used to identify the asset.</param>
    public Spritesheet(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Loads the spritesheet data by parsing the JSON definition.
    /// </summary>
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

    /// <summary>
    /// Unloads the spritesheet data from memory.
    /// </summary>
    public void Unload() => IsValid = false;

    /// <summary>
    /// Disposes the spritesheet and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _entries.Clear();

        GC.SuppressFinalize(this);
    }

    #region GetBounds
    /// <summary>
    /// Gets the bounds for multiple sprite names.
    /// </summary>
    /// <param name="names">The sprite names to get bounds for.</param>
    /// <returns>A list of bounds for the specified sprites.</returns>
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
    /// Gets the bounds for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The bounds of the sprite.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sprite name does not exist or bounds are empty.</exception>
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
    /// Attempts to get the bounds for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">When this method returns, contains the bounds if successful.</param>
    /// <returns><see langword="true"/> if the bounds were found; otherwise, <see langword="false"/>.</returns>
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
    /// Gets the patch for multiple sprite names.
    /// </summary>
    /// <param name="names">The sprite names to get patches for.</param>
    /// <returns>A list of patches for the specified sprites.</returns>
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
    /// Gets the patch for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The patch of the sprite.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sprite name does not exist or patch is empty.</exception>
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
    /// Attempts to get the patch for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">When this method returns, contains the patch if successful.</param>
    /// <returns><see langword="true"/> if the patch was found; otherwise, <see langword="false"/>.</returns>
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
    /// Gets the pivot for multiple sprite names.
    /// </summary>
    /// <param name="names">The sprite names to get pivots for.</param>
    /// <returns>A list of pivots for the specified sprites.</returns>
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
    /// Gets the pivot for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <returns>The pivot of the sprite.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sprite name does not exist or pivot is zero.</exception>
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
    /// Attempts to get the pivot for a specific sprite name.
    /// </summary>
    /// <param name="name">The sprite name.</param>
    /// <param name="value">When this method returns, contains the pivot if successful.</param>
    /// <returns><see langword="true"/> if the pivot was found; otherwise, <see langword="false"/>.</returns>
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