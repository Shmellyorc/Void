namespace Void.Packer;

public class FileEntry
{
    public string VirtualPath { get; set; }
    public uint OffsetInData { get; set; }
    public uint UncompressedSize { get; set; }
    public uint StoredSize { get; set; }
    public bool IsCompressed { get; set; }
    public uint CRC32 { get; set; }
}

public sealed class SolidPackBuilder
{
    private readonly PackOptions _options;
    private readonly List<PackFile> _files;
    private readonly List<FileEntry> _entries;
    private readonly MemoryStream _dataStream;

    public SolidPackBuilder(PackOptions options = null)
    {
        _options = options ?? new PackOptions();
        _files = new List<PackFile>();
        _entries = new List<FileEntry>();
        _dataStream = new MemoryStream();
    }

    public void AddFile(PackFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        if (string.IsNullOrEmpty(file.VirtualPath))
            throw new ArgumentException("Virtualpath cannot be empty");

        var normalizedPath = PathNormalizer.Normalize(file.VirtualPath);

        if (_entries.Any(x => x.VirtualPath == normalizedPath))
            throw new InvalidOperationException($"Duplicate file path: {normalizedPath}");

        _files.Add(file);
    }

    public void AddFiles(IEnumerable<PackFile> files)
    {
        foreach (var file in files)
            AddFile(file);
    }

    public PackContainer Build()
    {
        if (_files.Count == 0)
            throw new InvalidOperationException("No files to pack");

        if (_files.Count > _options.MaxFilesPerPack)
            throw new InvalidOperationException(
                $"File count ({_files.Count}) exceeds max per pack ({_options.MaxFilesPerPack})"
            );

        foreach (var file in _files)
        {
            ProcessFile(file);
        }

        var headerData = BuildHeader();
        byte[] compressedHeader = null;
        bool headerCompressed = false;

        if (_options.Compression != CompressionAlgorithm.None)
        {
            var (compressed, wasCompressed) = AdaptiveCompressor.Compress(
                headerData,
                _options.Compression,
                _options.CompressionLevel);

            compressedHeader = compressed;
            headerCompressed = wasCompressed;
        }
        else
            compressedHeader = headerData;

        byte[] dataBytes = _dataStream.ToArray();
        byte[] key = null;
        byte[] nonce = null;
        byte[] encryptedHeader = null;
        byte[] encryptedData = null;

        if (_options.Encrypt)
        {
            key = AesGcmEncryptor.GenerateKey();
            nonce = AesGcmEncryptor.GenerateNonce();

            byte[] headerAad = BuildHeaderAad();
            encryptedHeader = AesGcmEncryptor.Encrypt(compressedHeader, key, nonce, headerAad);

            // Derive data nonce from header nonce
            byte[] dataNonce = new byte[nonce.Length];
            Buffer.BlockCopy(nonce, 0, dataNonce, 0, nonce.Length);
            dataNonce[dataNonce.Length - 1] ^= 0x01;

            byte[] dataAad = BuildDataAad(encryptedHeader);
            encryptedData = AesGcmEncryptor.Encrypt(dataBytes, key, dataNonce, dataAad);
        }
        else
        {
            encryptedHeader = compressedHeader;
            encryptedData = dataBytes;
        }

        return BuildFinalPack(
            encryptedHeader,
            encryptedData,
            key,
            nonce,
            headerCompressed
        );
    }

    private void ProcessFile(PackFile file)
    {
        byte[] data = file.Data;
        byte[] processedData;
        bool compressed = false;
        uint crc = Crc32.Compute(data);

        if (_options.AdaptiveCompression && _options.Compression != CompressionAlgorithm.None)
        {
            var (compressedData, wasCompressed) = AdaptiveCompressor.Compress(
                data,
                _options.Compression,
                _options.CompressionLevel);

            processedData = compressedData;
            compressed = wasCompressed;
        }
        else
            processedData = data;

        uint offset = (uint)_dataStream.Position;

        _dataStream.Write(processedData, 0, processedData.Length);

        var entry = new FileEntry
        {
            VirtualPath = PathNormalizer.Normalize(file.VirtualPath),
            OffsetInData = offset,
            UncompressedSize = (uint)data.Length,
            StoredSize = (uint)processedData.Length,
            IsCompressed = compressed,
            CRC32 = crc
        };

        _entries.Add(entry);
    }

    private byte[] BuildHeader()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)PackConstants.CurrentVersion);

        uint fileTableOffset = PackConstants.HeaderBlockFixedSize;
        writer.Write(fileTableOffset);

        writer.Write((ushort)_entries.Count);
        writer.Write((byte)_options.Compression);
        writer.Write(new byte[PackConstants.HeaderReservedSize]);

        foreach (var entry in _entries)
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(entry.VirtualPath);
            writer.Write((ushort)pathBytes.Length);
            writer.Write(entry.OffsetInData);
            writer.Write(entry.UncompressedSize);
            writer.Write(entry.StoredSize);

            byte flags = 0;
            if (entry.IsCompressed) flags |= PackConstants.FlagFileCompressed;
            writer.Write(flags);

            writer.Write(entry.CRC32);
            writer.Write(pathBytes);
        }

        return ms.ToArray();
    }

    private byte[] BuildHeaderAad()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PackConstants.MagicBytes);
        writer.Write(PackConstants.CurrentVersion);
        writer.Write((ushort)_entries.Count);

        return ms.ToArray();
    }

    private byte[] BuildDataAad(byte[] encryptedHeader)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        uint headerHash = Crc32.Compute(encryptedHeader);
        writer.Write(headerHash);

        return ms.ToArray();
    }

    private PackContainer BuildFinalPack(
    byte[] encryptedHeader,
    byte[] encryptedData,
    byte[] key,
    byte[] nonce,
    bool headerCompressed
)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PackConstants.MagicBytes);
        writer.Write(PackConstants.CurrentVersion);

        byte flags = 0;
        if (key != null) flags |= PackConstants.FlagHeaderEncrypted;
        if (headerCompressed) flags |= PackConstants.FlagHeaderCompressed;
        writer.Write(flags);

        writer.Write((uint)encryptedHeader.Length);
        writer.Write((uint)encryptedData.Length);
        writer.Write((ushort)_entries.Count);

        if (nonce != null)
            writer.Write(nonce);
        else
            writer.Write(new byte[PackConstants.NonceSize]);

        // Write compression algorithm to bootstrap
        writer.Write((byte)_options.Compression);

        // Write remaining reserved bytes
        writer.Write(new byte[PackConstants.ReservedSize]);

        writer.Write(encryptedHeader);
        writer.Write(encryptedData);

        long originalSize = _entries.Sum(x => x.UncompressedSize);
        long packedSize = ms.Length;

        return new PackContainer
        {
            Data = ms.ToArray(),
            Key = key,
            FileCount = (ushort)_entries.Count,
            VirtualPaths = _entries.Select(x => x.VirtualPath).ToList(),
            OriginalSize = originalSize,
            PackedSize = packedSize
        };
    }
}