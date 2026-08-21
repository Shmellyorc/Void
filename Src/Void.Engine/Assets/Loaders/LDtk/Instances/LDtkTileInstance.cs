// ============================================================================
//  LDtkTileInstance.cs
// ============================================================================
//  Represents a tile instance within an LDtk level layer.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents a tile instance within an LDtk level layer.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkTileInstance"/> class represents a single tile placed
/// in a tile layer or auto-layer. It contains the tile's source rectangle,
/// flip effects, alpha, and position.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Source"/> - The source rectangle within the tileset texture</description></item>
///   <item><description><see cref="Effects"/> - The texture flip effects (horizontal, vertical, or both)</description></item>
///   <item><description><see cref="Tile"/> - The tile index</description></item>
///   <item><description><see cref="Alpha"/> - The alpha transparency of the tile</description></item>
///   <item><description><see cref="Location"/> - The grid location of the tile</description></item>
///   <item><description><see cref="Position"/> - The pixel position of the tile</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Texture Effects:</b>
/// The <see cref="TextureEffects"/> flags indicate how the tile texture should be flipped:
/// <list type="bullet">
///   <item><description><see cref="TextureEffects.None"/> - No flipping</description></item>
///   <item><description><see cref="TextureEffects.Horizontal"/> - Horizontal flip</description></item>
///   <item><description><see cref="TextureEffects.Vertical"/> - Vertical flip</description></item>
///   <item><description><see cref="TextureEffects.Horizontal"/> | <see cref="TextureEffects.Vertical"/> - Both horizontal and vertical flip</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get all tiles from a layer
/// var tiles = layer.InstanceAs&lt;LDtkTileInstance&gt;();
/// 
/// foreach (var tile in tiles)
/// {
///     // Get the source rectangle in the tileset
///     Rect2 source = tile.Source;
///     
///     // Check if the tile is flipped
///     if (tile.Effects.HasFlag(TextureEffects.Horizontal))
///     {
///         // Render flipped horizontally
///     }
///     
///     // Get position
///     Vect2 position = tile.Position;
///     
///     // Apply alpha
///     float alpha = tile.Alpha;
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class LDtkTileInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the source rectangle within the tileset texture.
    /// </summary>
    public Rect2 Source { get; }

    /// <summary>
    /// Gets the texture flip effects applied to this tile.
    /// </summary>
    public TextureEffects Effects { get; }

    /// <summary>
    /// Gets the tile index.
    /// </summary>
    public int Tile { get; }

    /// <summary>
    /// Gets the alpha transparency of the tile.
    /// </summary>
    public float Alpha { get; }

    /// <summary>
    /// Gets the grid location of the tile in tile coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the pixel position of the tile in world coordinates.
    /// </summary>
    public Vect2 Position { get; }

    internal LDtkTileInstance(Rect2 source, TextureEffects effects, int tile, float alpha,
        Vect2 location, Vect2 position)
    {
        Source = source;
        Effects = effects;
        Tile = tile;
        Alpha = alpha;
        Location = location;
        Position = position;
    }

    internal static List<ILDtkInstance> Process(JsonElement e, int tileSize)
    {
        var result = new List<ILDtkInstance>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var position = t.GetPosition("px");
            var src = t.GetPosition("src");
            var flag = t.GetPropertyOrDefault<int>("f");
            var tile = t.GetPropertyOrDefault<int>("t");
            var alpha = t.GetPropertyOrDefault<float>("a");
            var location = Vect2.Floor(position / tileSize);
            var srcRect = new Rect2(src, new(tileSize));

            TextureEffects effects = flag switch
            {
                1 => TextureEffects.Horizontal,
                2 => TextureEffects.Vertical,
                3 => TextureEffects.Horizontal | TextureEffects.Vertical,
                _ => TextureEffects.None
            };

            result.Add(new LDtkTileInstance(srcRect, effects, tile, alpha, location, position));
        }

        return result;
    }
}