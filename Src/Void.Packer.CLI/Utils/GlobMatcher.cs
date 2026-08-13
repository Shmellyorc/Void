namespace Void.Packer.CLI.Utils;

public static class GlobMatcher
{
    public static bool Match(string path, string pattern)
    {
        var glob = Glob.Parse(pattern);

        return glob.IsMatch(path);
    }
}