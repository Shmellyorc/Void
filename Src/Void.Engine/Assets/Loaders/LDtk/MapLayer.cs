// ============================================================================
//  MapLayer.cs
// ============================================================================
//  Layer metadata and parsed instances from an LDtk level.
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
/// Identifies the kind of data stored by an LDtk layer.
/// </summary>
public enum LDtkLayerType
{
    /// <summary>
    /// No recognized layer type.
    /// </summary>
    None,

    /// <summary>
    /// An integer-grid layer.
    /// </summary>
    IntGrid,

    /// <summary>
    /// An entity-instance layer.
    /// </summary>
    Entities,

    /// <summary>
    /// A manually placed tile layer.
    /// </summary>
    Tiles,

    /// <summary>
    /// An automatically generated tile layer.
    /// </summary>
    AutoLayer
}

/// <summary>
/// Represents one parsed layer from an LDtk level.
/// </summary>
/// <remarks>
/// The concrete objects exposed through <see cref="Instances"/> depend on
/// <see cref="Type"/>. Use <see cref="InstanceAs{T}"/> to retrieve only the
/// instance type you need.
/// </remarks>
public sealed class MapLayer
{
    /// <summary>
    /// Gets the LDtk layer identifier.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the kind of data stored by this layer.
    /// </summary>
    public LDtkLayerType Type { get; }

    /// <summary>
    /// Gets the layer dimensions measured in grid cells.
    /// </summary>
    public Vect2 GridSize { get; }

    /// <summary>
    /// Gets the layer grid size in pixels.
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// Gets the LDtk layer opacity.
    /// </summary>
    public float Opacity { get; }

    /// <summary>
    /// Gets the total pixel offset reported by LDtk.
    /// </summary>
    public Vect2 TotalOffset { get; }

    /// <summary>
    /// Gets the LDtk tileset UID used by this layer, or zero when none is assigned.
    /// </summary>
    public uint TilesetId { get; }

    /// <summary>
    /// Gets the LDtk-relative tileset path for this layer.
    /// </summary>
    public string TilesetPath { get; }

    /// <summary>
    /// Gets the LDtk instance ID of the layer.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the numeric LDtk level ID stored on the layer instance.
    /// </summary>
    public int LevelId { get; }

    /// <summary>
    /// Gets the layer's local pixel offset.
    /// </summary>
    public Vect2 Offset { get; }

    /// <summary>
    /// Gets whether the layer is marked visible in LDtk.
    /// </summary>
    public bool Visible { get; }

    /// <summary>
    /// Gets the parsed entities, tiles, or integer-grid entries for this layer.
    /// </summary>
    public IReadOnlyList<ILDtkInstance> Instances { get; }

    internal MapLayer(string name, LDtkLayerType type, Vect2 gridSize, int tileSize, float opacity,
        Vect2 totalOffset, uint tilesetId, string tilesetPath, string id, int levelId, Vect2 offset,
        bool visible, List<ILDtkInstance> instances)
    {
        Name = name;
        Type = type;
        GridSize = gridSize;
        TileSize = tileSize;
        Opacity = opacity;
        TotalOffset = totalOffset;
        TilesetId = tilesetId;
        TilesetPath = tilesetPath;
        Id = id;
        LevelId = levelId;
        Offset = offset;
        Visible = visible;
        Instances = instances;
    }

    /// <summary>
    /// Returns the instances in this layer that are assignable to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The LDtk instance type to retrieve.</typeparam>
    /// <returns>A new read-only list containing matching instances.</returns>
    public IReadOnlyList<T> InstanceAs<T>() where T : ILDtkInstance => [.. Instances.OfType<T>()];

    internal static List<MapLayer> Process(JsonElement e)
    {
        var result = new List<MapLayer>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var name = t.GetPropertyOrDefault("__identifier", string.Empty);
            var type = Enum.Parse<LDtkLayerType>(t.GetPropertyOrDefault("__type", "None"), true);
            var cX = t.GetPropertyOrDefault<int>("__cWid");
            var cY = t.GetPropertyOrDefault<int>("__cHei");
            var tileSize = t.GetPropertyOrDefault<int>("__gridSize");
            var opacity = t.GetPropertyOrDefault<float>("__opacity");
            var totalOffsetX = t.GetPropertyOrDefault<int>("__pxTotalOffsetX");
            var totalOffsetY = t.GetPropertyOrDefault<int>("__pxTotalOffsetY");
            var tilesetId = t.GetPropertyOrDefault("__tilesetDefUid", 0u);
            var tilesetPath = t.GetPropertyOrDefault("__tilesetRelPath", string.Empty);
            var id = t.GetPropertyOrDefault("iid", string.Empty);
            var levelId = t.GetPropertyOrDefault<int>("levelId");
            var offsetX = t.GetPropertyOrDefault<int>("pxOffsetX");
            var offsetY = t.GetPropertyOrDefault<int>("pxOffsetY");
            var visible = t.GetPropertyOrDefault<bool>("visible");
            var gridSize = new Vect2(cX, cY);

            List<ILDtkInstance> instResult = type switch
            {
                LDtkLayerType.IntGrid => LDtkIntGridInstance.Process(t.GetProperty("intGridCsv"), gridSize, tileSize),
                LDtkLayerType.Entities => LDtkEntityInstance.Process(t.GetProperty("entityInstances")),
                LDtkLayerType.Tiles => LDtkTileInstance.Process(t.GetProperty("gridTiles"), tileSize),
                LDtkLayerType.AutoLayer => LDtkTileInstance.Process(t.GetProperty("autoLayerTiles"), tileSize),
                _ => throw new ArgumentException($"Unable to find Map layer type, it is '{type}'.")
            };

            result.Add(
                new MapLayer(
                    name,
                    type,
                    gridSize,
                    tileSize,
                    opacity,
                    new(totalOffsetX, totalOffsetY),
                    tilesetId,
                    tilesetPath,
                    id,
                    levelId,
                    new(offsetX, offsetY),
                    visible,
                    instResult
                )
            );
        }

        return result;
    }
}
