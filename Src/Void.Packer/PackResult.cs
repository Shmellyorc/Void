// ============================================================================
//  PackResult.cs
// ============================================================================
//  Result types for SolidPack operations including packing, unpacking,
//  and updating.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Packer;

/// <summary>
/// Contains the results of a pack operation including all created packs and statistics.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackResult"/> class contains all the information about a
/// pack operation, including the individual packs created, file counts,
/// sizes, and a mapping from virtual paths to pack indices.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Packs"/> - The list of created pack containers</description></item>
///   <item><description><see cref="TotalFilesPacked"/> - The total number of files packed</description></item>
///   <item><description><see cref="TotalOriginalSize"/> - The total original size of all files</description></item>
///   <item><description><see cref="TotalPackedSize"/> - The total packed size of all files</description></item>
///   <item><description><see cref="CompressionRatio"/> - The compression ratio achieved</description></item>
///   <item><description><see cref="FileToPackMap"/> - Mapping from virtual path to pack index</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PackResult
{
    /// <summary>
    /// Gets or sets the list of created pack containers.
    /// </summary>
    public List<PackContainer> Packs { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of files packed.
    /// </summary>
    public int TotalFilesPacked { get; set; }

    /// <summary>
    /// Gets or sets the total original size of all files in bytes.
    /// </summary>
    public long TotalOriginalSize { get; set; }

    /// <summary>
    /// Gets or sets the total packed size of all files in bytes.
    /// </summary>
    public long TotalPackedSize { get; set; }

    /// <summary>
    /// Gets or sets the compression ratio (0 to 1). Higher is better.
    /// </summary>
    public double CompressionRatio { get; set; }

    /// <summary>
    /// Gets or sets the mapping from virtual path to pack index.
    /// </summary>
    public Dictionary<string, int> FileToPackMap { get; set; } = [];
}

/// <summary>
/// Represents a single SolidPack archive container.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackContainer"/> class contains the raw data and metadata
/// for a single SolidPack archive. It includes the file data, encryption key,
/// file count, virtual paths, and size information.
/// </para>
/// </remarks>
public class PackContainer
{
    /// <summary>
    /// Gets or sets the raw pack data.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the encryption key used for this pack.
    /// </summary>
    public byte[] Key { get; set; }

    /// <summary>
    /// Gets or sets the number of files in the pack.
    /// </summary>
    public ushort FileCount { get; set; }

    /// <summary>
    /// Gets or sets the list of virtual paths in the pack.
    /// </summary>
    public List<string> VirtualPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the original size of all files in bytes.
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// Gets or sets the packed size of all files in bytes.
    /// </summary>
    public long PackedSize { get; set; }
}

/// <summary>
/// Contains the results of an unpack operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="UnpackResult"/> class contains all the files extracted from
/// a SolidPack archive, along with any metadata extracted from the pack.
/// </para>
/// </remarks>
public class UnpackResult
{
    /// <summary>
    /// Gets or sets the list of extracted files.
    /// </summary>
    public List<PackFile> Files { get; set; } = [];

    /// <summary>
    /// Gets or sets metadata extracted from the pack.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}

/// <summary>
/// Contains the results of an update operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="UpdateResult"/> class contains the updated pack data and
/// lists of files that were added, removed, or updated during the operation.
/// </para>
/// </remarks>
public class UpdateResult
{
    /// <summary>
    /// Gets or sets the updated pack data.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the encryption key for the updated pack.
    /// </summary>
    public byte[] Key { get; set; }

    /// <summary>
    /// Gets or sets the list of files that were added.
    /// </summary>
    public List<string> AddedFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of files that were removed.
    /// </summary>
    public List<string> RemovedFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of files that were updated.
    /// </summary>
    public List<string> UpdatedFiles { get; set; } = [];
}

/// <summary>
/// Contains information about a file within a SolidPack archive.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FileInfo"/> class provides detailed information about a
/// file stored in a SolidPack archive, including its size, compression
/// status, and CRC32 checksum.
/// </para>
/// </remarks>
public class FileInfo
{
    /// <summary>
    /// Gets or sets the virtual path of the file.
    /// </summary>
    public string VirtualPath { get; set; }

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
/// Represents a chunk entry in the SolidPack archive.
/// </summary>
public class ChunkEntry
{
    /// <summary>
    /// Gets or sets the offset of the chunk in the data section.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the encrypted size of the chunk.
    /// </summary>
    public uint Size { get; set; }
}


/// <summary>
/// Represents errors that can occur during pack operations.
/// </summary>
public enum PackError
{
    /// <summary>No error.</summary>
    None,

    /// <summary>The pack file was not found at the specified path.</summary>
    PackNotFound,

    /// <summary>The file is not a valid SolidPack archive (invalid magic bytes).</summary>
    InvalidMagicBytes,

    /// <summary>The pack version is newer than supported.</summary>
    UnsupportedVersion,

    /// <summary>The pack file is too small to contain a valid header.</summary>
    PackTooSmall,

    /// <summary>The pack is encrypted but no key was provided or found.</summary>
    MissingKey,

    /// <summary>The provided key is invalid (decryption failed).</summary>
    InvalidKey,

    /// <summary>The header is corrupted or could not be decrypted.</summary>
    HeaderCorrupted,

    /// <summary>The file table in the header is corrupted.</summary>
    FileTableCorrupted,

    /// <summary>The chunk table in the header is corrupted.</summary>
    ChunkTableCorrupted,

    /// <summary>The requested file was not found in the pack.</summary>
    FileNotFound,

    /// <summary>A chunk failed to decrypt (tampered or wrong key).</summary>
    ChunkCorrupted,

    /// <summary>The requested chunk index is out of range.</summary>
    ChunkOutOfRange,

    /// <summary>The pack data is truncated (file ends before expected data).</summary>
    DataTruncated,

    /// <summary>A file's CRC32 checksum does not match.</summary>
    ChecksumMismatch,

    /// <summary>File decompression failed.</summary>
    DecompressionFailed,

    /// <summary>The compression algorithm is not supported.</summary>
    CompressionNotSupported,

    /// <summary>The chunk size is invalid (flag set but chunk size is zero).</summary>
    InvalidChunkSize,

    /// <summary>No files were provided to pack.</summary>
    NoFilesToPack,

    /// <summary>The file count exceeds the maximum allowed per pack.</summary>
    TooManyFiles,

    /// <summary>A duplicate virtual path was found.</summary>
    DuplicatePath,

    /// <summary>A file has an empty virtual path.</summary>
    EmptyVirtualPath,

    /// <summary>A source file could not be read.</summary>
    FileReadFailed,

    /// <summary>The pack is already mounted.</summary>
    PackAlreadyMounted,

    /// <summary>The pack reader has been disposed.</summary>
    PackIsDisposed
}