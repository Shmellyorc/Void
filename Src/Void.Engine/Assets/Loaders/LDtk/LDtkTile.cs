// ============================================================================
//  LDtkTile.cs
// ============================================================================
//  Tile references parsed from LDtk field values.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Identifies a tile inside an LDtk tileset.
/// </summary>
public readonly struct LDtkTile
{
    /// <summary>
    /// Gets the LDtk tileset UID containing the tile.
    /// </summary>
    public int TilesetId { get; }

    /// <summary>
    /// Gets the tile region in source-texture pixels.
    /// </summary>
    public Rect2 Source { get; }

    internal LDtkTile(int tilesetId, Rect2 source)
    {
        TilesetId = tilesetId;
        Source = source;
    }

    internal static LDtkTile Process(JsonElement e)
    {
        var tilesetId = e.GetPropertyOrDefault<int>("tilesetUid");
        var x = e.GetPropertyOrDefault<int>("x");
        var y = e.GetPropertyOrDefault<int>("y");
        var w = e.GetPropertyOrDefault<int>("w");
        var h = e.GetPropertyOrDefault<int>("h");

        return new LDtkTile(tilesetId, new(x, y, w, h));
    }
}
