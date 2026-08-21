// ============================================================================
//  PackOptions.cs
// ============================================================================
//  Configuration options for packing SolidPack archives.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Packer;

/// <summary>
/// Configuration options for packing SolidPack archives.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackOptions"/> class provides configuration settings for
/// the packing process, controlling encryption, compression, file limits,
/// and path handling.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="MaxFilesPerPack"/> - Maximum number of files per pack archive</description></item>
///   <item><description><see cref="Encrypt"/> - Whether to encrypt the pack contents</description></item>
///   <item><description><see cref="Compression"/> - The compression algorithm to use</description></item>
///   <item><description><see cref="AdaptiveCompression"/> - Whether to skip compression for files that don't benefit</description></item>
///   <item><description><see cref="CaseSensitive"/> - Whether virtual paths are case-sensitive</description></item>
///   <item><description><see cref="CompressionLevel"/> - The compression level (1-9)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create options with custom settings
/// var options = new PackOptions
/// {
///     MaxFilesPerPack = 500,
///     Encrypt = true,
///     Compression = CompressionAlgorithm.Deflate,
///     AdaptiveCompression = true,
///     CompressionLevel = 6
/// };
/// 
/// // Use options when packing
/// var result = Packer.Pack(files, options);
/// </code>
/// </para>
/// </remarks>
public sealed class PackOptions
{
    /// <summary>
    /// Gets or sets the maximum number of files per pack archive.
    /// </summary>
    /// <value>The maximum number of files. Default is <see cref="ushort.MaxValue"/>.</value>
    public ushort MaxFilesPerPack { get; set; } = ushort.MaxValue;

    /// <summary>
    /// Gets or sets whether the pack should be encrypted.
    /// </summary>
    /// <value><see langword="true"/> to encrypt; otherwise, <see langword="false"/>. Default is <see langword="true"/>.</value>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets the compression algorithm to use.
    /// </summary>
    /// <value>The compression algorithm. Default is <see cref="CompressionAlgorithm.Deflate"/>.</value>
    public CompressionAlgorithm Compression { get; set; } = CompressionAlgorithm.Deflate;

    /// <summary>
    /// Gets or sets whether to use adaptive compression.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to skip compression for files that don't benefit;
    /// otherwise, <see langword="false"/>. Default is <see langword="true"/>.
    /// </value>
    public bool AdaptiveCompression { get; set; } = true;

    /// <summary>
    /// Gets or sets whether virtual paths are case-sensitive.
    /// </summary>
    /// <value><see langword="true"/> for case-sensitive paths; otherwise, <see langword="false"/>. Default is <see langword="false"/>.</value>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets the compression level.
    /// </summary>
    /// <value>The compression level between 1 (fastest) and 9 (best compression). Default is 6.</value>
    public int CompressionLevel { get; set; } = 6;
}

/// <summary>
/// Represents a file to be packed into a SolidPack archive.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PackFile"/> class represents a single file entry in a
/// SolidPack archive. It contains the virtual path, raw data, and optional
/// metadata associated with the file.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="VirtualPath"/> - The virtual path of the file within the pack</description></item>
///   <item><description><see cref="Data"/> - The raw file data as a byte array</description></item>
///   <item><description><see cref="Metadata"/> - Optional metadata dictionary for the file</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a pack file from disk
/// var file = new PackFile
/// {
///     VirtualPath = "textures/player.png",
///     Data = File.ReadAllBytes("player.png"),
///     Metadata = new Dictionary&lt;string, object&gt;
///     {
///         ["Author"] = "Artist Name",
///         ["Compressed"] = true
///     }
/// };
/// 
/// // Pack the file
/// var result = Packer.Pack(new[] { file });
/// </code>
/// </para>
/// </remarks>
public sealed class PackFile
{
    /// <summary>
    /// Gets or sets the virtual path of the file within the pack.
    /// </summary>
    public string VirtualPath { get; set; }

    /// <summary>
    /// Gets or sets the raw file data.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets optional metadata associated with the file.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }
}