// ============================================================================
//  LDtkTileset.cs
// ============================================================================
//  Tileset metadata parsed from an LDtk project.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Describes a tileset definition from an LDtk project.
/// </summary>
/// <remarks>
/// The tileset stores LDtk metadata only. Use <see cref="AssetManager.LoadTilesetTexture(LDtkMap, uint)"/>
/// when you need the corresponding VOID texture with LDtk path remapping applied.
/// </remarks>
public sealed class LDtkTileset
{
    /// <summary>
    /// Gets the LDtk tileset UID.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the tileset identifier from the LDtk project.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tileset dimensions measured in cells.
    /// </summary>
    public Vect2 CellSize { get; }

    /// <summary>
    /// Gets the tileset texture dimensions in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the LDtk-relative path to the tileset image.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the tile grid size in pixels.
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// Gets the spacing in pixels between tiles.
    /// </summary>
    public int Spacing { get; }

    /// <summary>
    /// Gets the padding in pixels around the tileset grid.
    /// </summary>
    public int Padding { get; }

    /// <summary>
    /// Gets the enum tags assigned to the tileset.
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
