namespace Void.Packer.CLI.Services;

public class PackService
{
    public PackResult Build(IEnumerable<PackFile> files, PackOptions options)
    {
        return Packer.Pack(files, options);
    }

    public UnpackResult Extract(byte[] packData, byte[] key = null)
    {
        return Packer.Unpack(packData, key);
    }

    public bool Verify(byte[] packData, byte[] key = null)
    {
        return Packer.Verify(packData, key);
    }

    public List<string> ListFiles(byte[] packData, byte[] key = null)
    {
        return Packer.ListFiles(packData, key);
    }

    public UpdateResult Update(
        byte[] existingPackData,
        IEnumerable<PackFile> filesToAdd,
        IEnumerable<string> filesToRemove = null,
        byte[] key = null,
        PackOptions options = null)
    {
        return Packer.Update(existingPackData, filesToAdd, filesToRemove, key, options);
    }

    public List<(string VirtualPath, string FullPath)> ScanFiles(
        string contentRoot,
        IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns)
    {
        var result = new List<(string, string)>();

        // Default include if none specified
        if (!includePatterns.Any())
            includePatterns = new[] { "**/*.*" };

        // Get all files
        var allFiles = Directory.GetFiles(contentRoot, "*", SearchOption.AllDirectories);

        foreach (var fullPath in allFiles)
        {
            string virtualPath = Path.GetRelativePath(contentRoot, fullPath)
                .Replace('\\', '/');

            // Check includes
            bool included = false;
            foreach (var pattern in includePatterns)
            {
                if (GlobMatcher.Match(virtualPath, pattern))
                {
                    included = true;
                    break;
                }
            }

            if (!included)
                continue;

            // Check excludes
            if (excludePatterns != null)
            {
                bool excluded = false;
                foreach (var pattern in excludePatterns)
                {
                    if (GlobMatcher.Match(virtualPath, pattern))
                    {
                        excluded = true;
                        break;
                    }
                }

                if (excluded)
                    continue;
            }

            result.Add((virtualPath, fullPath));
        }

        return result;
    }
}