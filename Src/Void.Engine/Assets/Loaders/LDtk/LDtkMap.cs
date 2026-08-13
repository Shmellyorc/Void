using Void.Engine.Assets.Loaders.LDtk.Instances;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a parsed LDTK project asset, exposing access to levels, layers, entities, and tilesets.
/// Manages internal caches for fast hashed and indexed lookups.
/// </summary>
public sealed class LDtkMap : IAsset
{
    // cached levels, entities, etc:
    private readonly Dictionary<uint, LDtkLevel> _levelCacheById = [];
    private readonly Dictionary<uint, LDtkLevel> _levelCacheByName = [];
    private readonly Dictionary<ulong, LDtkEntityInstance> _entityCacheById = [];
    private readonly Dictionary<uint, MapLayer> _layerCacheById = [];
    private readonly Dictionary<uint, LDtkTileset> _tilesetCacheById = [];
    private readonly Dictionary<uint, LDtkTileset> _tilesetCacheByName = [];

    public uint Id { get; }
    public string Tag { get; }
    public bool IsValid { get; private set; }
    public DateTime LastAccessTime { get; private set; }
    public byte[] Data { get; private set; }
    public AssetType Type { get; private set; }

    internal LDtkMap(uint id, byte[] data, string filename)
    {
        Id = id;
        Data = data;
        Tag = filename;
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

        // Only parse on first load. After LRU unload, data is still cached.
        if (_levelCacheById.Count == 0)
        {
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
        }

        IsValid = true;
        LastAccessTime = DateTime.Now;
    }

    public void Unload()
    {
        // Keep dictionaries loaded for LRU cache reuse
        IsValid = false;
    }

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

    public bool TryGetEntityById(string id, out LDtkEntityInstance value)
    {
        if (string.IsNullOrEmpty(id))
        {
            value = null;
            return false;
        }

        var hash = HashHelper.Cache64(id);
        if (_entityCacheById.TryGetValue(hash, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null;
        return false;
    }
    #endregion


    #region Layer
    public MapLayer GetLayerById(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentNullException(nameof(id));
        if (!_layerCacheById.TryGetValue(HashHelper.Cache32(id), out var layer))
            throw new KeyNotFoundException($"Unable to find a layer with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return layer;
    }

    public bool TryGetLayerById(string id, out MapLayer value)
    {
        if (string.IsNullOrEmpty(id))
        {
            value = null;
            return false;
        }

        if (_layerCacheById.TryGetValue(HashHelper.Cache32(id), out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null;
        return false;
    }
    #endregion


    #region Levels
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

    public bool TryGetLevelById(string id, out LDtkLevel level)
    {
        if (string.IsNullOrEmpty(id))
        {
            level = null;
            return false;
        }

        var hash = HashHelper.Cache32(id);
        if (_levelCacheById.TryGetValue(hash, out level))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        level = null;
        return false;
    }

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

    public bool TryGetLevelByName(string name, out LDtkLevel level)
    {
        if (string.IsNullOrEmpty(name))
        {
            level = null;
            return false;
        }

        var hash = HashHelper.Cache32(name);
        if (_levelCacheByName.TryGetValue(hash, out level))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        level = null;
        return false;
    }
    #endregion


    #region Tileset
    public LDtkTileset GetTilesetById(uint id)
    {
        if (!_tilesetCacheById.TryGetValue(id, out var tileset))
            throw new KeyNotFoundException($"Unable to find a tileset with the id '{id}'.");

        LastAccessTime = DateTime.Now;
        return tileset;
    }

    public bool TryGetTilesetById(uint id, out LDtkTileset value)
    {
        if (_tilesetCacheById.TryGetValue(id, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null;
        return false;
    }

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

    public bool TryGetTilesetByName(string name, out LDtkTileset value)
    {
        if (string.IsNullOrEmpty(name))
        {
            value = null;
            return false;
        }

        var hash = HashHelper.Cache32(name);
        if (_tilesetCacheByName.TryGetValue(hash, out value))
        {
            LastAccessTime = DateTime.Now;
            return true;
        }

        value = null;
        return false;
    }
    #endregion
}