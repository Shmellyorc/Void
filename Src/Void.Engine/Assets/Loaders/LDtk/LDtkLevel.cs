// ============================================================================
//  LDtkLevel.cs
// ============================================================================
//  Level data parsed from an LDtk project.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a level loaded from an LDtk project.
/// </summary>
/// <remarks>
/// A level contains its world placement, dimensions, background data, neighbouring
/// level references, parsed layers, and custom field settings.
/// </remarks>
public sealed class LDtkLevel
{
    /// <summary>
    /// Gets the LDtk level identifier.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the LDtk instance ID for the level.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the level position in LDtk world pixels.
    /// </summary>
    public Vect2 Coords { get; }

    /// <summary>
    /// Gets the level depth in the LDtk world.
    /// </summary>
    public int WorldDepth { get; }

    /// <summary>
    /// Gets the level dimensions in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the level dimensions measured in default-grid cells.
    /// </summary>
    public Vect2 GridSize { get; }

    /// <summary>
    /// Gets the resolved background color for the level.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Gets the LDtk-relative path to the level background image, if one is assigned.
    /// </summary>
    public string BgPath { get; }

    /// <summary>
    /// Gets the background image position in pixels.
    /// </summary>
    public Vect2 BgPosition { get; }

    /// <summary>
    /// Gets the normalized background pivot reported by LDtk.
    /// </summary>
    public Vect2 BgPivot { get; }

    /// <summary>
    /// Gets the neighbouring level references for this level.
    /// </summary>
    public MapNeighbour Neighbours { get; }

    /// <summary>
    /// Gets the layers contained in this level.
    /// </summary>
    public IReadOnlyList<MapLayer> Layers { get; }

    /// <summary>
    /// Gets the custom level fields keyed by VOID's hash of the LDtk field name.
    /// </summary>
    /// <remarks>
    /// Use the static helpers on <see cref="LDtkSetting"/> to access settings by
    /// their original LDtk field names.
    /// </remarks>
    public IReadOnlyDictionary<uint, LDtkSetting> Settings { get; }

    internal LDtkLevel(string name, string id, Vect2 coords, int worthDepth, Vect2 size,
        Vect2 gridSize, Color color, string bgPath, Vect2 bgPosition, Vect2 bgPivot,
        MapNeighbour neighbours, List<MapLayer> layers, Dictionary<uint, LDtkSetting> settings)
    {
        Name = name;
        Id = id;
        Coords = coords;
        WorldDepth = worthDepth;
        Size = size;
        GridSize = gridSize;
        Color = color;
        BgPath = bgPath;
        BgPosition = bgPosition;
        BgPivot = bgPivot;
        Neighbours = neighbours;
        Layers = layers;
        Settings = settings;
    }

    internal static List<LDtkLevel> Process(JsonElement e, int tileSize)
    {
        var result = new List<LDtkLevel>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            Color color;
            var name = t.GetPropertyOrDefault("identifier", string.Empty);
            var id = t.GetPropertyOrDefault("iid", string.Empty);
            var worldX = t.GetPropertyOrDefault<int>("worldX");
            var worldY = t.GetPropertyOrDefault<int>("worldY");
            var worldDepth = t.GetPropertyOrDefault<int>("worldDepth");
            var pxX = t.GetPropertyOrDefault<int>("pxWid");
            var pxY = t.GetPropertyOrDefault<int>("pxHei");
            var bgRelPath = t.GetPropertyOrDefault("bgRelPath", string.Empty);
            var bgPivotX = t.GetPropertyOrDefault<float>("bgPivotX");
            var bgPivotY = t.GetPropertyOrDefault<float>("bgPivotY");
            var size = new Vect2(pxX, pxY);
            var gridSize = Vect2.Floor(size / tileSize);
            var pxBgPos = Vect2.Zero;

            if (t.TryGetProperty("bgColor", out var bgProp) && bgProp.ValueKind != JsonValueKind.Null)
                color = new Color(t.GetPropertyOrDefault("bgColor", "#ffffff"));
            else
                color = new Color(t.GetPropertyOrDefault("__bgColor", "#ffffff"));

            if (t.TryGetProperty("bgPos", out var bgPos) && bgPos.ValueKind != JsonValueKind.Null)
            {
                var bgElem = bgPos.EnumerateArray();
                pxBgPos = new Vect2(bgElem.First().GetInt32(), bgElem.Last().GetInt32());
            }

            var neighbours = MapNeighbour.Process(t.GetProperty("__neighbours"));
            var settings = JsonHelper.GetSettings(t.GetProperty("fieldInstances"));
            var layers = MapLayer.Process(t.GetProperty("layerInstances"));

            result.Add(
                new LDtkLevel(name, id, new(worldX, worldY), worldDepth, size, gridSize,
                color, bgRelPath, pxBgPos, new(bgPivotX, bgPivotY), neighbours, layers, settings)
            );
        }

        return result;
    }
}
