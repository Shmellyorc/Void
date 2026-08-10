namespace Void.Engine.Assets;

public sealed class AssetManager
{
    internal static uint _id;

    private readonly Dictionary<uint, IAsset> _assets = [];
    private readonly List<IMount> _mounts = [];

    private static readonly Dictionary<Type, string[]> SupportedExtetnions = new()
    {
        {typeof(Texture), [".png", ".bmp", ".tga", ".jpg", ".gif", ".psd", ".hdr", ".pic", ".pnm"] },
        // LDtkMap
        // SpriteFont
        // BitmapFont
        // Spritesheet
        // Sound
    };

    private static readonly Dictionary<Type, Func<uint, byte[], string, IAsset>> DefaultLoaders = new()
    {
        {typeof(Texture), (id, data, tag) => new Texture(id, data, tag, false, false)}
        // LDtkMap
        // SpriteFont
        // BitmapFont
        // Spritesheet
        // Sound
    };

    public static AssetManager Instance { get; private set; }

    internal AssetManager()
    {
        Instance ??= this;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _mounts.Add(new MacOsMount());

        _mounts.Add(new VirtualFileSystemMount());
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




    #region Mounts
    public IReadOnlyList<IMount> Mounts => _mounts;

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







    #region GetOrLoad
    public T Load<T>(string path) where T : IAsset
        => GetOrLoadInternal<T>(path, null);

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

    #endregion



    #region Private Methods
    private T GetOrLoadInternal<T>(string path, Func<uint, byte[], string, T> customLoader) where T : IAsset
    {
        var normalizedPath = NormalizedPath(path);

        if (!IsValidExtention(normalizedPath, typeof(T)))
            throw new FileNotFoundException(
                $"Asset '{normalizedPath}' has an unsupported extention for type '{typeof(T).Name}'. " +
                $"Supported extentions: {string.Join(", ", SupportedExtetnions[typeof(T)])}"
            );

        // Check if asset exists in cache:
        var hash = HashHelper.Cache32(path.Intern());

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
                $"Asset '{normalizedPath}' of typeo '{typeof(T).Name}' was not found in any move. " +
                $"Searched: {_mounts.Count} mount(s): {string.Join(", ", _mounts.Select(x => x.GetType().Name))}"
            );
        }

        T newAsset;

        try
        {
            if (customLoader != null)
                newAsset = customLoader(_id++, assetData, normalizedPath);
            else if (DefaultLoaders.TryGetValue(typeof(T), out var defaultLoader))
                newAsset = (T)defaultLoader(_id++, assetData, normalizedPath);
            else
                throw new InvalidOperationException(
                    $"No loader found for asset type '{typeof(T).Name}' " +
                    $"Registered types: {string.Join(", ", DefaultLoaders.Keys.Select(t => t.Name))}"
                );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create asset '{normalizedPath}' of type '{typeof(T).Name}' from mount '{foundInMount}'. " +
                $"Data size: {assetData.Length} bytes. Error: {ex.Message}", ex
            );
        }

        // store:
        _assets[hash] = newAsset;
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

        if (SupportedExtetnions.TryGetValue(assetType, out var extentions))
            return extentions.Contains(ext);

        return false;
    }

    private string GetFullPAth(string virtualPath)
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
}
