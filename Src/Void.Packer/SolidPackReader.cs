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
using System.Security.Cryptography;
using System.Text;

using Void.Packer.Utils;

namespace Void.Packer;

/// <summary>
/// Reader for accessing files from a SolidPack archive.
/// </summary>
public sealed class SolidPackReader : IDisposable
{
    private readonly byte[] _packData;
    private readonly string _packPath;
    private FileStream _packStream;
    private readonly Lock _streamLock = new();
    private DateTime _lastAccess;
    private readonly byte[] _key;
    private readonly bool _encrypted;
    private readonly bool _chunked;
    private readonly ushort _chunkSizeKB;
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
    private readonly List<ChunkEntry> _chunkTable;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidPackReader"/> class from a file path.
    /// The file is opened lazily on first read and closed after inactivity.
    /// </summary>
    /// <param name="packPath">The path to the pack file.</param>
    /// <param name="key">The optional encryption key. If null, the key is auto-detected from a .key file next to the pack.</param>
    /// <param name="caseSensitive">Whether virtual paths should be case-sensitive. Default is false.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="packPath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the pack file does not exist.</exception>
    /// <exception cref="PackException">Thrown with a specific <see cref="PackError"/> code when the pack cannot be loaded.</exception>
    public SolidPackReader(string packPath, byte[] key = null, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(packPath))
            throw new ArgumentNullException(nameof(packPath));

        _packPath = packPath;
        _key = key;
        _caseSensitive = caseSensitive;

        var bootstrap = ReadBootstrapFromDisk();
        ParseBootstrap(bootstrap, out _encrypted, out _headerCompressed, out _headerEncryptedSize, out _fileCount, out _nonce, out _compressionAlgorithm, out _chunked, out _chunkSizeKB);

        if (_fileCount == 0)
            throw new PackException(PackError.HeaderCorrupted, "Pack contains no files");

        _dataEncryptedOffset = PackConstants.BootstrapHeaderSize + _headerEncryptedSize;

        _decryptedHeader = DecryptHeader();

        ParseFileTable(_decryptedHeader, out _fileIndex, out _chunkTable);

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
    /// Initializes a new instance of the <see cref="SolidPackReader"/> class from memory.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <param name="caseSensitive">Whether virtual paths should be case-sensitive. Default is false.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="packData"/> is null.</exception>
    /// <exception cref="PackException">Thrown with a specific <see cref="PackError"/> code when the pack cannot be loaded.</exception>
    public SolidPackReader(byte[] packData, byte[] key = null, bool caseSensitive = false)
    {
        _packData = packData ?? throw new ArgumentNullException(nameof(packData));
        _key = key;
        _caseSensitive = caseSensitive;

        if (packData.Length < PackConstants.BootstrapHeaderSize)
            throw new PackException(PackError.PackTooSmall, "Pack data is too small to contain a valid header");

        ParseBootstrap(packData, out _encrypted, out _headerCompressed, out _headerEncryptedSize, out _fileCount, out _nonce, out _compressionAlgorithm, out _chunked, out _chunkSizeKB);

        if (_fileCount == 0)
            throw new PackException(PackError.HeaderCorrupted, "Pack contains no files");

        _dataEncryptedOffset = PackConstants.BootstrapHeaderSize + _headerEncryptedSize;

        _decryptedHeader = DecryptHeader();

        ParseFileTable(_decryptedHeader, out _fileIndex, out _chunkTable);

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
    /// <value>The number of files stored in the pack archive.</value>
    public ushort FileCount => _fileCount;

    /// <summary>
    /// Determines whether a file exists at the specified virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path to check.</param>
    /// <returns><see langword="true"/> if the file exists in the pack; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="PackException">Thrown with <see cref="PackError.PackIsDisposed"/> when the reader has been disposed.</exception>
    public bool FileExists(string virtualPath)
    {
        ThrowIfDisposed();

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
    /// <exception cref="PackException">Thrown with <see cref="PackError.PackIsDisposed"/> when the reader has been disposed.</exception>
    /// <exception cref="PackException">Thrown with <see cref="PackError.FileNotFound"/> when the file does not exist.</exception>
    public byte[] ReadFile(string virtualPath)
    {
        ThrowIfDisposed();

        var normalized = PathNormalizer.Normalize(virtualPath);
        string actualPath;

        if (_caseSensitive)
        {
            actualPath = normalized;
        }
        else
        {
            if (!_caseInsensitiveMap.TryGetValue(normalized, out actualPath))
                throw new PackException(PackError.FileNotFound, $"File '{virtualPath}' not found in pack.");
        }

        if (!_fileIndex.TryGetValue(actualPath, out var entry))
            throw new PackException(PackError.FileNotFound, $"File '{virtualPath}' not found in pack.");

        return ReadFileData(entry);
    }

    /// <summary>
    /// Gets information about a file in the pack.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the file.</param>
    /// <returns>A <see cref="FileInfo"/> containing file metadata, or null if the file does not exist.</returns>
    /// <exception cref="PackException">Thrown with <see cref="PackError.PackIsDisposed"/> when the reader has been disposed.</exception>
    public FileInfo GetFileInfo(string virtualPath)
    {
        ThrowIfDisposed();

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
    /// <returns>An enumerable of virtual paths contained in the pack.</returns>
    /// <exception cref="PackException">Thrown with <see cref="PackError.PackIsDisposed"/> when the reader has been disposed.</exception>
    public IEnumerable<string> ListFiles()
    {
        ThrowIfDisposed();

        return _fileIndex.Keys;
    }

    /// <summary>
    /// Verifies the integrity of all files in the pack using CRC32 checksums.
    /// </summary>
    /// <returns><see langword="true"/> if all files are valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="PackException">Thrown with <see cref="PackError.PackIsDisposed"/> when the reader has been disposed.</exception>
    public bool VerifyIntegrity()
    {
        ThrowIfDisposed();

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
    /// Closes the stream if inactive for the specified timeout.
    /// </summary>
    /// <param name="timeout">The inactivity timeout. When the stream has been idle longer than this, it is closed.</param>
    public void CheckInactive(TimeSpan timeout)
    {
        lock (_streamLock)
        {
            if (_packStream != null && (DateTime.Now - _lastAccess) > timeout)
            {
                _packStream.Dispose();
                _packStream = null;
            }
        }
    }

    /// <summary>
    /// Disposes the reader and releases all resources including the stream.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        lock (_streamLock)
        {
            _packStream?.Dispose();
            _packStream = null;
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Attempts to create a pack reader without throwing exceptions.
    /// </summary>
    /// <param name="packPath">The path to the pack file.</param>
    /// <param name="key">The optional encryption key. If null, the key is auto-detected from a .key file next to the pack.</param>
    /// <param name="reader">When this method returns, contains the created reader, or null if creation failed.</param>
    /// <param name="error">When this method returns, contains the specific error code if creation failed, or <see cref="PackError.None"/> on success.</param>
    /// <returns><see langword="true"/> if the reader was created successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(string packPath, byte[] key, out SolidPackReader reader, out PackError error)
    {
        reader = null;
        error = PackError.None;

        if (string.IsNullOrEmpty(packPath))
        {
            error = PackError.PackNotFound;
            return false;
        }

        if (!File.Exists(packPath))
        {
            error = PackError.PackNotFound;
            return false;
        }

        if (key == null)
        {
            string autoKeyPath = Path.ChangeExtension(packPath, ".key");
            if (File.Exists(autoKeyPath))
            {
                key = File.ReadAllBytes(autoKeyPath);
            }
        }

        try
        {
            reader = new SolidPackReader(packPath, key);
            return true;
        }
        catch (PackException ex)
        {
            error = ex.Error;
            return false;
        }
        catch (EndOfStreamException)
        {
            error = PackError.DataTruncated;
            return false;
        }
        catch (Exception)
        {
            error = PackError.HeaderCorrupted;
            return false;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new PackException(PackError.PackIsDisposed, "Pack reader has been disposed.");
    }

    private FileStream GetStream()
    {
        lock (_streamLock)
        {
            if (_packStream == null)
            {
                _packStream = File.OpenRead(_packPath);
            }

            _lastAccess = DateTime.Now;
            return _packStream;
        }
    }

    private byte[] ReadBootstrapFromDisk()
    {
        byte[] bootstrap = new byte[PackConstants.BootstrapHeaderSize];

        lock (_streamLock)
        {
            var stream = GetStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.ReadExactly(bootstrap, 0, PackConstants.BootstrapHeaderSize);
        }

        return bootstrap;
    }

    private byte[] ReadFromStream(long offset, int size)
    {
        byte[] buffer = new byte[size];

        lock (_streamLock)
        {
            var stream = GetStream();
            stream.Seek(offset, SeekOrigin.Begin);
            stream.ReadExactly(buffer, 0, size);
        }

        return buffer;
    }

    private void ParseBootstrap(byte[] bootstrap, out bool encrypted, out bool headerCompressed, out uint headerEncryptedSize, out ushort fileCount,
        out byte[] nonce, out CompressionAlgorithm algorithm, out bool chunked, out ushort chunkSizeKB)
    {
        int offset = 0;

        var magic = new byte[PackConstants.MagicSize];
        Buffer.BlockCopy(bootstrap, offset, magic, 0, PackConstants.MagicSize);
        if (!magic.SequenceEqual(PackConstants.MagicBytes))
            throw new PackException(PackError.InvalidMagicBytes, "Invalid pack magic bytes");
        offset += PackConstants.MagicSize;

        ushort version = BitConverter.ToUInt16(bootstrap, offset);
        if (version > PackConstants.CurrentVersion)
            throw new PackException(PackError.UnsupportedVersion, $"Pack version {version} is newer than supported");
        offset += PackConstants.VersionSize;

        byte flags = bootstrap[offset];
        encrypted = (flags & PackConstants.FlagHeaderEncrypted) != 0;
        headerCompressed = (flags & PackConstants.FlagHeaderCompressed) != 0;
        chunked = (flags & PackConstants.FlagChunked) != 0;
        offset += PackConstants.FlagSize;

        headerEncryptedSize = BitConverter.ToUInt32(bootstrap, offset);
        offset += PackConstants.HeaderEncryptedSizeSize;

        offset += PackConstants.DataEncryptedSizeSize;

        fileCount = BitConverter.ToUInt16(bootstrap, offset);
        offset += PackConstants.FileCountSize;

        nonce = new byte[PackConstants.NonceSize];
        Buffer.BlockCopy(bootstrap, offset, nonce, 0, PackConstants.NonceSize);
        offset += PackConstants.NonceSize;

        algorithm = (CompressionAlgorithm)bootstrap[offset];
        offset += PackConstants.AlgorithmSize;

        chunkSizeKB = BitConverter.ToUInt16(bootstrap, offset);
        offset += PackConstants.ChunkSizeSize;

        if (chunked && chunkSizeKB == 0)
            throw new PackException(PackError.InvalidChunkSize, "Chunked flag set but chunk size is zero");
    }

    private byte[] DecryptHeader()
    {
        byte[] encryptedHeader;

        if (_packData != null)
        {
            encryptedHeader = new byte[_headerEncryptedSize];
            Buffer.BlockCopy(_packData, PackConstants.BootstrapHeaderSize, encryptedHeader, 0, (int)_headerEncryptedSize);
        }
        else
        {
            encryptedHeader = ReadFromStream(PackConstants.BootstrapHeaderSize, (int)_headerEncryptedSize);
        }

        _encryptedHeaderData = encryptedHeader;

        byte[] headerData;

        if (_encrypted)
        {
            if (_key == null)
                throw new PackException(PackError.MissingKey, "Pack is encrypted but no key provided.");

            byte[] headerAad = BuildHeaderAad();

            try
            {
                headerData = AesGcmEncryptor.Decrypt(encryptedHeader, _key, _nonce, headerAad);
            }
            catch (CryptographicException)
            {
                throw new PackException(PackError.InvalidKey, "Invalid key or corrupted header.");
            }
        }
        else
        {
            headerData = encryptedHeader;
        }

        if (_headerCompressed && _compressionAlgorithm != CompressionAlgorithm.None)
        {
            using var compressedStream = new MemoryStream(headerData);
            using var decompressedStream = new MemoryStream();

            try
            {
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
                    throw new PackException(PackError.CompressionNotSupported, $"Compression algorithm {_compressionAlgorithm} not supported.");
                }
            }
            catch (PackException)
            {
                throw;
            }
            catch
            {
                throw new PackException(PackError.HeaderCorrupted, "Header decompression failed.");
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

    private void ParseFileTable(byte[] headerData, out Dictionary<string, FileEntry> index, out List<ChunkEntry> chunkTable)
    {
        index = new Dictionary<string, FileEntry>(_caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        chunkTable = new List<ChunkEntry>();

        int offset = 0;

        ushort headerVersion = BitConverter.ToUInt16(headerData, offset);
        if (headerVersion != PackConstants.CurrentVersion)
            throw new PackException(PackError.HeaderCorrupted, $"Header version mismatch: {headerVersion} vs {PackConstants.CurrentVersion}");
        offset += 2;

        uint fileTableOffset = BitConverter.ToUInt32(headerData, offset);
        offset += 4;

        ushort fileCount = BitConverter.ToUInt16(headerData, offset);
        if (fileCount != _fileCount)
            throw new PackException(PackError.HeaderCorrupted, $"Header file count mismatch: {fileCount} vs {_fileCount}");
        offset += 2;

        byte compressionByte = headerData[offset];
        var headerCompression = (CompressionAlgorithm)compressionByte;
        if (headerCompression != _compressionAlgorithm)
            throw new PackException(PackError.HeaderCorrupted, $"Compression algorithm mismatch: header={headerCompression}, bootstrap={_compressionAlgorithm}");
        offset += 1;

        offset += PackConstants.HeaderReservedSize;

        offset = PackConstants.HeaderBlockFixedSize;

        for (int i = 0; i < fileCount; i++)
        {
            if (offset + PackConstants.FileEntryFixedSize > headerData.Length)
                throw new PackException(PackError.FileTableCorrupted, "File table truncated");

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

            if (offset + pathLength > headerData.Length)
                throw new PackException(PackError.FileTableCorrupted, "File table path truncated");

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

        if (_chunked)
        {
            if (offset + 4 > headerData.Length)
                throw new PackException(PackError.ChunkTableCorrupted, "Chunk table missing");

            uint chunkCount = BitConverter.ToUInt32(headerData, offset);
            offset += 4;

            for (int i = 0; i < chunkCount; i++)
            {
                if (offset + 8 > headerData.Length)
                    throw new PackException(PackError.ChunkTableCorrupted, "Chunk table truncated");

                uint chunkOffset = BitConverter.ToUInt32(headerData, offset);
                offset += 4;

                uint chunkSize = BitConverter.ToUInt32(headerData, offset);
                offset += 4;

                chunkTable.Add(new ChunkEntry
                {
                    Offset = chunkOffset,
                    Size = chunkSize
                });
            }
        }
    }

    private byte[] ReadFileData(FileEntry entry)
    {
        if (_chunked && _chunkTable.Count > 0)
        {
            return ReadFileDataChunked(entry);
        }
        else
        {
            return ReadFileDataSolid(entry);
        }
    }

    private byte[] ReadFileDataSolid(FileEntry entry)
    {
        byte[] dataSection;

        if (_packData != null)
        {
            int dataSectionLength = _packData.Length - (int)_dataEncryptedOffset;
            dataSection = new byte[dataSectionLength];
            Buffer.BlockCopy(_packData, (int)_dataEncryptedOffset, dataSection, 0, dataSectionLength);
        }
        else
        {
            var stream = GetStream();
            int dataSectionLength = (int)(stream.Length - _dataEncryptedOffset);
            dataSection = ReadFromStream(_dataEncryptedOffset, dataSectionLength);
        }

        byte[] decryptedData;
        if (_encrypted)
        {
            byte[] dataNonce = new byte[_nonce.Length];
            Buffer.BlockCopy(_nonce, 0, dataNonce, 0, _nonce.Length);
            dataNonce[dataNonce.Length - 1] ^= 0x01;

            byte[] dataAad = BitConverter.GetBytes(Crc32.Compute(_encryptedHeaderData));

            try
            {
                decryptedData = AesGcmEncryptor.Decrypt(dataSection, _key, dataNonce, dataAad);
            }
            catch (CryptographicException)
            {
                throw new PackException(PackError.InvalidKey, "Invalid key or corrupted data section.");
            }
        }
        else
        {
            decryptedData = dataSection;
        }

        if (entry.OffsetInData + entry.StoredSize > decryptedData.Length)
            throw new PackException(PackError.DataTruncated, "File data extends beyond decrypted data");

        byte[] storedData = new byte[entry.StoredSize];
        Buffer.BlockCopy(decryptedData, (int)entry.OffsetInData, storedData, 0, (int)entry.StoredSize);

        if (entry.IsCompressed)
        {
            try
            {
                return AdaptiveCompressor.Decompress(
                    storedData,
                    (int)entry.UncompressedSize,
                    _compressionAlgorithm
                );
            }
            catch
            {
                throw new PackException(PackError.DecompressionFailed, "File decompression failed.");
            }
        }

        return storedData;
    }

    private byte[] ReadFileDataChunked(FileEntry entry)
    {
        int chunkSizeBytes = _chunkSizeKB * 1024;
        int startChunk = (int)(entry.OffsetInData / chunkSizeBytes);
        int endChunk = (int)((entry.OffsetInData + entry.StoredSize - 1) / chunkSizeBytes);

        using var ms = new MemoryStream();

        for (int chunkIndex = startChunk; chunkIndex <= endChunk; chunkIndex++)
        {
            if (chunkIndex >= _chunkTable.Count)
                throw new PackException(PackError.ChunkOutOfRange, $"Chunk index {chunkIndex} out of range (max {_chunkTable.Count - 1})");

            var chunk = _chunkTable[chunkIndex];

            byte[] encryptedChunk;

            if (_packData != null)
            {
                encryptedChunk = new byte[chunk.Size];
                Buffer.BlockCopy(_packData, (int)(_dataEncryptedOffset + chunk.Offset), encryptedChunk, 0, (int)chunk.Size);
            }
            else
            {
                encryptedChunk = ReadFromStream(_dataEncryptedOffset + chunk.Offset, (int)chunk.Size);
            }

            byte[] decryptedChunk;
            if (_encrypted)
            {
                byte[] chunkNonce = new byte[_nonce.Length];
                Buffer.BlockCopy(_nonce, 0, chunkNonce, 0, _nonce.Length);
                byte[] indexBytes = BitConverter.GetBytes(chunkIndex);
                chunkNonce[8] ^= indexBytes[0];
                chunkNonce[9] ^= indexBytes[1];
                chunkNonce[10] ^= indexBytes[2];
                chunkNonce[11] ^= indexBytes[3];

                byte[] chunkAad = BuildChunkAad(chunkIndex);

                try
                {
                    decryptedChunk = AesGcmEncryptor.Decrypt(encryptedChunk, _key, chunkNonce, chunkAad);
                }
                catch (CryptographicException)
                {
                    throw new PackException(PackError.ChunkCorrupted, $"Chunk {chunkIndex} decryption failed.");
                }
            }
            else
            {
                decryptedChunk = encryptedChunk;
            }

            int chunkStartInData = chunkIndex * chunkSizeBytes;
            int copyStart = Math.Max((int)entry.OffsetInData, chunkStartInData) - chunkStartInData;
            int copyEnd = Math.Min((int)(entry.OffsetInData + entry.StoredSize), chunkStartInData + decryptedChunk.Length) - chunkStartInData;
            int copyLength = copyEnd - copyStart;

            if (copyLength > 0)
            {
                ms.Write(decryptedChunk, copyStart, copyLength);
            }
        }

        byte[] storedData = ms.ToArray();

        if (entry.IsCompressed)
        {
            try
            {
                return AdaptiveCompressor.Decompress(
                    storedData,
                    (int)entry.UncompressedSize,
                    _compressionAlgorithm
                );
            }
            catch
            {
                throw new PackException(PackError.DecompressionFailed, "File decompression failed.");
            }
        }

        return storedData;
    }

    private byte[] BuildChunkAad(int chunkIndex)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        uint headerHash = Crc32.Compute(_encryptedHeaderData);
        writer.Write(headerHash);
        writer.Write(chunkIndex);

        return ms.ToArray();
    }
}