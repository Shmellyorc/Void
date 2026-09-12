// ============================================================================
//  LDtkMap.cs
// ============================================================================
//  LDtk project asset and lookup cache.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Loads an LDtk project and provides cached access to its levels, layers, entities, and tilesets.
/// </summary>
/// <remarks>
/// <para>
/// Load maps through <see cref="AssetManager"/>. The JSON is parsed on the first
/// <see cref="Load"/> and lookup caches are built for common ID and name queries.
/// </para>
/// <code>
/// var map = AssetManager.Instance.Load&lt;LDtkMap&gt;("Maps/world.ldtk");
///
/// LDtkLevel town = map.GetLevelByName("Town");
/// if (map.TryGetTilesetByName("Terrain", out var tileset))
/// {
///     Texture texture = AssetManager.Instance.LoadTilesetTexture(map, tileset.Id);
/// }
/// </code>
/// </remarks>
public sealed class LDtkMap : IAsset
{
    private readonly Dictionary<uint, LDtkLevel> _levelCacheById = [];
    private readonly Dictionary<uint, LDtkLevel> _levelCacheByName = [];
    private readonly Dictionary<ulong, LDtkEntityInstance> _entityCacheById = [];
    private readonly Dictionary<uint, MapLayer> _layerCacheById = [];
    private readonly Dictionary<uint, LDtkTileset> _tilesetCacheById = [];
    private readonly Dictionary<uint, LDtkTileset> _tilesetCacheByName = [];

    /// <summary>
    /// Gets the asset identifier assigned by VOID.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized asset path or tag used to identify the map.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets whether the map is currently marked as loaded.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets or sets the last access time used by asset eviction tracking.
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets the original LDtk JSON bytes retained by the asset.
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// Gets the asset classification.
    /// </summary>
    public AssetType Type { get; private set; }

    internal LDtkMap(uint id, byte[] data, string filename)
    {
        Id = id;
        Data = data;
        Tag = filename;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Parses the LDtk JSON and builds the map lookup caches when needed.
    /// </summary>
    /// <remarks>
    /// Calling this method on an already loaded map only refreshes
    /// <see cref="LastAccessTime"/>. If the map was unloaded, existing caches are
    /// reused instead of parsing the source JSON again.
    /// </remarks>
    public void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        if (_levelCacheById.Count > 0)
        {
            LastAccessTime = DateTime.Now;
            IsValid = true;
            return;
        }

        using var doc = JsonDocument.Parse(Data);
        var root = doc.RootElement;

        if (!root.TryGetProperty("defs", out var jDefs))
            throw new InvalidOperationException("Unable to find LDtk Defs");
        if (!jDefs.TryGetProperty("tilesets", out var jTilesets))
            throw new InvalidOperationException("Unable to find LDtk Tilesets");
        if (!root.TryGetProperty("defaultGridSize", out var jDefaultGridSize))
            throw new InvalidOperationException("Unable to find LDtk 'DefaultGridSize'.");
        if (!root.TryGetProperty("levels", out var jLevels))
            throw new InvalidOperationException("Unable to find LDtk 'Levels'.");

        var tilesets = LDtkTileset.Process(jTilesets);
        var levels = LDtkLevel.Process(jLevels, jDefaultGridSize.GetInt32());

        foreach (var tileset in tilesets)
        {
            _tilesetCacheById[tileset.Id] = tileset;
            _tilesetCacheByName[HashHelper.Cache32(tileset.Name)] = tileset;
        }

        foreach (var level in levels)
        {
            _levelCacheById[HashHelper.Cache32(level.Id)] = level;
            _levelCacheByName[HashHelper.Cache32(level.Name)] = level;

            foreach (var layer in level.Layers)
            {
                _layerCacheById[HashHelper.Cache32(layer.Id)] = layer;

                if (layer.Type != LDtkLayerType.Entities)
                    continue;

                foreach (var entity in layer.InstanceAs<LDtkEntityInstance>())
                {
                    _entityCacheById[HashHelper.Cache64(entity.Id)] = entity;
                }
            }
        }

        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    /// <summary>
    /// Marks the map as unloaded while retaining its parsed lookup caches.
    /// </summary>
    public void Unload() => IsValid = false;

    /// <summary>
    /// Clears parsed lookup data and marks the map as invalid.
    /// </summary>
    public void Dispose()
    {
        _levelCacheById.Clear();
        _levelCacheByName.Clear();
        _entityCacheById.Clear();
        _layerCacheById.Clear();
        _tilesetCacheById.Clear();
        _tilesetCacheByName.Clear();

        GC.SuppressFinalize(this);
        IsValid = false;
    }

    #region Entity

    /// <summary>
    /// Gets an entity instance by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The entity instance ID.</param>
    /// <returns>The matching entity instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no matching entity is cached.</exception>
    public LDtkEntityInstance GetEntityById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        var hash = HashHelper.Cache64(id);
        if (!_entityCacheById.TryGetValue(hash, out var entity))
            throw new KeyNotFoundException($"Unable to find an entity with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return entity;
    }

    /// <summary>
    /// Attempts to get an entity instance by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The entity instance ID.</param>
    /// <param name="value">Receives the entity when found.</param>
    /// <returns><see langword="true"/> when a matching entity is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetEntityById(string id, out LDtkEntityInstance value)
    {
        if (string.IsNullOrEmpty(id))
        {
            value = null!;
            return false;
        }

        var hash = HashHelper.Cache64(id);
        if (_entityCacheById.TryGetValue(hash, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null!;
        return false;
    }
    #endregion

    #region Layer

    /// <summary>
    /// Gets a layer by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The layer instance ID.</param>
    /// <returns>The matching layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no matching layer is cached.</exception>
    public MapLayer GetLayerById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        if (!_layerCacheById.TryGetValue(HashHelper.Cache32(id), out var layer))
            throw new KeyNotFoundException($"Unable to find a layer with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return layer;
    }

    /// <summary>
    /// Attempts to get a layer by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The layer instance ID.</param>
    /// <param name="value">Receives the layer when found.</param>
    /// <returns><see langword="true"/> when a matching layer is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetLayerById(string id, out MapLayer value)
    {
        if (string.IsNullOrEmpty(id))
        {
            value = null!;
            return false;
        }

        if (_layerCacheById.TryGetValue(HashHelper.Cache32(id), out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null!;
        return false;
    }
    #endregion

    #region Levels

    /// <summary>
    /// Gets a level by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The level instance ID.</param>
    /// <returns>The matching level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no matching level is cached.</exception>
    public LDtkLevel GetLevelById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        var hash = HashHelper.Cache32(id);
        if (!_levelCacheById.TryGetValue(hash, out var level))
            throw new KeyNotFoundException($"Unable to find a level with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return level;
    }

    /// <summary>
    /// Attempts to get a level by its LDtk instance ID.
    /// </summary>
    /// <param name="id">The level instance ID.</param>
    /// <param name="level">Receives the level when found.</param>
    /// <returns><see langword="true"/> when a matching level is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetLevelById(string id, out LDtkLevel level)
    {
        if (string.IsNullOrEmpty(id))
        {
            level = null!;
            return false;
        }

        var hash = HashHelper.Cache32(id);
        if (_levelCacheById.TryGetValue(hash, out level))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        level = null!;
        return false;
    }

    /// <summary>
    /// Gets a level by its LDtk identifier.
    /// </summary>
    /// <param name="name">The level identifier.</param>
    /// <returns>The matching level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no matching level is cached.</exception>
    public LDtkLevel GetLevelByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        var hash = HashHelper.Cache32(name);
        if (!_levelCacheByName.TryGetValue(hash, out var level))
            throw new KeyNotFoundException($"Unable to find a level with the name '{name}'.");

        LastAccessTime = DateTime.Now;
        return level;
    }

    /// <summary>
    /// Attempts to get a level by its LDtk identifier.
    /// </summary>
    /// <param name="name">The level identifier.</param>
    /// <param name="level">Receives the level when found.</param>
    /// <returns><see langword="true"/> when a matching level is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetLevelByName(string name, out LDtkLevel level)
    {
        if (string.IsNullOrEmpty(name))
        {
            level = null!;
            return false;
        }

        var hash = HashHelper.Cache32(name);
        if (_levelCacheByName.TryGetValue(hash, out level))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        level = null!;
        return false;
    }
    #endregion

    #region Tileset

    /// <summary>
    /// Gets a tileset by its LDtk UID.
    /// </summary>
    /// <param name="id">The tileset UID.</param>
    /// <returns>The matching tileset.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no matching tileset is cached.</exception>
    public LDtkTileset GetTilesetById(uint id)
    {
        if (!_tilesetCacheById.TryGetValue(id, out var tileset))
            throw new KeyNotFoundException($"Unable to find a tileset with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return tileset;
    }

    /// <summary>
    /// Attempts to get a tileset by its LDtk UID.
    /// </summary>
    /// <param name="id">The tileset UID.</param>
    /// <param name="value">Receives the tileset when found.</param>
    /// <returns><see langword="true"/> when a matching tileset is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetTilesetById(uint id, out LDtkTileset value)
    {
        if (_tilesetCacheById.TryGetValue(id, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>
    /// Gets a tileset by its LDtk identifier.
    /// </summary>
    /// <param name="name">The tileset identifier.</param>
    /// <returns>The matching tileset.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no matching tileset is cached.</exception>
    public LDtkTileset GetTilesetByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        var hash = HashHelper.Cache32(name);
        if (!_tilesetCacheByName.TryGetValue(hash, out var tileset))
            throw new KeyNotFoundException($"Unable to find a tileset with the name '{name}'.");

        LastAccessTime = DateTime.Now;
        return tileset;
    }

    /// <summary>
    /// Attempts to get a tileset by its LDtk identifier.
    /// </summary>
    /// <param name="name">The tileset identifier.</param>
    /// <param name="value">Receives the tileset when found.</param>
    /// <returns><see langword="true"/> when a matching tileset is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetTilesetByName(string name, out LDtkTileset value)
    {
        if (string.IsNullOrEmpty(name))
        {
            value = null!;
            return false;
        }

        var hash = HashHelper.Cache32(name);
        if (_tilesetCacheByName.TryGetValue(hash, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null!;
        return false;
    }
    #endregion
}
