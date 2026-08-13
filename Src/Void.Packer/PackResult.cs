namespace Void.Packer;

public sealed class PackResult
{
    public List<PackContainer> Packs { get; set; } = [];
    public int TotalFilesPacked { get; set; }
    public long TotalOriginalSize { get; set; }  // Fixed typo
    public long TotalPackedSize { get; set; }
    public double CompressionRatio { get; set; }
    public Dictionary<string, int> FileToPackMap { get; set; } = [];
}

public class PackContainer
{
    public byte[] Data { get; set; }
    public byte[] Key { get; set; }
    public ushort FileCount { get; set; }
    public List<string> VirtualPaths { get; set; } = [];
    public long OriginalSize { get; set; }  // Fixed typo
    public long PackedSize { get; set; }
}

public class UnpackResult
{
    public List<PackFile> Files { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = [];
}

public class UpdateResult
{
    public byte[] Data { get; set; }
    public byte[] Key { get; set; }
    public List<string> AddedFiles { get; set; } = [];
    public List<string> RemovedFiles { get; set; } = [];
    public List<string> UpdatedFiles { get; set; } = [];
}

public class FileInfo
{
    public string VirtualPath { get; set; }
    public uint UncompressedSize { get; set; }
    public uint StoredSize { get; set; }
    public bool IsCompressed { get; set; }
    public uint CRC32 { get; set; }
}