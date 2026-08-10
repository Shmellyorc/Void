namespace Void.Engine.Assets.Mounts;

public sealed class VirtualFileSystemMount : IMount
{
    public string Name => "Virtual File System";

    public bool HasFile(string virtualPath)
    {
        string fullPath = GetFullPath(virtualPath);
        return File.Exists(fullPath);
    }

    public byte[] ReadFile(string virtualPath)
    {
        string fullPath = GetFullPath(virtualPath);
        return File.ReadAllBytes(fullPath);
    }

    private string GetFullPath(string virtualPath)
    {
        string contentRoot = GameSettings.Instance.AppContentRoot;

        if (!contentRoot.EndsWith('/') && !contentRoot.EndsWith('\\'))
            contentRoot += Path.DirectorySeparatorChar;

        string fullPath = Path.GetFullPath(Path.Combine(contentRoot, virtualPath));

        if (!fullPath.StartsWith(Path.GetFullPath(contentRoot)))
            throw new UnauthorizedAccessException($"Cannot access files outside of ContentRoot: {virtualPath}");

        return fullPath;
    }
}
