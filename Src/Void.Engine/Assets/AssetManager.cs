// ============================================================================
//  AssetManager.cs
// ============================================================================
//  Manages asset loading, caching, mount search order, pack files, and custom
//  asset type registration.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets;

/// <summary>
/// Manages assets loaded through VOID's virtual content system.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssetManager"/> is a singleton that loads assets through an ordered
/// collection of mounts and caches the resulting <see cref="IAsset"/> instances.
/// Repeated requests for the same cached path return the existing asset and reload
/// it when necessary.
/// </para>
/// <para>
/// Mounts are searched from first to last. Use <see cref="AddMountToStart"/> for
/// sources that should override lower-priority content and <see cref="AddMountToEnd"/>
/// for fallback sources.
/// </para>
/// <para>
/// Custom asset types can be added with <see cref="RegisterAssetType{T}"/>. Pack
/// files can be opened with <see cref="LoadPack(string,string)"/> or
/// <see cref="LoadPack(string,string,string)"/> and then added to the mount order.
/// </para>
/// <code>
/// var assets = AssetManager.Instance;
///
/// Texture player = assets.Load&lt;Texture&gt;("Sprites/player.png");
/// Sound pickup = assets.LoadSound("Audio/pickup.ogg");
///
/// var pack = assets.LoadPack("Packs/game.pack");
/// assets.AddMountToStart(pack);
/// </code>
/// </remarks>
public sealed class AssetManager
{
    #region fields
    private static uint s_id;
    private DateTime _lastEvictionCheck = DateTime.MinValue;
    private static readonly Lazy<AssetManager> _instance =
        new(() => new AssetManager());
    private static readonly Lock IdLock = new();
    private readonly ConcurrentDictionary<ulong, IAsset> _assets = [];
    private readonly List<IMount> _mounts = [];

    private static readonly HashSet<Type> EngineAssetTypes =
    [
        typeof(Texture),
        typeof(LDtkMap),
        typeof(SpriteFont),
        typeof(Spritesheet),
        typeof(Sound),
        typeof(Shader),
    ];

    private static readonly Dictionary<Type, string[]> SupportedExtensions = new()
    {
        {typeof(Texture), [".png", ".bmp", ".tga", ".jpg", ".gif", ".psd", ".hdr", ".pic", ".pnm"] },
        {typeof(LDtkMap), [".ldtk", ".json"]},
        {typeof(SpriteFont), [".png", ".bmp", ".tga", ".jpg", ".gif", ".psd", ".hdr", ".pic", ".pnm"]},
        {typeof(Spritesheet), [".sheet", ".json"]},
        {typeof(Sound), [
            ".ogg", ".wav", ".flac", ".mp3", ".aiff", ".au", ".raw", ".paf", ".svx", ".nist", ".voc",
            ".ircam", ".w64", ".mat4", ".mat5", ".pvf", ".htk", ".sds", ".avr", ".sd2", ".caf", ".wve",
            ".mpc2k", ".rf64"
        ]},
        {typeof(Shader), [".shader"]},
    };

    private static readonly Dictionary<Type, Func<uint, byte[], string, IAsset>> SupportedLoaders = new()
    {
        {typeof(Texture), (id, data, tag) => new Texture(id, data, tag, false, false)},
        {typeof(LDtkMap), (id, data, tag) => new LDtkMap(id, data, tag)},
        {typeof(SpriteFont), (id, data, tag) => new SpriteFont(id, data, tag, SpriteFont.CharsetFull)},
        {typeof(Spritesheet), (id, data, tag) => new Spritesheet(id, data, tag)},
        {typeof(Sound), (id, data, tag) => new Sound(id, data, tag, SoundPriority.Normal)},
        {typeof(Shader), (id, data, tag) => new Shader(id, data, tag)},
    };
    #endregion

    #region Properties

    /// <summary>
    /// Gets the shared asset manager instance.
    /// </summary>
    public static AssetManager Instance => _instance.Value;

    /// <summary>
    /// Gets the active mounts in their current search order.
    /// </summary>
    public IReadOnlyList<IMount> Mounts => _mounts;
    #endregion

    #region Constructor
    private AssetManager()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _mounts.Add(new MacOsMount());

        _mounts.Add(new VirtualFileSystemMount());

        Logger.Instance.InfoWithCategory("AssetManager", "Initialized with {0} mounts", _mounts.Count);
    }
    #endregion

    #region Mounts
    /// <summary>
    /// Adds a mount at the highest-priority position in the search order.
    /// </summary>
    /// <param name="mount">The mount to add.</param>
    public void AddMountToStart(IMount mount) => _mounts.Insert(0, mount);

    /// <summary>
    /// Adds a mount at the lowest-priority position in the search order.
    /// </summary>
    /// <param name="mount">The mount to add.</param>
    public void AddMountToEnd(IMount mount) => _mounts.Add(mount);

    /// <summary>
    /// Inserts a mount at a specific position in the search order.
    /// </summary>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="mount">The mount to insert.</param>
    public void InsertMount(int index, IMount mount) => _mounts.Insert(index, mount);

    /// <summary>
    /// Removes a mount from the search order.
    /// </summary>
    /// <param name="mount">The mount to remove.</param>
    public void RemoveMount(IMount mount) => _mounts.Remove(mount);

    /// <summary>
    /// Removes all current mounts and restores VOID's default mount configuration.
    /// </summary>
    public void ClearMounts()
    {
        _mounts.Clear();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _mounts.Add(new MacOsMount());

        _mounts.Add(new VirtualFileSystemMount());
    }
    #endregion

    #region Pack Mounts
    /// <summary>
    /// Opens a pack file and creates a mount for it.
    /// </summary>
    /// <param name="packPath">The pack path relative to the configured content root.</param>
    /// <param name="mountName">An optional display name for the mount.</param>
    /// <returns>The created pack mount.</returns>
    /// <remarks>
    /// The returned mount is not added to the asset search order automatically.
    /// Add it with <see cref="AddMountToStart"/>, <see cref="AddMountToEnd"/>, or
    /// <see cref="InsertMount"/>. If a companion <c>.key</c> file exists beside
    /// the pack, it is loaded automatically.
    /// </remarks>
    public PackMount LoadPack(string packPath, string mountName = null)
        => LoadPack(packPath, null, mountName);

    /// <summary>
    /// Opens a pack file with an optional key file and creates a mount for it.
    /// </summary>
    /// <param name="packPath">The pack path relative to the configured content root.</param>
    /// <param name="keyPath">The key path relative to the configured content root, or <see langword="null"/> to use automatic key discovery.</param>
    /// <param name="mountName">An optional display name for the mount.</param>
    /// <returns>The created pack mount.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="packPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the pack or explicitly requested key file cannot be found.</exception>
    /// <remarks>
    /// The returned mount is not added to the asset search order automatically.
    /// When <paramref name="keyPath"/> is null or empty, VOID looks for a key file
    /// beside the pack with the same base name and a <c>.key</c> extension.
    /// </remarks>
    public PackMount LoadPack(string packPath, string keyPath, string mountName = null)
    {
        if (string.IsNullOrEmpty(packPath))
            throw new ArgumentException("Pack path cannot be null or empty", nameof(packPath));

        string fullPackPath = GetFullPath(packPath);

        if (!File.Exists(fullPackPath))
            throw new FileNotFoundException($"Pack file not found: {fullPackPath}");

        Logger.Instance.InfoWithCategory("AssetManager", "Loading pack: {0} (key: {1})", packPath, keyPath ?? "none");

        byte[] key = null;
        if (!string.IsNullOrEmpty(keyPath))
        {
            string fullKeyPath = GetFullPath(keyPath);
            if (!File.Exists(fullKeyPath))
                throw new FileNotFoundException($"Key file not found: {fullKeyPath}");

            key = File.ReadAllBytes(fullKeyPath);
        }
        else
        {
            string autoKeyPath = Path.ChangeExtension(fullPackPath, ".key");
            if (File.Exists(autoKeyPath))
            {
                key = File.ReadAllBytes(autoKeyPath);
            }
        }

        Logger.Instance.InfoWithCategory("AssetManager", "Pack loaded: {0}", mountName ?? Path.GetFileNameWithoutExtension(packPath));

        return new PackMount(fullPackPath, key, mountName ?? Path.GetFileNameWithoutExtension(packPath));
    }

    /// <summary>
    /// Opens every <c>.pack</c> file in the top level of a content directory.
    /// </summary>
    /// <param name="directoryPath">The directory path relative to the configured content root.</param>
    /// <returns>The pack mounts that were opened successfully.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directoryPath"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    /// <remarks>
    /// A companion <c>.key</c> file is used when present. Individual pack failures
    /// are logged and skipped. Returned mounts are not added to the asset search
    /// order automatically.
    /// </remarks>
    public List<PackMount> LoadAllPacks(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

        string fullDirPath = GetFullPath(directoryPath);

        if (!Directory.Exists(fullDirPath))
            throw new DirectoryNotFoundException($"Directory not found: {fullDirPath}");

        var packs = new List<PackMount>();

        var packFiles = Directory.GetFiles(fullDirPath, "*.pack", SearchOption.TopDirectoryOnly);

        foreach (var packFile in packFiles)
        {
            try
            {
                string keyFile = Path.ChangeExtension(packFile, ".key");
                string keyPath = File.Exists(keyFile) ? keyFile : null;

                var mount = LoadPack(
                    Path.GetRelativePath(GameSettings.Instance.AppContentRoot, packFile),
                    keyPath != null ? Path.GetRelativePath(GameSettings.Instance.AppContentRoot, keyPath) : null,
                    Path.GetFileNameWithoutExtension(packFile)
                );

                packs.Add(mount);
            }
            catch (Exception ex)
            {
                Logger.Instance.WarningWithCategory("AssetManager", "Failed to load pack '{0}': {1}", packFile, ex.Message);
            }
        }

        return packs;
    }

    /// <summary>
    /// Removes a pack mount from the search order and disposes it.
    /// </summary>
    /// <param name="mount">The pack mount to unload. A null value is ignored.</param>
    public void UnloadPack(PackMount mount)
    {
        if (mount == null)
            return;

        RemoveMount(mount);

        mount.Dispose();
    }

    /// <summary>
    /// Removes and disposes every active <see cref="PackMount"/>.
    /// </summary>
    public void UnloadAllPacks()
    {
        var packs = _mounts.OfType<PackMount>().ToList();

        foreach (var pack in packs)
        {
            RemoveMount(pack);
            pack.Dispose();
        }
    }
    #endregion

    #region GetOrLoad
    /// <summary>
    /// Loads or retrieves a cached asset from the virtual content system.
    /// </summary>
    /// <typeparam name="T">The registered asset type to load.</typeparam>
    /// <param name="path">The virtual asset path.</param>
    /// <returns>The loaded or cached asset.</returns>
    public T Load<T>(string path) where T : IAsset => GetOrLoadInternal<T>(path, null);

    /// <summary>
    /// Attempts to load or retrieve a cached asset without propagating load errors.
    /// </summary>
    /// <typeparam name="T">The registered asset type to load.</typeparam>
    /// <param name="path">The virtual asset path.</param>
    /// <param name="asset">Receives the loaded asset when the operation succeeds.</param>
    /// <returns><see langword="true"/> when an asset was returned; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Load failures are written to the engine log and returned as
    /// <see langword="false"/> instead of being thrown to the caller.
    /// </remarks>
    public bool TryLoad<T>(string path, out T asset) where T : IAsset
    {
        try
        {
            asset = GetOrLoadInternal<T>(path, null);
            return asset != null;
        }
        catch (Exception ex)
        {
            Logger.Instance.WarningWithCategory("AssetManager",
                "Failed to load asset '{0}' of type '{1}': {2}", path, typeof(T).Name, ex.Message);
            asset = default!;
            return false;
        }
    }

    /// <summary>
    /// Loads or retrieves a cached texture with the requested sampling behavior.
    /// </summary>
    /// <param name="path">The virtual texture path.</param>
    /// <param name="repeat">Whether texture coordinates outside the texture bounds should repeat.</param>
    /// <param name="smoothing">Whether texture filtering should use smoothing.</param>
    /// <returns>The loaded or cached texture.</returns>
    public Texture LoadTexture(string path, bool repeat, bool smoothing)
        => GetOrLoadInternal(path, (id, data, tag) => new Texture(id, data, tag, repeat, smoothing));

    /// <summary>
    /// Loads or retrieves a cached sprite font with the requested font settings.
    /// </summary>
    /// <param name="path">The virtual font image path.</param>
    /// <param name="spacing">Additional horizontal glyph spacing.</param>
    /// <param name="lineSpacing">Additional vertical spacing between lines.</param>
    /// <param name="charset">The character sequence represented by the font image.</param>
    /// <returns>The loaded or cached sprite font.</returns>
    public SpriteFont LoadSpriteFont(string path, float spacing = 0f, float lineSpacing = 0f, string charset = SpriteFont.CharsetFull)
        => GetOrLoadInternal(path, (id, data, tag) => new SpriteFont(id, data, tag, charset, lineSpacing, spacing));

    /// <summary>
    /// Loads or retrieves a cached sound with the requested playback priority.
    /// </summary>
    /// <param name="path">The virtual sound path.</param>
    /// <param name="priority">The priority assigned to the sound.</param>
    /// <returns>The loaded or cached sound.</returns>
    public Sound LoadSound(string path, SoundPriority priority = SoundPriority.Normal)
        => GetOrLoadInternal(path, (id, data, tag) => new Sound(id, data, tag, priority));

    /// <summary>
    /// Loads the texture referenced by an LDtk tileset.
    /// </summary>
    /// <param name="map">The LDtk map that owns the tileset definition.</param>
    /// <param name="tilesetId">The LDtk tileset identifier.</param>
    /// <returns>The loaded tileset texture.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="tilesetId"/> represents no assigned tileset.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the map does not contain the requested tileset.</exception>
    public Texture LoadTilesetTexture(LDtkMap map, uint tilesetId)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (tilesetId == uint.MaxValue)
            throw new InvalidOperationException("Tileset ID is invalid (no tileset assigned).");
        if (!map.TryGetTilesetById(tilesetId, out var tileset))
            throw new KeyNotFoundException($"Tileset with ID {tilesetId} was not found in LDTK project");

        var formatted = FileHelper.RemapLDTKPath(tileset.Path, GameSettings.Instance.AppContentRoot);
        var wanted = FileHelper.Normalize(formatted);

        return Load<Texture>(wanted);
    }

    /// <summary>
    /// Attempts to load the texture referenced by an LDtk tileset.
    /// </summary>
    /// <param name="map">The LDtk map that owns the tileset definition.</param>
    /// <param name="tilesetId">The LDtk tileset identifier.</param>
    /// <param name="texture">Receives the loaded texture when the operation succeeds.</param>
    /// <returns><see langword="true"/> when the tileset texture was loaded; otherwise, <see langword="false"/>.</returns>
    public bool TryLoadTilesetTexture(LDtkMap map, uint tilesetId, out Texture texture)
    {
        try
        {
            texture = LoadTilesetTexture(map, tilesetId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.WarningWithCategory("AssetManager",
                "Failed to load tileset texture for tileset ID {0}: {1}", tilesetId, ex.Message);

            texture = null!;
            return false;
        }
    }
    #endregion

    #region Register Custom Assets
    /// <summary>
    /// Registers a custom asset type and the file extensions used to load it.
    /// </summary>
    /// <typeparam name="T">The asset type to register.</typeparam>
    /// <param name="extensions">Supported extensions, including the leading period, such as <c>.map</c>.</param>
    /// <param name="factory">A factory that creates the asset from its assigned ID, source bytes, and normalized tag.</param>
    /// <exception cref="ArgumentException">Thrown when no extensions are supplied.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the asset type is already registered.</exception>
    /// <remarks>
    /// Registration applies to subsequent generic <see cref="Load{T}"/> requests
    /// for the custom type.
    /// </remarks>
    public void RegisterAssetType<T>(string[] extensions, Func<uint, byte[], string, T> factory) where T : IAsset
    {
        if (extensions == null || extensions.Length == 0)
            throw new ArgumentException("At least one extension required", nameof(extensions));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        Logger.Instance.InfoWithCategory("AssetManager", "Registering asset type: {0} with extensions: {1}",
            typeof(T).Name, string.Join(", ", extensions));

        var type = typeof(T);

        if (SupportedExtensions.ContainsKey(type))
            throw new InvalidOperationException($"Asset type '{type.Name}' is already registered.");

        SupportedExtensions[type] = extensions;
        SupportedLoaders[type] = (id, data, tag) => factory(id, data, tag);
    }

    /// <summary>
    /// Determines whether an asset type has a registered loader and extension set.
    /// </summary>
    /// <typeparam name="T">The asset type to check.</typeparam>
    /// <returns><see langword="true"/> when the type is registered; otherwise, <see langword="false"/>.</returns>
    public bool IsAssetTypeRegistered<T>()
        => SupportedExtensions.ContainsKey(typeof(T));

    /// <summary>
    /// Removes a previously registered custom asset type.
    /// </summary>
    /// <typeparam name="T">The custom asset type to unregister.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown when the type is built into VOID or is not currently registered.</exception>
    public void UnregisterAssetType<T>()
    {
        var type = typeof(T);

        if (EngineAssetTypes.Contains(type))
            throw new InvalidOperationException($"Cannot unregister engine asset type '{type.Name}'.");

        if (!SupportedExtensions.ContainsKey(type))
            throw new InvalidOperationException($"Asset type '{type.Name}' is not registered.");

        SupportedExtensions.Remove(type);
        SupportedLoaders.Remove(type);
    }
    #endregion

    #region Private Methods
    private T GetOrLoadInternal<T>(string path, Func<uint, byte[], string, T> customLoader) where T : IAsset
    {
        var normalizedPath = NormalizedPath(path);

        if (!IsValidExtention(normalizedPath, typeof(T)))
            throw new FileNotFoundException(
                $"Asset '{normalizedPath}' has an unsupported extention for type '{typeof(T).Name}'. " +
                $"Supported extentions: {string.Join(", ", SupportedExtensions[typeof(T)])}"
            );

        var hash = HashHelper.Cache64(path);
        if (_assets.TryGetValue(hash, out var existingAsset))
        {
            Logger.Instance.DebugWithCategory("AssetManager", "Cache hit: {0} (hash: {1})", normalizedPath, hash);

            existingAsset.Load();
            EvictOneExpiredAsset();
            return (T)existingAsset;
        }

        byte[] assetData = null!;
        string foundInMount = null!;

        foreach (var mount in _mounts)
        {
            if (mount.HasFile(normalizedPath))
            {
                try
                {
                    assetData = mount.ReadFile(normalizedPath);
                    foundInMount = mount.GetType().Name;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Instance.WarningWithCategory("AssetManager", "Mount '{0}' reported file '{1}' but failed to read: {2}",
                        mount.GetType().Name, normalizedPath, ex.Message);
                    continue;
                }
            }
        }

        if (assetData == null)
        {
            throw new FileNotFoundException(
                $"Asset '{normalizedPath}' of type '{typeof(T).Name}' was not found in any move. " +
                $"Searched: {_mounts.Count} mount(s): {string.Join(", ", _mounts.Select(x => x.GetType().Name))}"
            );
        }

        T newAsset;

        try
        {
            if (customLoader != null)
                newAsset = customLoader(GetNextId(), assetData, normalizedPath);
            else if (SupportedLoaders.TryGetValue(typeof(T), out var defaultLoader))
                newAsset = (T)defaultLoader(GetNextId(), assetData, normalizedPath);
            else
            {
                throw new InvalidOperationException(
                    $"No loader found for asset type '{typeof(T).Name}' " +
                    $"Registered types: {string.Join(", ", SupportedLoaders.Keys.Select(t => t.Name))}"
                );
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create asset '{normalizedPath}' of type '{typeof(T).Name}' from mount '{foundInMount}'. " +
                $"Data size: {assetData.Length} bytes. Error: {ex.Message}", ex
            );
        }

        _assets.TryAdd(hash, newAsset);
        newAsset.Load();

        Logger.Instance.DebugWithCategory("AssetManager", "Loaded asset: {0} ({1} bytes from {2})",
            normalizedPath, assetData.Length, foundInMount);

        EvictOneExpiredAsset();

        return newAsset;
    }

    private void EvictOneExpiredAsset()
    {
        var evictionMinutes = GameSettings.Instance.AssetEvictionMinutes;
        if (evictionMinutes <= 0) return;

        if ((DateTime.Now - _lastEvictionCheck).TotalMinutes < GameSettings.Instance.AssetCheckIntervalMinutes)
            return;

        _lastEvictionCheck = DateTime.Now;

        var now = DateTime.Now;
        var threshold = TimeSpan.FromMinutes(evictionMinutes);

        foreach (var (k, v) in _assets)
        {
            if (!v.IsValid) continue;

            if ((now - v.LastAccessTime) > threshold)
            {
                Logger.Instance.DebugWithCategory("AssetManager", "Evicted asset: {0} (idle for {1} minutes)",
                    v.Tag, evictionMinutes);

                v.Unload();
                break;
            }
        }
    }

    private string NormalizedPath(string path)
    {
        if (path.IsEmpty())
            return "";

        path = path.Replace('\\', '/');
        path = path.Replace("..", "");

        while (path.Contains("//"))
            path = path.Replace("//", "/");

        if (path.StartsWith('/'))
            path = path[1..];

        return path;
    }

    private bool IsValidExtention(string path, Type assetType)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (SupportedExtensions.TryGetValue(assetType, out var extentions))
            return extentions.Contains(ext);

        return false;
    }

    private string GetFullPath(string virtualPath)
    {
        var contentRoot = GameSettings.Instance.AppContentRoot;

        if (!contentRoot.EndsWith('/') && !contentRoot.EndsWith('\\'))
            contentRoot += Path.AltDirectorySeparatorChar;

        var fullPath = Path.GetFullPath(Path.Combine(contentRoot, virtualPath));

        if (!fullPath.StartsWith(Path.GetFullPath(contentRoot)))
            throw new UnauthorizedAccessException($"Cannot access file outside of ContentRoot: {virtualPath}");

        return fullPath;
    }
    #endregion

    #region Internal Methods
    internal bool TryGetAsset<T>(string tag, out T asset) where T : IAsset
    {
        var hash = HashHelper.Cache64(tag);
        if (_assets.TryGetValue(hash, out var a))
        {
            asset = (T)a;
            return true;
        }
        asset = default;
        return false;
    }

    internal T LoadFromData<T>(byte[] data, string tag) where T : IAsset
    {
        if (data == null || data.Length == 0)
            throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(tag))
            throw new ArgumentNullException(nameof(tag));

        var hash = HashHelper.Cache64(tag);

        if (_assets.TryGetValue(hash, out var existing))
            return (T)existing;
        if (!SupportedLoaders.TryGetValue(typeof(T), out var loader))
            throw new InvalidOperationException($"No loader found for asset type '{typeof(T).Name}'");

        var asset = (T)loader(GetNextId(), data, tag);

        if (asset is SpriteFont font)
        {
            font.LineSpacing = 2;
            font.Spacing = 1;
        }

        asset.Load();

        _assets.TryAdd(hash, asset);

        Logger.Instance.DebugWithCategory("AssetManager", "Loaded asset from data: {0} ({1} bytes)", tag, data.Length);

        return asset;
    }

    internal static uint GetNextId()
    {
        lock (IdLock)
        {
            return ++s_id;
        }
    }

    internal void Clear()
    {
        Logger.Instance.InfoWithCategory("AssetManager",
            "Clearing {0} assets and {1} mounts", _assets.Count, _mounts.Count);

        foreach (var asset in _assets.Values)
            asset.Dispose();
        _assets.Clear();

        foreach (var mount in _mounts.OfType<IDisposable>())
            mount.Dispose();
        _mounts.Clear();
    }
    #endregion
}
