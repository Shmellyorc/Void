// ============================================================================
//  LDtkMap.cs
// ============================================================================
//  LDtk map asset that loads and provides access to levels, layers, entities,
//  tilesets, and settings from an LDtk project file.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// LDtk map asset that loads and provides access to levels, layers, entities,
/// tilesets, and settings from an LDtk project file.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkMap"/> class implements <see cref="IAsset"/> and provides
/// a strongly-typed interface for accessing all data from an LDtk project file.
/// It parses the JSON once and builds caches for fast lookup of levels, layers,
/// entities, and tilesets.
/// </para>
/// <para>
/// <b>Data Structure:</b>
/// <list type="bullet">
///   <item><description><see cref="LDtkMap"/> → Contains <see cref="LDtkLevel"/>s and <see cref="LDtkTileset"/>s</description></item>
///   <item><description><see cref="LDtkLevel"/> → Contains <see cref="MapLayer"/>s and settings</description></item>
///   <item><description><see cref="MapLayer"/> → Contains <see cref="ILDtkInstance"/> items (entities, tiles, int grid)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lookup Methods:</b>
/// <list type="bullet">
///   <item><description><b>Levels:</b> By ID or name</description></item>
///   <item><description><b>Layers:</b> By ID</description></item>
///   <item><description><b>Entities:</b> By ID</description></item>
///   <item><description><b>Tilesets:</b> By ID or name</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load the LDtk map through AssetManager
/// var map = AssetManager.Instance.Load&lt;LDtkMap&gt;("levels/level.ldtk");
/// 
/// // Get a level by name
/// var level = map.GetLevelByName("Level_01");
/// 
/// // Get a layer by ID
/// var layer = map.GetLayerById("layer_id");
/// 
/// // Get all entities in a layer
/// var entities = layer.InstanceAs&lt;LDtkEntityInstance&gt;();
/// 
/// // Get a specific entity by ID
/// var entity = map.GetEntityById("entity_id");
/// 
/// // Get a tileset
/// var tileset = map.GetTilesetByName("Tileset_01");
/// 
/// // Access level settings
/// var setting = LDtkSetting.GetStringSetting(level.Settings, "SettingName");
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
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
    /// Gets the unique identifier of the map.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the map.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets a value indicating whether the map is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the last access time of the map for eviction tracking.
    /// </summary>
    public uint LastAccessTick { get; set; }

    /// <summary>
    /// Gets the raw map data bytes.
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// Gets the asset type.
    /// </summary>
    public AssetType Type { get; private set; }

    internal LDtkMap(uint id, byte[] data, string filename)
    {
        Id = id;
        Data = data;
        Tag = filename;
        Type = AssetType.Normal;
    }

    /// <summary>
    /// Loads the LDtk map by parsing the JSON data and building lookup caches.
    /// </summary>
    public void Load()
    {
        if (IsValid)
        {
            return;
        }

        if (_levelCacheById.Count > 0)
        {
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

        IsValid = true;
    }

    /// <summary>
    /// Unloads the map data from memory while keeping the caches for fast reloading.
    /// </summary>
    public void Unload() => IsValid = false;

    /// <summary>
    /// Disposes the map and clears all cached data.
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
    /// Gets an entity instance by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>The entity instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the entity is not found.</exception>
    public LDtkEntityInstance GetEntityById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        var hash = HashHelper.Cache64(id);
        if (!_entityCacheById.TryGetValue(hash, out var entity))
            throw new KeyNotFoundException($"Unable to find an entity with the id '{id}'.");

        AssetManager.Instance.Touch(this);
        return entity;
    }

    /// <summary>
    /// Attempts to get an entity instance by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="value">When this method returns, contains the entity instance if found.</param>
    /// <returns><see langword="true"/> if the entity was found; otherwise, <see langword="false"/>.</returns>
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
            AssetManager.Instance.Touch(this);
            return true;
        }

        value = null!;
        return false;
    }
    #endregion

    #region Layer

    /// <summary>
    /// Gets a layer by its ID.
    /// </summary>
    /// <param name="id">The layer ID.</param>
    /// <returns>The layer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the layer is not found.</exception>
    public MapLayer GetLayerById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        if (!_layerCacheById.TryGetValue(HashHelper.Cache32(id), out var layer))
            throw new KeyNotFoundException($"Unable to find a layer with the id '{id}'.");

        AssetManager.Instance.Touch(this);
        return layer;
    }

    /// <summary>
    /// Attempts to get a layer by its ID.
    /// </summary>
    /// <param name="id">The layer ID.</param>
    /// <param name="value">When this method returns, contains the layer if found.</param>
    /// <returns><see langword="true"/> if the layer was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetLayerById(string id, out MapLayer value)
    {
        if (string.IsNullOrEmpty(id))
        {
            value = null!;
            return false;
        }

        if (_layerCacheById.TryGetValue(HashHelper.Cache32(id), out value))
        {
            AssetManager.Instance.Touch(this);
            return true;
        }

        value = null!;
        return false;
    }
    #endregion

    #region Levels

    /// <summary>
    /// Gets a level by its ID.
    /// </summary>
    /// <param name="id">The level ID.</param>
    /// <returns>The level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the level is not found.</exception>
    public LDtkLevel GetLevelById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        var hash = HashHelper.Cache32(id);
        if (!_levelCacheById.TryGetValue(hash, out var level))
            throw new KeyNotFoundException($"Unable to find a level with the id '{id}'.");

        AssetManager.Instance.Touch(this);
        return level;
    }

    /// <summary>
    /// Attempts to get a level by its ID.
    /// </summary>
    /// <param name="id">The level ID.</param>
    /// <param name="level">When this method returns, contains the level if found.</param>
    /// <returns><see langword="true"/> if the level was found; otherwise, <see langword="false"/>.</returns>
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
            AssetManager.Instance.Touch(this);
            return true;
        }

        level = null!;
        return false;
    }

    /// <summary>
    /// Gets a level by its name.
    /// </summary>
    /// <param name="name">The level name.</param>
    /// <returns>The level.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the level is not found.</exception>
    public LDtkLevel GetLevelByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        var hash = HashHelper.Cache32(name);
        if (!_levelCacheByName.TryGetValue(hash, out var level))
            throw new KeyNotFoundException($"Unable to find a level with the name '{name}'.");

        AssetManager.Instance.Touch(this);
        return level;
    }

    /// <summary>
    /// Attempts to get a level by its name.
    /// </summary>
    /// <param name="name">The level name.</param>
    /// <param name="level">When this method returns, contains the level if found.</param>
    /// <returns><see langword="true"/> if the level was found; otherwise, <see langword="false"/>.</returns>
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
            AssetManager.Instance.Touch(this);
            return true;
        }

        level = null!;
        return false;
    }
    #endregion

    #region Tileset

    /// <summary>
    /// Gets a tileset by its ID.
    /// </summary>
    /// <param name="id">The tileset ID.</param>
    /// <returns>The tileset.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the tileset is not found.</exception>
    public LDtkTileset GetTilesetById(uint id)
    {
        if (!_tilesetCacheById.TryGetValue(id, out var tileset))
            throw new KeyNotFoundException($"Unable to find a tileset with the id '{id}'.");

        AssetManager.Instance.Touch(this);
        return tileset;
    }

    /// <summary>
    /// Attempts to get a tileset by its ID.
    /// </summary>
    /// <param name="id">The tileset ID.</param>
    /// <param name="value">When this method returns, contains the tileset if found.</param>
    /// <returns><see langword="true"/> if the tileset was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetTilesetById(uint id, out LDtkTileset value)
    {
        if (_tilesetCacheById.TryGetValue(id, out value))
        {
            AssetManager.Instance.Touch(this);
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>
    /// Gets a tileset by its name.
    /// </summary>
    /// <param name="name">The tileset name.</param>
    /// <returns>The tileset.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the tileset is not found.</exception>
    public LDtkTileset GetTilesetByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        var hash = HashHelper.Cache32(name);
        if (!_tilesetCacheByName.TryGetValue(hash, out var tileset))
            throw new KeyNotFoundException($"Unable to find a tileset with the name '{name}'.");

        AssetManager.Instance.Touch(this);
        return tileset;
    }

    /// <summary>
    /// Attempts to get a tileset by its name.
    /// </summary>
    /// <param name="name">The tileset name.</param>
    /// <param name="value">When this method returns, contains the tileset if found.</param>
    /// <returns><see langword="true"/> if the tileset was found; otherwise, <see langword="false"/>.</returns>
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
            AssetManager.Instance.Touch(this);
            return true;
        }

        value = null!;
        return false;
    }
    #endregion
}