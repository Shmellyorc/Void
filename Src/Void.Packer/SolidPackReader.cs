namespace Void.Packer;

public sealed class SolidPackReader : IDisposable
{
    private readonly byte[] _packData;
    private readonly byte[] _key;
    private readonly bool _encrypted;
    private readonly ushort _fileCount;
    private readonly bool _headerCompressed;
    private readonly uint _headerEncryptedSize;
    private readonly uint _dataEncryptedOffset;
    private byte[] _encryptedHeaderData;
    private readonly byte[] _nonce;
    private readonly Dictionary<string, FileEntry> _fileIndex;
    private readonly Dictionary<string, string> _caseInsensitiveMap;
    private readonly bool _caseSensitive;
    private readonly byte[] _decryptedHeader;
    private readonly CompressionAlgorithm _compressionAlgorithm;
    private bool _isDisposed;

    public SolidPackReader(byte[] packData, byte[] key = null, bool caseSensitive = false)
    {
        _packData = packData ?? throw new ArgumentNullException(nameof(packData));
        _key = key;
        _caseSensitive = caseSensitive;

        if (packData.Length < PackConstants.BootstrapHeaderSize)
            throw new InvalidOperationException("Pack data is too small to contain a valid header");

        ParseBootstrap(out _encrypted, out _headerCompressed, out _headerEncryptedSize, out _fileCount, out _nonce, out _compressionAlgorithm);

        if (_fileCount == 0)
            throw new InvalidDataException("Pack contains no files");

        _dataEncryptedOffset = PackConstants.BootstrapHeaderSize + _headerEncryptedSize;

        _decryptedHeader = DecryptHeader();

        ParseFileTable(_decryptedHeader, out _fileIndex);

        if (_encrypted && _key == null)
            throw new InvalidOperationException("Pack is encrypted but no key provided.");

        if (!_caseSensitive)
        {
            _caseInsensitiveMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, _) in _fileIndex)
            {
                var normalize = PathNormalizer.Normalize(k);
                _caseInsensitiveMap[normalize] = k;
            }
        }
    }

    public ushort FileCount => _fileCount;

    public bool FileExists(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        var normalized = PathNormalizer.Normalize(virtualPath);

        if (_caseSensitive)
            return _fileIndex.ContainsKey(normalized);

        return _caseInsensitiveMap.ContainsKey(normalized);
    }

    public byte[] ReadFile(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        var normalized = PathNormalizer.Normalize(virtualPath);
        string actualPath;

        if (_caseSensitive)
        {
            actualPath = normalized;
        }
        else
        {
            if (!_caseInsensitiveMap.TryGetValue(normalized, out actualPath))
                throw new FileNotFoundException($"File '{virtualPath}' not found in pack.");
        }

        if (!_fileIndex.TryGetValue(actualPath, out var entry))
            throw new FileNotFoundException($"File '{virtualPath}' not found in pack.");

        return ReadFileData(entry);
    }

    public FileInfo GetFileInfo(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        var normalized = PathNormalizer.Normalize(virtualPath);
        string actualPath;

        if (_caseSensitive)
        {
            actualPath = normalized;
        }
        else
        {
            if (!_caseInsensitiveMap.TryGetValue(normalized, out actualPath))
                return null;
        }

        if (!_fileIndex.TryGetValue(actualPath, out var entry))
            return null;

        return new FileInfo
        {
            VirtualPath = entry.VirtualPath,
            UncompressedSize = entry.UncompressedSize,
            StoredSize = entry.StoredSize,
            IsCompressed = entry.IsCompressed,
            CRC32 = entry.CRC32
        };
    }

    public IEnumerable<string> ListFiles()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        return _fileIndex.Keys;
    }

    public bool VerifyIntegrity()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        try
        {
            foreach (var entry in _fileIndex.Values)
            {
                var data = ReadFileData(entry);
                var crc = Crc32.Compute(data);
                if (crc != entry.CRC32)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
    }

    private void ParseBootstrap(out bool encrypted, out bool headerCompressed, out uint headerEncryptedSize, out ushort fileCount,
    out byte[] nonce, out CompressionAlgorithm algorithm)
    {
        int offset = 0;

        var magic = new byte[PackConstants.MagicSize];
        Buffer.BlockCopy(_packData, offset, magic, 0, PackConstants.MagicSize);
        if (!magic.SequenceEqual(PackConstants.MagicBytes))
            throw new InvalidDataException("Invalid pack magic bytes");
        offset += PackConstants.MagicSize;

        ushort version = BitConverter.ToUInt16(_packData, offset);
        if (version > PackConstants.CurrentVersion)
            throw new InvalidDataException($"Pack version {version} is newer than supported {PackConstants.CurrentVersion}");
        offset += PackConstants.VersionSize;

        byte flags = _packData[offset];
        encrypted = (flags & PackConstants.FlagHeaderEncrypted) != 0;
        headerCompressed = (flags & PackConstants.FlagHeaderCompressed) != 0;
        offset += PackConstants.FlagSize;

        headerEncryptedSize = BitConverter.ToUInt32(_packData, offset);
        offset += PackConstants.HeaderEncryptedSizeSize;

        offset += PackConstants.DataEncryptedSizeSize;

        fileCount = BitConverter.ToUInt16(_packData, offset);
        offset += PackConstants.FileCountSize;

        nonce = new byte[PackConstants.NonceSize];
        Buffer.BlockCopy(_packData, offset, nonce, 0, PackConstants.NonceSize);
        offset += PackConstants.NonceSize;

        // Read compression algorithm from bootstrap
        algorithm = (CompressionAlgorithm)_packData[offset];
        offset += PackConstants.AlgorithmSize;

        long expectedSize = PackConstants.BootstrapHeaderSize + headerEncryptedSize;
        if (_packData.Length < expectedSize)
            throw new InvalidDataException($"Pack data truncated. Expected {expectedSize} bytes, got {_packData.Length} bytes");
    }

    private byte[] DecryptHeader()
    {
        byte[] encryptedHeader = new byte[_headerEncryptedSize];
        Buffer.BlockCopy(
            _packData,
            PackConstants.BootstrapHeaderSize,
            encryptedHeader,
            0,
            (int)_headerEncryptedSize
        );

        _encryptedHeaderData = encryptedHeader; // SAVE THIS

        byte[] headerData;

        if (_encrypted)
        {
            if (_key == null)
                throw new InvalidOperationException("Pack is encrypted but no key provided.");

            byte[] headerAad = BuildHeaderAad();
            headerData = AesGcmEncryptor.Decrypt(encryptedHeader, _key, _nonce, headerAad);
        }
        else
        {
            headerData = encryptedHeader;
        }

        if (_headerCompressed && _compressionAlgorithm != CompressionAlgorithm.None)
        {
            using var compressedStream = new MemoryStream(headerData);
            using var decompressedStream = new MemoryStream();

            if (_compressionAlgorithm == CompressionAlgorithm.Deflate)
            {
                using var decompressor = new DeflateStream(compressedStream, CompressionMode.Decompress);
                decompressor.CopyTo(decompressedStream);
            }
            else if (_compressionAlgorithm == CompressionAlgorithm.Brotli)
            {
                using var decompressor = new BrotliStream(compressedStream, CompressionMode.Decompress);
                decompressor.CopyTo(decompressedStream);
            }
            else
            {
                throw new NotSupportedException($"Compression algorithm {_compressionAlgorithm} not supported.");
            }

            headerData = decompressedStream.ToArray();
        }

        return headerData;
    }

    private byte[] BuildHeaderAad()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PackConstants.MagicBytes);
        writer.Write(PackConstants.CurrentVersion);
        writer.Write(_fileCount);

        return ms.ToArray();
    }

    private void ParseFileTable(byte[] headerData, out Dictionary<string, FileEntry> index)
    {
        index = new Dictionary<string, FileEntry>(_caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        int offset = 0;

        ushort headerVersion = BitConverter.ToUInt16(headerData, offset);
        if (headerVersion != PackConstants.CurrentVersion)
            throw new InvalidDataException($"Header version mismatch: {headerVersion} vs {PackConstants.CurrentVersion}");
        offset += 2;

        uint fileTableOffset = BitConverter.ToUInt32(headerData, offset);
        offset += 4;

        ushort fileCount = BitConverter.ToUInt16(headerData, offset);
        if (fileCount != _fileCount)
            throw new InvalidDataException($"Header file count mismatch: {fileCount} vs {_fileCount}");
        offset += 2;

        byte compressionByte = headerData[offset];
        var headerCompression = (CompressionAlgorithm)compressionByte;
        if (headerCompression != _compressionAlgorithm)
            throw new InvalidDataException($"Compression algorithm mismatch: header={headerCompression}, bootstrap={_compressionAlgorithm}");
        offset += 1;

        offset += PackConstants.HeaderReservedSize; // Skip reserved bytes

        offset = PackConstants.HeaderBlockFixedSize;

        for (int i = 0; i < fileCount; i++)
        {
            ushort pathLength = BitConverter.ToUInt16(headerData, offset);
            offset += 2;

            uint offsetInData = BitConverter.ToUInt32(headerData, offset);
            offset += 4;

            uint uncompressedSize = BitConverter.ToUInt32(headerData, offset);
            offset += 4;

            uint storedSize = BitConverter.ToUInt32(headerData, offset);
            offset += 4;

            byte flags = headerData[offset];
            offset += 1;

            uint crc = BitConverter.ToUInt32(headerData, offset);
            offset += 4;

            byte[] pathBytes = new byte[pathLength];
            Buffer.BlockCopy(headerData, offset, pathBytes, 0, pathLength);
            string path = Encoding.UTF8.GetString(pathBytes);
            offset += pathLength;

            var entry = new FileEntry
            {
                VirtualPath = path,
                OffsetInData = offsetInData,
                UncompressedSize = uncompressedSize,
                StoredSize = storedSize,
                IsCompressed = (flags & PackConstants.FlagFileCompressed) != 0,
                CRC32 = crc
            };

            index[path] = entry;
        }
    }

    private byte[] ReadFileData(FileEntry entry)
    {
        // Get the raw data section
        int dataSectionLength = _packData.Length - (int)_dataEncryptedOffset;
        byte[] dataSection = new byte[dataSectionLength];
        Buffer.BlockCopy(_packData, (int)_dataEncryptedOffset, dataSection, 0, dataSectionLength);

        // Decrypt if needed
        byte[] decryptedData;
        if (_encrypted)
        {
            // Derive data nonce from stored nonce
            byte[] dataNonce = new byte[_nonce.Length];
            Buffer.BlockCopy(_nonce, 0, dataNonce, 0, _nonce.Length);
            dataNonce[dataNonce.Length - 1] ^= 0x01;

            byte[] dataAad = BitConverter.GetBytes(Crc32.Compute(_encryptedHeaderData));
            decryptedData = AesGcmEncryptor.Decrypt(dataSection, _key, dataNonce, dataAad);
        }
        else
        {
            decryptedData = dataSection;
        }

        // Now read the file from decrypted data
        if (entry.OffsetInData + entry.StoredSize > decryptedData.Length)
            throw new InvalidDataException($"File data extends beyond decrypted data");

        byte[] storedData = new byte[entry.StoredSize];
        Buffer.BlockCopy(decryptedData, (int)entry.OffsetInData, storedData, 0, (int)entry.StoredSize);

        if (entry.IsCompressed)
        {
            return AdaptiveCompressor.Decompress(
                storedData,
                (int)entry.UncompressedSize,
                _compressionAlgorithm
            );
        }

        return storedData;
    }
}