namespace Void.Packer;

public static class Packer
{
    public static PackResult Pack(IEnumerable<PackFile> files, PackOptions options = null)
    {
        options ??= new PackOptions();
        var fileList = files.ToList();

        if (fileList.Count == 0)
            throw new ArgumentException("No files to pack", nameof(files));

        var groups = SplitIntoGroups(fileList, options.MaxFilesPerPack);
        var result = new PackResult();

        foreach (var group in groups)
        {
            var builder = new SolidPackBuilder(options);
            builder.AddFiles(group);  // Fixed typo
            var container = builder.Build();

            result.Packs.Add(container);
            result.TotalFilesPacked += container.FileCount;
            result.TotalOriginalSize += container.OriginalSize;
            result.TotalPackedSize += container.PackedSize;

            // build map:
            foreach (var path in container.VirtualPaths)
            {
                result.FileToPackMap[path] = result.Packs.Count - 1;
            }
        }

        result.CompressionRatio = result.TotalFilesPacked > 0
            ? 1.0 - ((double)result.TotalPackedSize) / result.TotalOriginalSize
            : 0;

        return result;
    }

    public static UnpackResult Unpack(byte[] packData, byte[] key = null)
    {
        using var reader = new SolidPackReader(packData, key);

        var result = new UnpackResult();

        foreach (var path in reader.ListFiles())
        {
            var data = reader.ReadFile(path);
            
            result.Files.Add(new PackFile
            {
                VirtualPath = path,
                Data = data
            });
        }

        return result;
    }

    public static bool Verify(byte[] packData, byte[] key = null)
    {
        try
        {
            using var reader = new SolidPackReader(packData, key);
            return reader.VerifyIntegrity();
        }
        catch
        {
            return false;
        }
    }

    public static List<string> ListFiles(byte[] packData, byte[] key = null)
    {
        using var reader = new SolidPackReader(packData, key);
        return reader.ListFiles().ToList();
    }

    public static UpdateResult Update(
        byte[] existingPackData,
        IEnumerable<PackFile> filesToAdd,
        IEnumerable<string> filesToRemove = null,
        byte[] key = null,
        PackOptions options = null
    )
    {
        options ??= new PackOptions();

        var unpack = Unpack(existingPackData, key);
        var fileDict = unpack.Files.ToDictionary(f => f.VirtualPath);
        var added = new List<string>();
        var removed = new List<string>();
        var updated = new List<string>();

        if (filesToRemove != null)
        {
            foreach (var path in filesToRemove)
            {
                if (fileDict.Remove(path))
                    removed.Add(path);
            }
        }

        // Add/Update files
        foreach (var file in filesToAdd)
        {
            string normalizedPath = PathNormalizer.Normalize(file.VirtualPath);

            if (fileDict.ContainsKey(normalizedPath))
                updated.Add(normalizedPath);
            else
                added.Add(normalizedPath);

            fileDict[normalizedPath] = file;
        }

        var packResult = Pack(fileDict.Values, options);

        if (packResult.Packs.Count == 0)
            throw new InvalidOperationException("Update resulted in no packs");

        if (packResult.Packs.Count > 1)
            throw new InvalidOperationException(
                $"Update resulted in {packResult.Packs.Count} packs. " +
                $"This is not supported. Try increasing MaxFilesPerPack or reducing the number of files."
            );

        return new UpdateResult
        {
            Data = packResult.Packs[0].Data,
            Key = packResult.Packs[0].Key,
            AddedFiles = added,
            RemovedFiles = removed,
            UpdatedFiles = updated
        };
    }

    private static List<List<PackFile>> SplitIntoGroups(List<PackFile> files, ushort maxPerGroup)
    {
        var groups = new List<List<PackFile>>();

        for (int i = 0; i < files.Count; i += maxPerGroup)
        {
            var group = files.Skip(i).Take(maxPerGroup).ToList();
            groups.Add(group);
        }

        return groups;
    }
}