// ============================================================================
//  LDtkTile.cs
// ============================================================================
//  Represents a tile reference within an LDtk tileset.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a tile reference within an LDtk tileset.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkTile"/> structure identifies a specific tile within a
/// tileset by its tileset ID and source rectangle. It is used in LDtk settings
/// and tile instances to reference individual tiles.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="TilesetId"/> - The unique identifier of the tileset containing the tile</description></item>
///   <item><description><see cref="Source"/> - The rectangular region of the tile within the tileset texture</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get a tile from a setting
/// var tile = LDtkSetting.GetTileSetting(settings, "TileName");
/// 
/// // Access tile properties
/// int tilesetId = tile.TilesetId;
/// Rect2 source = tile.Source;
/// 
/// // Get the tileset from the map
/// var tileset = map.GetTilesetById((uint)tile.TilesetId);
/// 
/// // Load the tile texture
/// var texture = AssetManager.Instance.LoadTexture(tileset.Path);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe.
/// </para>
/// </remarks>
public readonly struct LDtkTile
{
    /// <summary>
    /// Gets the unique identifier of the tileset containing this tile.
    /// </summary>
    public int TilesetId { get; }

    /// <summary>
    /// Gets the rectangular region of the tile within the tileset texture.
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