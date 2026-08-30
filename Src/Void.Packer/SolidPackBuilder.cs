// ============================================================================
//  SolidPackBuilder.cs
// ============================================================================
//  Builder for creating SolidPack archives with configurable compression
//  and encryption.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Packer;

/// <summary>
/// Represents a file entry in the SolidPack archive.
/// </summary>
public class FileEntry
{
    /// <summary>
    /// Gets or sets the virtual path of the file.
    /// </summary>
    public string VirtualPath { get; set; }

    /// <summary>
    /// Gets or sets the offset of the file data in the data block.
    /// </summary>
    public uint OffsetInData { get; set; }

    /// <summary>
    /// Gets or sets the uncompressed size of the file in bytes.
    /// </summary>
    public uint UncompressedSize { get; set; }

    /// <summary>
    /// Gets or sets the stored (compressed) size of the file in bytes.
    /// </summary>
    public uint StoredSize { get; set; }

    /// <summary>
    /// Gets or sets whether the file is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Gets or sets the CRC32 checksum of the file.
    /// </summary>
    public uint CRC32 { get; set; }
}

/// <summary>
/// Builder for creating SolidPack archives with configurable compression and encryption.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SolidPackBuilder"/> class provides a fluent API for building
/// SolidPack archives. It handles file processing, compression, encryption,
/// and header generation.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item><description>Add files individually or in batches</description></item>
///   <item><description>Automatic compression with adaptive detection</description></item>
///   <item><description>AES-GCM encryption with separate header and data nonces</description></item>
///   <item><description>CRC32 integrity checking for each file</description></item>
///   <item><description>Path normalization and duplicate detection</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a builder with options
/// var builder = new SolidPackBuilder(new PackOptions
/// {
///     Compression = CompressionAlgorithm.Deflate,
///     Encrypt = true,
///     CompressionLevel = 6
/// });
/// 
/// // Add files
/// builder.AddFile(new PackFile
/// {
///     VirtualPath = "textures/player.png",
///     Data = File.ReadAllBytes("player.png")
/// });
/// 
/// // Build the pack
/// var container = builder.Build();
/// 
/// // Write to disk
/// File.WriteAllBytes("assets.pack", container.Data);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe.
/// </para>
/// </remarks>
public sealed class SolidPackBuilder
{
    private readonly PackOptions _options;
    private readonly List<PackFile> _files;
    private readonly List<FileEntry> _entries;
    private readonly MemoryStream _dataStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolidPackBuilder"/> class.
    /// </summary>
    /// <param name="options">The packing options. If null, default options are used.</param>
    public SolidPackBuilder(PackOptions options = null)
    {
        _options = options ?? new PackOptions();
        _files = new List<PackFile>();
        _entries = new List<FileEntry>();
        _dataStream = new MemoryStream();
    }

    /// <summary>
    /// Adds a single file to the pack.
    /// </summary>
    /// <param name="file">The file to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="file"/> has an empty virtual path.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a file with the same virtual path already exists.</exception>
    public void AddFile(PackFile file)
    {
        if (file == null)
            throw new PackException(PackError.FileReadFailed, "File cannot be null");

        if (string.IsNullOrEmpty(file.VirtualPath))
            throw new PackException(PackError.EmptyVirtualPath, "Virtualpath cannot be empty");

        if (file.Data == null)
            throw new PackException(PackError.FileReadFailed, $"File data is null for '{file.VirtualPath}'");

        var normalizedPath = PathNormalizer.Normalize(file.VirtualPath);

        if (_entries.Any(x => x.VirtualPath == normalizedPath))
            throw new PackException(PackError.DuplicatePath, $"Duplicate file path: {normalizedPath}");

        _files.Add(file);
    }

    /// <summary>
    /// Adds multiple files to the pack.
    /// </summary>
    /// <param name="files">The files to add.</param>
    public void AddFiles(IEnumerable<PackFile> files)
    {
        foreach (var file in files)
            AddFile(file);
    }

    /// <summary>
    /// Builds the SolidPack archive.
    /// </summary>
    /// <returns>A <see cref="PackContainer"/> containing the pack data and metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no files have been added or the file count exceeds the maximum.</exception>
    public PackContainer Build()
    {
        if (_files.Count == 0)
            throw new PackException(PackError.NoFilesToPack, "No files to pack");

        if (_files.Count > _options.MaxFilesPerPack)
            throw new PackException(PackError.TooManyFiles,
                $"File count ({_files.Count}) exceeds max per pack ({_options.MaxFilesPerPack})");

        foreach (var file in _files)
            ProcessFile(file);

        byte[] dataBytes = _dataStream.ToArray();
        byte[] key = null;
        byte[] nonce = null;
        byte[] encryptedHeader = null;
        byte[] encryptedData = null;
        bool chunked = false;
        List<ChunkEntry> chunkTable = new();

        if (_options.Encrypt)
        {
            key = AesGcmEncryptor.GenerateKey();
            nonce = AesGcmEncryptor.GenerateNonce();

            int chunkSize = _options.ChunkSizeKB * 1024;
            if (chunkSize > 0 && dataBytes.Length > chunkSize)
            {
                chunked = true;
            }

            if (chunked)
            {
                encryptedData = EncryptDataChunked(dataBytes, key, nonce, chunkSize, chunkTable, null);
            }

            byte[] headerData = BuildHeader(chunked, chunkTable);

            byte[] compressedHeader;
            bool headerCompressed;

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
            {
                compressedHeader = headerData;
                headerCompressed = false;
            }

            byte[] headerAad = BuildHeaderAad();
            encryptedHeader = AesGcmEncryptor.Encrypt(compressedHeader, key, nonce, headerAad);

            if (chunked)
            {
                encryptedData = EncryptDataChunked(dataBytes, key, nonce, chunkSize, chunkTable, encryptedHeader);
            }
            else
            {
                byte[] dataNonce = new byte[nonce.Length];
                Buffer.BlockCopy(nonce, 0, dataNonce, 0, nonce.Length);
                dataNonce[dataNonce.Length - 1] ^= 0x01;

                byte[] dataAad = BuildDataAad(encryptedHeader);
                encryptedData = AesGcmEncryptor.Encrypt(dataBytes, key, dataNonce, dataAad);
            }

            return BuildFinalPack(
                encryptedHeader,
                encryptedData,
                key,
                nonce,
                headerCompressed,
                chunked
            );
        }
        else
        {
            byte[] headerData = BuildHeader(false, chunkTable);

            byte[] compressedHeader;
            bool headerCompressed;

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
            {
                compressedHeader = headerData;
                headerCompressed = false;
            }

            encryptedHeader = compressedHeader;
            encryptedData = dataBytes;

            return BuildFinalPack(
                encryptedHeader,
                encryptedData,
                null,
                null,
                headerCompressed,
                false
            );
        }
    }

    private byte[] EncryptDataChunked(byte[] dataBytes, byte[] key, byte[] nonce, int chunkSize, List<ChunkEntry> chunkTable, byte[] encryptedHeader)
    {
        using var ms = new MemoryStream();
        int offset = 0;
        int chunkIndex = 0;
        chunkTable.Clear();

        while (offset < dataBytes.Length)
        {
            int remaining = dataBytes.Length - offset;
            int currentChunkSize = Math.Min(chunkSize, remaining);

            byte[] chunkData = new byte[currentChunkSize];
            Buffer.BlockCopy(dataBytes, offset, chunkData, 0, currentChunkSize);

            byte[] chunkNonce = new byte[nonce.Length];
            Buffer.BlockCopy(nonce, 0, chunkNonce, 0, nonce.Length);
            byte[] indexBytes = BitConverter.GetBytes(chunkIndex);
            chunkNonce[8] ^= indexBytes[0];
            chunkNonce[9] ^= indexBytes[1];
            chunkNonce[10] ^= indexBytes[2];
            chunkNonce[11] ^= indexBytes[3];

            byte[] chunkAad;
            if (encryptedHeader != null)
            {
                uint headerHash = Crc32.Compute(encryptedHeader);
                using var aadMs = new MemoryStream();
                using var writer = new BinaryWriter(aadMs);
                writer.Write(headerHash);
                writer.Write(chunkIndex);
                chunkAad = aadMs.ToArray();
            }
            else
            {
                chunkAad = BitConverter.GetBytes(chunkIndex);
            }

            byte[] encryptedChunk = AesGcmEncryptor.Encrypt(chunkData, key, chunkNonce, chunkAad);

            chunkTable.Add(new ChunkEntry
            {
                Offset = (uint)ms.Position,
                Size = (uint)encryptedChunk.Length
            });

            ms.Write(encryptedChunk, 0, encryptedChunk.Length);

            offset += currentChunkSize;
            chunkIndex++;
        }

        return ms.ToArray();
    }

    private byte[] BuildChunkAad(byte[] encryptedHeader, int chunkIndex)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        uint headerHash = Crc32.Compute(encryptedHeader);
        writer.Write(headerHash);
        writer.Write(chunkIndex);

        return ms.ToArray();
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

    private byte[] BuildHeader(bool chunked, List<ChunkEntry> chunkTable)
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

        if (chunked && chunkTable.Count > 0)
        {
            writer.Write((uint)chunkTable.Count);
            foreach (var chunk in chunkTable)
            {
                writer.Write(chunk.Offset);
                writer.Write(chunk.Size);
            }
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
        bool headerCompressed,
        bool chunked
    )
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PackConstants.MagicBytes);
        writer.Write(PackConstants.CurrentVersion);

        byte flags = 0;
        if (key != null) flags |= PackConstants.FlagHeaderEncrypted;
        if (headerCompressed) flags |= PackConstants.FlagHeaderCompressed;
        if (chunked) flags |= PackConstants.FlagChunked;
        writer.Write(flags);

        writer.Write((uint)encryptedHeader.Length);
        writer.Write((uint)encryptedData.Length);
        writer.Write((ushort)_entries.Count);

        if (nonce != null)
            writer.Write(nonce);
        else
            writer.Write(new byte[PackConstants.NonceSize]);

        writer.Write((byte)_options.Compression);
        writer.Write((ushort)_options.ChunkSizeKB);

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