namespace Void.Engine.Assets.Mounts;

public sealed class MacOsPackMount : IMount, IDisposable
{
    private readonly PackMount _packMount;
    private readonly string _resourcePath;
    private bool _disposed;

    public string Name => _packMount?.Name ?? "MacOs Pack Mount";

    public MacOsPackMount(string packFileName, byte[] key = null)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsPackMount is only supported on MacOS");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        string resourcePath;
        if (bundlePath.Contains(".app/Contents/MacOs"))
        {
            resourcePath = bundlePath.Replace("MacOs", "Resources");
        }
        else
        {
            // On Development:
            resourcePath = GameSettings.Instance.AppContentRoot;
        }

        if (!Directory.Exists(resourcePath))
            resourcePath = GameSettings.Instance.AppContentRoot;

        _resourcePath = resourcePath;

        string packPath = Path.Combine(_resourcePath, packFileName);

        if (!File.Exists(packPath))
            throw new FileNotFoundException($"Pack file not found: {packPath}");

        byte[] packData = File.ReadAllBytes(packPath);

        if (key == null)
        {
            string keyPath = Path.ChangeExtension(packPath, ".key");
            if (File.Exists(keyPath))
            {
                key = File.ReadAllBytes(keyPath);
            }
        }

        _packMount = new PackMount(packData, key, Path.GetFileNameWithoutExtension(packFileName));
    }

    public bool HasFile(string virtualPath)
        => _packMount.HasFile(virtualPath);

    public byte[] ReadFile(string virtualPath)
        => _packMount.ReadFile(virtualPath);

    public bool VerifyIntegrity()
        => _packMount.VerifyIntegrity();

    public IEnumerable<string> ListFiles()
        => _packMount.ListFiles();

    public void Dispose()
    {
        if (!_disposed)
        {
            _packMount?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}