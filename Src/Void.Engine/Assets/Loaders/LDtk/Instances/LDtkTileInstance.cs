// ============================================================================
//  LDtkTileInstance.cs
// ============================================================================
//  Represents a tile placed in an LDtk tile layer or auto-layer.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents a tile placed in an LDtk tile layer or auto-layer.
/// </summary>
public sealed class LDtkTileInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the source rectangle of the tile within its tileset texture.
    /// </summary>
    public Rect2 Source { get; }

    /// <summary>
    /// Gets the horizontal and vertical flip flags applied to the tile.
    /// </summary>
    public TextureEffects Effects { get; }

    /// <summary>
    /// Gets the LDtk tile index.
    /// </summary>
    public int Tile { get; }

    /// <summary>
    /// Gets the tile opacity reported by LDtk.
    /// </summary>
    public float Alpha { get; }

    /// <summary>
    /// Gets the tile location in layer grid coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the tile position in pixels within its level.
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
