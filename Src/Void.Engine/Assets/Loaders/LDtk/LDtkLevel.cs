// ============================================================================
//  LDtkLevel.cs
// ============================================================================
//  Represents a level within an LDtk map, containing layers, settings,
//  and metadata such as position, size, and background information.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a level within an LDtk map, containing layers, settings,
/// and metadata such as position, size, and background information.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkLevel"/> class contains all the data for a single level
/// in an LDtk project. It provides access to the level's name, ID, position,
/// size, background, neighboring levels, layers, and field settings.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Name"/> - The display name of the level</description></item>
///   <item><description><see cref="Id"/> - The unique identifier of the level</description></item>
///   <item><description><see cref="Coords"/> - The world coordinates of the level</description></item>
///   <item><description><see cref="WorldDepth"/> - The depth of the level in the world</description></item>
///   <item><description><see cref="Size"/> - The size of the level in pixels</description></item>
///   <item><description><see cref="GridSize"/> - The size of the level in tiles</description></item>
///   <item><description><see cref="Color"/> - The background color of the level</description></item>
///   <item><description><see cref="BgPath"/> - The path to the background image</description></item>
///   <item><description><see cref="Neighbours"/> - The neighboring levels</description></item>
///   <item><description><see cref="Layers"/> - The layers in the level</description></item>
///   <item><description><see cref="Settings"/> - The field settings of the level</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get a level by name or ID
/// var level = map.GetLevelByName("Level_01");
/// 
/// // Access level properties
/// Vect2 size = level.Size;
/// Vect2 gridSize = level.GridSize;
/// Color bgColor = level.Color;
/// 
/// // Iterate over layers
/// foreach (var layer in level.Layers)
/// {
///     if (layer.Type == LDtkLayerType.Entities)
///     {
///         var entities = layer.InstanceAs&lt;LDtkEntityInstance&gt;();
///     }
/// }
/// 
/// // Access level settings
/// if (LDtkSetting.TryGetStringSetting(level.Settings, "SettingName", out var value))
/// {
///     // Use the setting value
/// }
/// 
/// // Get neighbouring levels
/// if (!string.IsNullOrEmpty(level.Neighbours.North))
/// {
///     var northLevel = map.GetLevelById(level.Neighbours.North);
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class LDtkLevel
{
    /// <summary>
    /// Gets the display name of the level.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique identifier of the level.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the world coordinates of the level.
    /// </summary>
    public Vect2 Coords { get; }

    /// <summary>
    /// Gets the depth of the level in the world.
    /// </summary>
    public int WorldDepth { get; }

    /// <summary>
    /// Gets the size of the level in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the size of the level in tiles.
    /// </summary>
    public Vect2 GridSize { get; }

    /// <summary>
    /// Gets the background color of the level.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Gets the path to the background image.
    /// </summary>
    public string BgPath { get; }

    /// <summary>
    /// Gets the position of the background image.
    /// </summary>
    public Vect2 BgPosition { get; }

    /// <summary>
    /// Gets the pivot point of the background image.
    /// </summary>
    public Vect2 BgPivot { get; }

    /// <summary>
    /// Gets the neighboring levels of this level.
    /// </summary>
    public MapNeighbour Neighbours { get; }

    /// <summary>
    /// Gets the layers in this level.
    /// </summary>
    public IReadOnlyList<MapLayer> Layers { get; }

    /// <summary>
    /// Gets the field settings of this level.
    /// </summary>
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