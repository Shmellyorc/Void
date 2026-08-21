// ============================================================================
//  SolidPackReader.cs
// ============================================================================
//  Reader for accessing files from a SolidPack archive.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Void.Packer.Utils;

namespace Void.Packer;

/// <summary>
/// Reader for accessing files from a SolidPack archive.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SolidPackReader"/> class provides read-only access to files
/// stored in a SolidPack archive. It handles decryption, decompression, and
/// file lookup with case-sensitive or case-insensitive path matching.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Read files by virtual path</description></item>
///   <item><description>List all files in the pack</description></item>
///   <item><description>Get file information (size, compression, CRC32)</description></item>
///   <item><description>Verify pack integrity</description></item>
///   <item><description>Case-sensitive or case-insensitive path matching</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Read a pack file
/// byte[] packData = File.ReadAllBytes("assets.pack");
/// 
/// using var reader = new SolidPackReader(packData, key: null, caseSensitive: false);
/// 
/// // Check if a file exists
/// if (reader.FileExists("textures/player.png"))
/// {
///     // Read the file
///     byte[] data = reader.ReadFile("textures/player.png");
/// }
/// 
/// // List all files
/// foreach (var path in reader.ListFiles())
/// {
///     var info = reader.GetFileInfo(path);
///     Console.WriteLine($"{path}: {info.UncompressedSize} bytes");
/// }
/// 
/// // Verify integrity
/// bool isValid = reader.VerifyIntegrity();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidPackReader"/> class.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <param name="caseSensitive">Whether paths should be case-sensitive.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="packData"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the pack data is too small or encrypted without a key.</exception>
    /// <exception cref="InvalidDataException">Thrown when the pack data is corrupted or has an invalid format.</exception>
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

    /// <summary>
    /// Gets the number of files in the pack.
    /// </summary>
    public ushort FileCount => _fileCount;

    /// <summary>
    /// Determines whether a file exists at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path to check.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public bool FileExists(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        var normalized = PathNormalizer.Normalize(virtualPath);

        if (_caseSensitive)
            return _fileIndex.ContainsKey(normalized);

        return _caseInsensitiveMap.ContainsKey(normalized);
    }

    /// <summary>
    /// Reads a file from the pack and returns its contents as a byte array.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist in the pack.</exception>
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

    /// <summary>
    /// Gets information about a file in the pack.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file.</param>
    /// <returns>A <see cref="FileInfo"/> object containing file metadata, or null if the file does not exist.</returns>
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

    /// <summary>
    /// Lists all virtual paths in the pack.
    /// </summary>
    /// <returns>An enumerable of virtual paths.</returns>
    public IEnumerable<string> ListFiles()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SolidPackReader));

        return _fileIndex.Keys;
    }

    /// <summary>
    /// Verifies the integrity of all files in the pack using CRC32 checksums.
    /// </summary>
    /// <returns><see langword="true"/> if all files are valid; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Disposes the reader and releases resources.
    /// </summary>
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

        _encryptedHeaderData = encryptedHeader;

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

        offset += PackConstants.HeaderReservedSize;

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
        int dataSectionLength = _packData.Length - (int)_dataEncryptedOffset;
        byte[] dataSection = new byte[dataSectionLength];
        Buffer.BlockCopy(_packData, (int)_dataEncryptedOffset, dataSection, 0, dataSectionLength);

        byte[] decryptedData;
        if (_encrypted)
        {
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