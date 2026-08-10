namespace Void.Packer.Utils;

public static class PathNormalizer
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        path = path.Replace('\\', '/');

        if (path.StartsWith('/'))
            path = path[1..];

        path = path.Replace("..", "");

        while (path.Contains("//"))
            path = path.Replace("//", "/");

        return path;
    }
}
