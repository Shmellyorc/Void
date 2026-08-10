using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Void.Packer.Encryption;
using Void.Packer.Utils;

namespace Void.Packer;

public sealed class SolidPackReader : IDisposable
{
    private readonly byte[] _packData;
    private readonly byte[] _key;
    private readonly bool _encrypted;
    private readonly ushort _fileCount;
    private readonly uint _headerEncryptedSize;
    private readonly uint _dataEncryptedOffset;
    private readonly byte[] _nonce;
    private readonly Dictionary<string, FileEntry> _fileIndex;
    private readonly Dictionary<string, string> _caseInsensitiveMap;
    private readonly bool _caseSensitive;
    private readonly byte[] _decryptedHeader;
    private readonly CompressionAlgorithm _compressionAlgorithm; // Store compression used
    private bool _isDisposed;

    public SolidPackReader(byte[] packData, byte[] key = null, bool caseSensitive = false)
    {
        _packData = packData ?? throw new ArgumentNullException(nameof(packData));
        _key = key;
        _caseSensitive = caseSensitive;

        if (packData.Length < PackConstants.BootstrapHeaderSize)
            throw new InvalidOperationException("Pack data is too small to contain a valid header");

        ParseBootstrap(out _encrypted, out _headerEncryptedSize, out _fileCount, out _nonce, out _compressionAlgorithm);

        if (_fileCount == 0)
            throw new InvalidDataException("Pack contains no files");

        _dataEncryptedOffset = PackConstants.BootstrapHeaderSize + _headerEncryptedSize;

        _decryptedHeader = DecryptHeader();

        ParseFileTable(_decryptedHeader, out _fileIndex);

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

    private void ParseBootstrap(out bool encrypted, out uint headerEncryptedSize, out ushort fileCount, out byte[] nonce, out CompressionAlgorithm algorithm)
    {
        int offset = 0;

        // magic:
        var magic = new byte[PackConstants.MagicSize];
        Buffer.BlockCopy(_packData, offset, magic, 0, PackConstants.MagicSize);
        if (!magic.SequenceEqual(PackConstants.MagicBytes))
            throw new InvalidDataException("Invalid pack magic bytes");
        offset += PackConstants.MagicSize;

        // Version:
        ushort version = BitConverter.ToUInt16(_packData, offset);
        if (version > PackConstants.CurrentVersion)
            throw new InvalidDataException($"Pack version {version} is newer than supported {PackConstants.CurrentVersion}");
        offset += PackConstants.VersionSize;

        byte flags = _packData[offset];
        encrypted = (flags & PackConstants.FlagHeaderEncrypted) != 0;
        offset += PackConstants.FlagSize;

        headerEncryptedSize = BitConverter.ToUInt32(_packData, offset);
        offset += PackConstants.HeaderEncryptedSizeSize;

        // Data encrypted size (Skip, calculated from total)
        offset += PackConstants.DataEncryptedSizeSize;

        // File count:
        fileCount = BitConverter.ToUInt16(_packData, offset);
        offset += PackConstants.FileCountSize;

        // Nonce
        nonce = new byte[PackConstants.NonceSize];
        Buffer.BlockCopy(_packData, offset, nonce, 0, PackConstants.NonceSize);
        offset += PackConstants.NonceSize;

        // Reserved (skip)
        // offset += PackConstants.ReservedSize

        // Verify:
        long expectedSize = PackConstants.BootstrapHeaderSize + headerEncryptedSize;
        if (_packData.Length < expectedSize)
            throw new InvalidDataException($"Pack data truncated. Expected {expectedSize} bytes, got {_packData.Length} bytes");

        algorithm = CompressionAlgorithm.Deflate;
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

        if (_encrypted)
        {
            if (_key == null)
                throw new InvalidOperationException("Pack is encrypted but no key provided.");

            byte[] headerAad = BuildHeaderAad();

            return AesGcmEncryptor.Decrypt(encryptedHeader, _key, _nonce, headerAad);
        }

        return encryptedHeader;
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

        // Compression used (read it)
        byte compressionByte = headerData[offset];
        var headerCompression = (CompressionAlgorithm)compressionByte;
        if (headerCompression != _compressionAlgorithm)
            throw new InvalidDataException($"Compression algorithm mismatch: header={headerCompression}, bootstrap={_compressionAlgorithm}");
        offset += 1;

        // Reserved (Skip)
        offset += 3;

        // Seek to file table:
        offset = (int)fileTableOffset;

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
        uint absoluteOffset = _dataEncryptedOffset + entry.OffsetInData;
        byte[] storedData = new byte[entry.StoredSize];

        Buffer.BlockCopy(_packData, (int)absoluteOffset, storedData, 0, (int)entry.StoredSize);

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