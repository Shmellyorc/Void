namespace Void.Engine.Assets.Mounts;

public sealed class MacOsMount : IMount
{
    private readonly string _resourcePath;

    public string Name => "MacOs Bundle";

    public MacOsMount()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MacOsMount is only supported on MacOs");

        string bundlePath = AppDomain.CurrentDomain.BaseDirectory;

        if (bundlePath.Contains(".app/Contents/MacOs"))
        {
            _resourcePath = bundlePath.Replace("MacOs", "Resources");
        }
        else
        {
            // On Development:
            _resourcePath = GameSettings.Instance.AppContentRoot;
        }

        if (!Directory.Exists(_resourcePath))
            _resourcePath = GameSettings.Instance.AppContentRoot;
    }

    public bool HasFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.Exists(fullPath);
    }

    public byte[] ReadFile(string virtualPath)
    {
        string fullPath = Path.Combine(_resourcePath, virtualPath);
        return File.ReadAllBytes(fullPath);
    }
}
