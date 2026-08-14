namespace Void.Engine.Assets;

public sealed class AssetManager
{
    #region fields
    private static uint s_id;
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
        ]}
    };

    private static readonly Dictionary<Type, Func<uint, byte[], string, IAsset>> SupportedLoaders = new()
    {
        {typeof(Texture), (id, data, tag) => new Texture(id, data, tag, false, false)},
        {typeof(LDtkMap), (id, data, tag) => new LDtkMap(id, data, tag)},
        {typeof(SpriteFont), (id, data, tag) => new SpriteFont(id, data, tag, SpriteFont.CharsetFull)},
        {typeof(Spritesheet), (id, data, tag) => new Spritesheet(id, data, tag)},
        {typeof(Sound), (id, data, tag) => new Sound(id, data, tag)}
    };
    #endregion



    #region Properties
    public static AssetManager Instance { get; private set; }
    public IReadOnlyList<IMount> Mounts => _mounts;
    #endregion



    #region Constructor
    internal AssetManager()
    {
        Instance ??= this;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _mounts.Add(new MacOsMount());

        _mounts.Add(new VirtualFileSystemMount());
    }
    #endregion



    #region Mounts
    public void AddMountToStart(IMount mount) => _mounts.Insert(0, mount);

    public void AddMountToEnd(IMount mount) => _mounts.Add(mount);

    public void InsertMount(int index, IMount mount) => _mounts.Insert(index, mount);

    public void RemoveMount(IMount mount) => _mounts.Remove(mount);

    public void ClearMounts()
    {
        _mounts.Clear();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _mounts.Add(new MacOsMount());

        _mounts.Add(new VirtualFileSystemMount());
    }
    #endregion



    #region Pack Mounts
    public PackMount LoadPack(string packPath, string mountName = null)
        => LoadPack(packPath, null, mountName);

    public PackMount LoadPack(string packPath, string keyPath, string mountName = null)
    {
        if (string.IsNullOrEmpty(packPath))
            throw new ArgumentException("Pack path cannot be null or empty", nameof(packPath));

        string fullPackPath = GetFullPath(packPath);

        if (!File.Exists(fullPackPath))
            throw new FileNotFoundException($"Pack file not found: {fullPackPath}");

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

        return LoadPackData(packData, key, mountName ?? Path.GetFileNameWithoutExtension(packPath));
    }

    public PackMount LoadPackData(byte[] packData, byte[] key = null, string mountName = null)
    {
        if (packData == null || packData.Length == 0)
            throw new ArgumentException("Pack data cannot be null or empty", nameof(packData));

        var mount = new PackMount(packData, key, mountName ?? "Pack Mount");

        AddMountToEnd(mount);

        return mount;
    }

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
                Console.WriteLine($"[AssetManager] Failed to load pack '{packFile}': {ex.Message}");
            }
        }

        return mounted;
    }

    public void UnloadPack(PackMount mount)
    {
        if (mount == null)
            return;

        RemoveMount(mount);

        mount.Dispose();
    }

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
    public T Load<T>(string path) where T : IAsset => GetOrLoadInternal<T>(path, null);

    public bool TryLoad<T>(string path, out T asset) where T : IAsset
    {
        try
        {
            asset = GetOrLoadInternal<T>(path, null);
            return asset != null;
        }
        catch
        {
            asset = default;
            return false;
        }
    }

    public Texture LoadTexture(string path, bool repeat, bool smoothing)
        => GetOrLoadInternal(path, (id, data, tag) => new Texture(id, data, tag, repeat, smoothing));

    public SpriteFont LoadSpriteFont(string path, float spacing = 0f, float lineSpacing = 0f, string charset = SpriteFont.CharsetFull)
        => GetOrLoadInternal(path, (id, data, tag) => new SpriteFont(id, data, tag, charset, lineSpacing, spacing));

    #endregion



    #region Register Custom Assets
    public static void RegisterAssetType<T>(string[] extensions, Func<uint, byte[], string, T> factory) where T : IAsset
    {
        if (extensions == null || extensions.Length == 0)
            throw new ArgumentException("At least one extension required", nameof(extensions));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var type = typeof(T);

        if (SupportedExtensions.ContainsKey(type))
            throw new InvalidOperationException($"Asset type '{type.Name}' is already registered.");

        SupportedExtensions[type] = extensions;
        SupportedLoaders[type] = (id, data, tag) => factory(id, data, tag);
    }

    public static bool IsAssetTypeRegistered<T>()
        => SupportedExtensions.ContainsKey(typeof(T));

    public static void UnregisterAssetType<T>()
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
            existingAsset.Load();
            EvictOneExpiredAsset();
            return (T)existingAsset;
        }

        // Find it:
        byte[] assetData = null;
        string foundInMount = null;

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
                    System.Console.WriteLine(
                        $"[AssetManager] Mount '{mount.GetType().Name}' reported file '{normalizedPath}' but failed to read: {ex.Message}"
                    );
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

        // store:
        _assets.TryAdd(hash, newAsset);
        newAsset.Load();

        EvictOneExpiredAsset();

        return newAsset;
    }

    private void EvictOneExpiredAsset()
    {
        var evictionMinutes = GameSettings.Instance.AssetEvictionMinutes;
        if (evictionMinutes <= 0) return;

        foreach (var (k, v) in _assets)
        {

            if ((DateTime.Now - v.LastAccessTime) > TimeSpan.FromMinutes(evictionMinutes))
            {
                v.Unload();
                break; // only do one at a time, so it doesnt spike and/or lag
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

        // Security check:
        if (!fullPath.StartsWith(Path.GetFullPath(contentRoot)))
            throw new UnauthorizedAccessException($"Cannot access file outside of ContentRoot: {virtualPath}");

        return fullPath;
    }

    #endregion



    #region Internal Methods
    internal static uint GetNextId()
    {
        lock (IdLock)
        {
            return ++s_id;
        }
    }

    internal void Clear()
    {
        foreach (var asset in _assets.Values)
        {
            asset.Dispose();
        }
        _assets.Clear();

        foreach (var mount in _mounts.OfType<IDisposable>())
            mount.Dispose();
        _mounts.Clear();
    }
    #endregion
}
