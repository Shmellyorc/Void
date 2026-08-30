// ============================================================================
//  Packer.cs
// ============================================================================
//  High-level API for creating, extracting, verifying, and updating SolidPack
//  archive files.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Void.Packer.Utils;

namespace Void.Packer;

/// <summary>
/// High-level API for creating, extracting, verifying, and updating SolidPack
/// archive files.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Packer"/> class provides a convenient static interface for
/// working with SolidPack archives. It supports packing files into a single
/// archive or splitting across multiple packs, extracting files, verifying
/// integrity, and updating existing packs.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description><see cref="Pack"/> - Creates one or more SolidPack archives from files</description></item>
///   <item><description><see cref="Unpack"/> - Extracts all files from a SolidPack archive</description></item>
///   <item><description><see cref="Verify"/> - Checks the integrity of a SolidPack archive</description></item>
///   <item><description><see cref="ListFiles"/> - Lists all files contained in a SolidPack archive</description></item>
///   <item><description><see cref="Update"/> - Updates an existing pack with new files</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a pack from files
/// var files = new[]
/// {
///     new PackFile { VirtualPath = "textures/player.png", Data = File.ReadAllBytes("player.png") },
///     new PackFile { VirtualPath = "sounds/explosion.wav", Data = File.ReadAllBytes("explosion.wav") }
/// };
/// 
/// var result = Packer.Pack(files, new PackOptions { MaxFilesPerPack = 100 });
/// 
/// // Write the pack to disk
/// File.WriteAllBytes("assets.pack", result.Packs[0].Data);
/// 
/// // Verify the pack
/// bool isValid = Packer.Verify(File.ReadAllBytes("assets.pack"));
/// 
/// // List files in the pack
/// var fileList = Packer.ListFiles(File.ReadAllBytes("assets.pack"));
/// 
/// // Extract all files
/// var unpackResult = Packer.Unpack(File.ReadAllBytes("assets.pack"));
/// 
/// // Update an existing pack
/// var updateResult = Packer.Update(
///     File.ReadAllBytes("assets.pack"),
///     new[] { new PackFile { VirtualPath = "newfile.txt", Data = new byte[] { 1, 2, 3 } } },
///     filesToRemove: new[] { "oldfile.txt" }
/// );
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe for read operations. Write operations should
/// be synchronized externally.
/// </para>
/// </remarks>
public static class Packer
{
    /// <summary>
    /// Packs a collection of files into one or more SolidPack archives.
    /// </summary>
    /// <param name="files">The files to pack.</param>
    /// <param name="options">Packing options. If null, default options are used.</param>
    /// <returns>A <see cref="PackResult"/> containing the packed archives and statistics.</returns>
    /// <exception cref="ArgumentException">Thrown when no files are provided.</exception>
    public static PackResult Pack(IEnumerable<PackFile> files, PackOptions options = null)
    {
        options ??= new PackOptions();
        var fileList = files.ToList();

        if (fileList.Count == 0)
            throw new ArgumentException("No files to pack", nameof(files));

        var groups = SplitIntoGroups(fileList, options.MaxFilesPerPack);
        var result = new PackResult();

        foreach (var group in groups)
        {
            var builder = new SolidPackBuilder(options);
            builder.AddFiles(group);
            var container = builder.Build();

            result.Packs.Add(container);
            result.TotalFilesPacked += container.FileCount;
            result.TotalOriginalSize += container.OriginalSize;
            result.TotalPackedSize += container.PackedSize;

            foreach (var path in container.VirtualPaths)
            {
                result.FileToPackMap[path] = result.Packs.Count - 1;
            }
        }

        result.CompressionRatio = result.TotalFilesPacked > 0
            ? 1.0 - ((double)result.TotalPackedSize) / result.TotalOriginalSize
            : 0;

        return result;
    }

    /// <summary>
    /// Extracts all files from a SolidPack archive.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <returns>An <see cref="UnpackResult"/> containing all extracted files.</returns>
    public static UnpackResult Unpack(byte[] packData, byte[] key = null)
    {
        using var reader = new SolidPackReader(packData, key);

        var result = new UnpackResult();

        foreach (var path in reader.ListFiles())
        {
            var data = reader.ReadFile(path);

            result.Files.Add(new PackFile
            {
                VirtualPath = path,
                Data = data
            });
        }

        return result;
    }

    /// <summary>
    /// Verifies the integrity of a SolidPack archive.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <returns><see langword="true"/> if the pack is valid; otherwise, <see langword="false"/>.</returns>
    public static bool Verify(byte[] packData, byte[] key = null)
    {
        try
        {
            using var reader = new SolidPackReader(packData, key);
            return reader.VerifyIntegrity();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lists all files contained in a SolidPack archive.
    /// </summary>
    /// <param name="packData">The raw pack data.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <returns>A list of virtual paths contained in the pack.</returns>
    public static List<string> ListFiles(byte[] packData, byte[] key = null)
    {
        using var reader = new SolidPackReader(packData, key);
        return reader.ListFiles().ToList();
    }

    /// <summary>
    /// Updates an existing SolidPack archive with new files and removed files.
    /// </summary>
    /// <param name="existingPackData">The existing pack data.</param>
    /// <param name="filesToAdd">The files to add or update.</param>
    /// <param name="filesToRemove">The virtual paths of files to remove.</param>
    /// <param name="key">The optional encryption key.</param>
    /// <param name="options">Packing options. If null, default options are used.</param>
    /// <returns>An <see cref="UpdateResult"/> containing the updated pack data and change information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the update results in multiple packs.</exception>
    public static UpdateResult Update(
        byte[] existingPackData,
        IEnumerable<PackFile> filesToAdd,
        IEnumerable<string> filesToRemove = null,
        byte[] key = null,
        PackOptions options = null
    )
    {
        options ??= new PackOptions();

        var unpack = Unpack(existingPackData, key);
        var fileDict = unpack.Files.ToDictionary(f => f.VirtualPath);
        var added = new List<string>();
        var removed = new List<string>();
        var updated = new List<string>();

        if (filesToRemove != null)
        {
            foreach (var path in filesToRemove)
            {
                if (fileDict.Remove(path))
                    removed.Add(path);
            }
        }

        foreach (var file in filesToAdd)
        {
            string normalizedPath = PathNormalizer.Normalize(file.VirtualPath);

            if (fileDict.ContainsKey(normalizedPath))
                updated.Add(normalizedPath);
            else
                added.Add(normalizedPath);

            fileDict[normalizedPath] = file;
        }

        var packResult = Pack(fileDict.Values, options);

        if (packResult.Packs.Count == 0)
            throw new InvalidOperationException("Update resulted in no packs");

        if (packResult.Packs.Count > 1)
            throw new InvalidOperationException(
                $"Update resulted in {packResult.Packs.Count} packs. " +
                $"This is not supported. Try increasing MaxFilesPerPack or reducing the number of files."
            );

        return new UpdateResult
        {
            Data = packResult.Packs[0].Data,
            Key = packResult.Packs[0].Key,
            AddedFiles = added,
            RemovedFiles = removed,
            UpdatedFiles = updated
        };
    }

    private static List<List<PackFile>> SplitIntoGroups(List<PackFile> files, ushort maxPerGroup)
    {
        var groups = new List<List<PackFile>>();

        for (int i = 0; i < files.Count; i += maxPerGroup)
        {
            var group = files.Skip(i).Take(maxPerGroup).ToList();
            groups.Add(group);
        }

        return groups;
    }



    /// <summary>
    /// Attempts to load a pack file without throwing exceptions.
    /// </summary>
    /// <param name="packPath">The path to the pack file.</param>
    /// <param name="key">The optional encryption key. If null, the key is auto-detected from a .key file next to the pack.</param>
    /// <param name="reader">When this method returns, contains the loaded pack reader, or null if loading failed.</param>
    /// <param name="error">When this method returns, contains the error code if loading failed, or PackError.None on success.</param>
    /// <returns><see langword="true"/> if the pack was loaded successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryLoadPack(string packPath, byte[] key, out SolidPackReader reader, out PackError error)
        => SolidPackReader.TryCreate(packPath, key, out reader, out error);

    /// <summary>
    /// Attempts to load a pack file without throwing exceptions.
    /// </summary>
    /// <param name="packPath">The path to the pack file.</param>
    /// <param name="reader">When this method returns, contains the loaded pack reader, or null if loading failed.</param>
    /// <param name="error">When this method returns, contains the error code if loading failed, or PackError.None on success.</param>
    /// <returns><see langword="true"/> if the pack was loaded successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryLoadPack(string packPath, out SolidPackReader reader, out PackError error)
        => SolidPackReader.TryCreate(packPath, null, out reader, out error);
}