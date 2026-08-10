using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Void.Packer.Encryption;

namespace Void.Packer;

public sealed class PackOptions
{
    public ushort MaxFilesPerPack { get; set; } = ushort.MaxValue;

    public bool Encrypt { get; set; } = true;

    public CompressionAlgorithm Compression { get; set; } = CompressionAlgorithm.Deflate;

    public bool AdaptiveCompression { get; set; } = true;

    public bool CaseSensitive { get; set; } = false;

    public int CompressionLevel { get; set; } = 6;
}

public sealed class PackFile
{
    public string VirtualPath { get; set; }
    public byte[] Data { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}