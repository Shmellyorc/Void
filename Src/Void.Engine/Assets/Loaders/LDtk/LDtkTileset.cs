// ============================================================================
//  LDtkTileset.cs
// ============================================================================
//  Represents a tileset definition from an LDtk project.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a tileset definition from an LDtk project.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkTileset"/> class contains all the metadata for a tileset
/// defined in an LDtk project, including its dimensions, path, and tags.
/// Tilesets are loaded as part of the <see cref="LDtkMap"/> and can be
/// accessed by ID or name.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Id"/> - Unique identifier for the tileset</description></item>
///   <item><description><see cref="Name"/> - Display name of the tileset</description></item>
///   <item><description><see cref="CellSize"/> - Number of tiles in each dimension</description></item>
///   <item><description><see cref="Size"/> - Size of the tileset texture in pixels</description></item>
///   <item><description><see cref="Path"/> - Relative path to the tileset image file</description></item>
///   <item><description><see cref="TileSize"/> - Size of each tile in pixels</description></item>
///   <item><description><see cref="Spacing"/> - Spacing between tiles in the texture</description></item>
///   <item><description><see cref="Padding"/> - Padding around tiles in the texture</description></item>
///   <item><description><see cref="Tags"/> - List of tags associated with the tileset</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get a tileset from the map
/// var tileset = map.GetTilesetByName("Tileset_01");
/// 
/// // Access tileset properties
/// int tileSize = tileset.TileSize;
/// string texturePath = tileset.Path;
/// 
/// // Load the tileset texture
/// var texture = AssetManager.Instance.LoadTexture(tileset.Path);
/// 
/// // Get tileset by ID
/// var tilesetById = map.GetTilesetById(1);
/// 
/// // Check tags
/// if (tileset.Tags.Contains("ground"))
/// {
///     // Handle ground tileset
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class LDtkTileset
{
    /// <summary>
    /// Gets the unique identifier of the tileset.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the name of the tileset.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the number of tiles in each dimension.
    /// </summary>
    public Vect2 CellSize { get; }

    /// <summary>
    /// Gets the size of the tileset texture in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the relative path to the tileset image file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the size of each tile in pixels.
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// Gets the spacing between tiles in the texture.
    /// </summary>
    public int Spacing { get; }

    /// <summary>
    /// Gets the padding around tiles in the texture.
    /// </summary>
    public int Padding { get; }

    /// <summary>
    /// Gets the list of tags associated with the tileset.
    /// </summary>
    public List<string> Tags { get; }

    internal LDtkTileset(uint id, string name, Vect2 cellSize, Vect2 size,
        string path, int tileSize, int spacing, int padding, List<string> tags)
    {
        Id = id;
        Name = name;
        CellSize = cellSize;
        Size = size;
        Path = path;
        TileSize = tileSize;
        Spacing = spacing;
        Padding = padding;
        Tags = tags;
    }

    internal static List<LDtkTileset> Process(JsonElement e)
    {
        var result = new List<LDtkTileset>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var cWidth = t.GetPropertyOrDefault<int>("__cWid");
            var cHeight = t.GetPropertyOrDefault<int>("__cHei");
            var name = t.GetPropertyOrDefault<string>("identifier");
            var id = t.GetPropertyOrDefault<uint>("uid");
            var path = t.GetPropertyOrDefault("relPath", string.Empty);
            var pxWid = t.GetPropertyOrDefault<int>("pxWid");
            var pxHei = t.GetPropertyOrDefault<int>("pxHei");
            var tileSize = t.GetPropertyOrDefault<int>("tileGridSize");
            var spacing = t.GetPropertyOrDefault<int>("spacing");
            var padding = t.GetPropertyOrDefault<int>("padding");
            var tags = t.GetProperty("enumTags").EnumerateArray()
                .Where(x => x.ValueKind != JsonValueKind.Null)
                .Select(x => x.GetString()!)
                .ToList();

            result.Add(
                new LDtkTileset(
                    id,
                    name,
                    new(cWidth, cHeight),
                    new(pxWid, pxHei),
                    path,
                    tileSize,
                    spacing,
                    padding,
                    tags
                )
            );
        }

        return result;
    }
}