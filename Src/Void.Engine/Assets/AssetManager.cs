// ============================================================================
//  AssetManager.cs
// ============================================================================
//  Core asset management system with caching, mounting, pack loading,
//  and custom asset type registration.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets;

/// <summary>
/// Core asset management system with caching, mounting, pack loading,
/// and custom asset type registration.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AssetManager"/> provides a unified system for loading,
/// caching, and managing assets of various types. It supports:
/// <list type="bullet">
///   <item><description>Automatic asset caching with eviction based on idle time</description></item>
///   <item><description>Multiple mount points for flexible asset sources</description></item>
///   <item><description>Pack loading for encrypted/compressed asset archives</description></item>
///   <item><description>Custom asset type registration</description></item>
///   <item><description>Thread-safe concurrent caching</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Asset Loading Flow:</b>
/// <list type="number">
///   <item><description>Request an asset via <see cref="Load{T}"/> with a virtual path</description></item>
///   <item><description>AssetManager checks the cache by path hash</description></item>
///   <item><description>If found, it returns the cached asset (auto-reloads if unloaded)</description></item>
///   <item><description>If not found, it searches mounts in priority order</description></item>
///   <item><description>The first mount that has the file reads the data</description></item>
///   <item><description>A new asset instance is created using the appropriate loader</description></item>
///   <item><description>The asset is cached and returned</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Mount System:</b>
/// Mounts are virtual file systems that provide access to assets. Mounts are
/// searched in the order they were added (or inserted), with the first mount
/// that contains the file providing the asset data. The following built-in
/// mount types are available:
/// <list type="bullet">
///   <item><description><see cref="VirtualFileSystemMount"/> - Direct file system access to the content root</description></item>
///   <item><description><see cref="MacOsMount"/> - macOS application bundle resource access</description></item>
///   <item><description><see cref="PackMount"/> - Encrypted and/or compressed asset pack archives</description></item>
///   <item><description><see cref="MacOsPackMount"/> - macOS-specific pack mount for bundle resources</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Pack System:</b>
/// Packs are encrypted and/or compressed archives that contain assets. They
/// can be loaded with an optional key file for encryption. Packs act as
/// read-only mounts that provide fast, secure asset delivery.
/// </para>
/// <para>
/// <b>Asset Eviction:</b>
/// Assets are automatically unloaded after a configurable idle time
/// (<see cref="GameSettings.AssetEvictionMinutes"/>). This helps manage memory
/// usage by removing assets that haven't been accessed recently.
/// </para>
/// <para>
/// <b>Custom Asset Types:</b>
/// New asset types can be registered using <see cref="RegisterAssetType{T}"/>
/// with their supported file extensions and a factory function. This allows
/// the engine to be extended with custom asset types.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe and uses concurrent collections for cache management.
/// </para>
/// </remarks>
public sealed class AssetManager
{
    #region fields
    private static uint s_id;
    private static ushort s_accessCounter = 0;
    private static ushort s_evictionCheckCounter = 0;
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
    /// Gets the singleton instance of the asset manager.
    /// </summary>
    public static AssetManager Instance => _instance.Value;

    /// <summary>
    /// Gets the list of active mounts in priority order.
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
    /// Adds a mount to the beginning of the search order (highest priority).
    /// </summary>
    public void AddMountToStart(IMount mount) => _mounts.Insert(0, mount);

    /// <summary>
    /// Adds a mount to the end of the search order (lowest priority).
    /// </summary>
    public void AddMountToEnd(IMount mount) => _mounts.Add(mount);

    /// <summary>
    /// Inserts a mount at the specified index in the search order.
    /// </summary>
    public void InsertMount(int index, IMount mount) => _mounts.Insert(index, mount);

    /// <summary>
    /// Removes a mount from the search order.
    /// </summary>
    public void RemoveMount(IMount mount) => _mounts.Remove(mount);

    /// <summary>
    /// Clears all mounts and re-adds the default mounts.
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
    /// Loads a pack file as a mount.
    /// </summary>
    /// <param name="packPath">The virtual path to the pack file.</param>
    /// <param name="mountName">The name of the mount (optional).</param>
    /// <returns>The loaded pack mount.</returns>
    public PackMount LoadPack(string packPath, string mountName = null)
        => LoadPack(packPath, null, mountName);

    /// <summary>
    /// Loads a pack file as a mount with a key file for encryption.
    /// </summary>
    /// <param name="packPath">The virtual path to the pack file.</param>
    /// <param name="keyPath">The virtual path to the key file.</param>
    /// <param name="mountName">The name of the mount (optional).</param>
    /// <returns>The loaded pack mount.</returns>
    public PackMount LoadPack(string packPath, string keyPath, string mountName = null)
    {
        if (string.IsNullOrEmpty(packPath))
            throw new ArgumentException("Pack path cannot be null or empty", nameof(packPath));

        string fullPackPath = GetFullPath(packPath);

        if (!File.Exists(fullPackPath))
            throw new FileNotFoundException($"Pack file not found: {fullPackPath}");

        Logger.Instance.InfoWithCategory("AssetManager", "Loading pack: {0} (key: {1})", packPath, keyPath ?? "none");

        byte[] packData = File.ReadAllBytes(fullPackPath);
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

        return LoadPackData(packData, key, mountName ?? Path.GetFileNameWithoutExtension(packPath));
    }

    /// <summary>
    /// Loads a pack from raw data bytes.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <param name="mountName">The name of the mount (optional).</param>
    /// <returns>The loaded pack mount.</returns>
    public PackMount LoadPackData(byte[] packData, byte[] key = null, string mountName = null)
    {
        if (packData == null || packData.Length == 0)
            throw new ArgumentException("Pack data cannot be null or empty", nameof(packData));

        Logger.Instance.DebugWithCategory("AssetManager",
            "Loading pack data: {0} bytes, name: {1}", packData.Length, mountName ?? "Pack Mount");

        var mount = new PackMount(packData, key, mountName ?? "Pack Mount");

        AddMountToEnd(mount);

        return mount;
    }

    /// <summary>
    /// Loads all pack files found in a directory.
    /// </summary>
    /// <param name="directoryPath">The virtual path to the directory.</param>
    /// <returns>A list of loaded pack mounts.</returns>
    public List<PackMount> LoadAllPacks(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));

        string fullDirPath = GetFullPath(directoryPath);

        if (!Directory.Exists(fullDirPath))
            throw new DirectoryNotFoundException($"Directory not found: {fullDirPath}");

        var mounted = new List<PackMount>();

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

                mounted.Add(mount);
            }
            catch (Exception ex)
            {
                Logger.Instance.WarningWithCategory("AssetManager", "Failed to load pack '{0}': {1}", packFile, ex.Message);
            }
        }

        return mounted;
    }

    /// <summary>
    /// Unloads a pack mount and removes it from the search order.
    /// </summary>
    public void UnloadPack(PackMount mount)
    {
        if (mount == null)
            return;

        RemoveMount(mount);

        mount.Dispose();
    }

    /// <summary>
    /// Unloads all pack mounts.
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
    /// Loads an asset of the specified type from the virtual path.
    /// </summary>
    /// <typeparam name="T">The asset type to load.</typeparam>
    /// <param name="path">The virtual path to the asset.</param>
    /// <returns>The loaded asset.</returns>
    public T Load<T>(string path) where T : IAsset => GetOrLoadInternal<T>(path, null);

    /// <summary>
    /// Attempts to load an asset of the specified type from the virtual path.
    /// </summary>
    /// <typeparam name="T">The asset type to load.</typeparam>
    /// <param name="path">The virtual path to the asset.</param>
    /// <param name="asset">When this method returns, contains the loaded asset, or default if loading failed.</param>
    /// <returns><see langword="true"/> if the asset was loaded successfully; otherwise, <see langword="false"/>.</returns>
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
    /// Loads a texture with specific repeat and smoothing settings.
    /// </summary>
    public Texture LoadTexture(string path, bool repeat, bool smoothing)
        => GetOrLoadInternal(path, (id, data, tag) => new Texture(id, data, tag, repeat, smoothing));

    /// <summary>
    /// Loads a sprite font with optional character set, spacing, and line spacing.
    /// </summary>
    public SpriteFont LoadSpriteFont(string path, float spacing = 0f, float lineSpacing = 0f, string charset = SpriteFont.CharsetFull)
        => GetOrLoadInternal(path, (id, data, tag) => new SpriteFont(id, data, tag, charset, lineSpacing, spacing));

    /// <summary>
    /// Loads a sound with the specified priority.
    /// </summary>
    public Sound LoadSound(string path, SoundPriority priority = SoundPriority.Normal)
        => GetOrLoadInternal(path, (id, data, tag) => new Sound(id, data, tag, priority));

    /// <summary>
    /// Loads a tileset texture from an LDtk map.
    /// </summary>
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
    /// Attempts to load a tileset texture from an LDtk map.
    /// </summary>
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
    /// Registers a custom asset type with its supported extensions and factory.
    /// </summary>
    /// <typeparam name="T">The asset type to register.</typeparam>
    /// <param name="extensions">The supported file extensions.</param>
    /// <param name="factory">The factory function that creates the asset.</param>
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
    /// Determines whether an asset type is registered.
    /// </summary>
    public bool IsAssetTypeRegistered<T>()
        => SupportedExtensions.ContainsKey(typeof(T));

    /// <summary>
    /// Unregisters a custom asset type.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when trying to unregister an engine asset type or a type that is not registered.</exception>
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
            // EvictOneExpiredAsset();
            Touch(existingAsset);
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
        Touch(newAsset);

        Logger.Instance.DebugWithCategory("AssetManager", "Loaded asset: {0} ({1} bytes from {2})",
            normalizedPath, assetData.Length, foundInMount);

        // EvictOneExpiredAsset();

        return newAsset;
    }

    private void EvictOneExpiredAsset()
    {
        ushort threshold = GameSettings.Instance.AssetStalenessThreshold;
        if (threshold == 0) return;

        foreach (var (k, v) in _assets)
        {
            ushort age = CalculateAge(s_accessCounter, v.LastAccessTick);

            if (age > threshold)
            {
                Logger.Instance.DebugWithCategory("AssetManager", "Evicted asset: {0} ({1} since llast used)", v.Tag, age);

                v.Unload();
                break;
            }
        }
    }

    private static ushort CalculateAge(ushort currentTick, ushort lastAccessTick)
    {
        if (currentTick >= lastAccessTick)
            return (ushort)(currentTick - lastAccessTick);
        else
            return (ushort)(ushort.MaxValue - lastAccessTick + currentTick + 1);
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
    internal void Touch(IAsset asset)
    {
        s_accessCounter++;
        asset.LastAccessTick = s_accessCounter;

        s_evictionCheckCounter++;

        // Check eviction periodically
        if (s_evictionCheckCounter >= GameSettings.Instance.AssetEvictionCheckInterval)
        {
            s_evictionCheckCounter = 0;
            EvictOneExpiredAsset();
        }
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

        s_accessCounter = 0;
        s_evictionCheckCounter = 0;
    }
    #endregion
}