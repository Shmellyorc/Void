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

    private static byte[] UintToBytes(uint value)
    {
        return new byte[] {
        (byte)(value & 0xFF),
        (byte)((value >> 8) & 0xFF),
        (byte)((value >> 16) & 0xFF),
        (byte)((value >> 24) & 0xFF)
    };
    }
}
