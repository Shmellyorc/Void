// ============================================================================
//  MapLayer.cs
// ============================================================================
//  Represents a layer within an LDtk level, containing instances of tiles,
//  entities, or integer grid data.
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
/// Defines the type of an LDtk layer.
/// </summary>
public enum LDtkLayerType
{
    /// <summary>
    /// No layer type specified.
    /// </summary>
    None,

    /// <summary>
    /// An integer grid layer containing cell values.
    /// </summary>
    IntGrid,

    /// <summary>
    /// An entity layer containing entity instances.
    /// </summary>
    Entities,

    /// <summary>
    /// A tile layer containing tile instances.
    /// </summary>
    Tiles,

    /// <summary>
    /// An auto-layer containing automatically placed tiles.
    /// </summary>
    AutoLayer
}

/// <summary>
/// Represents a layer within an LDtk level, containing instances of tiles,
/// entities, or integer grid data.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MapLayer"/> class contains all the data for a single layer
/// in an LDtk level. It includes metadata such as name, type, opacity, and
/// the actual instances (entities, tiles, or int grid values).
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Name"/> - The display name of the layer</description></item>
///   <item><description><see cref="Type"/> - The layer type (IntGrid, Entities, Tiles, AutoLayer)</description></item>
///   <item><description><see cref="GridSize"/> - The size of the layer grid in tiles</description></item>
///   <item><description><see cref="TileSize"/> - The size of each tile in pixels</description></item>
///   <item><description><see cref="Opacity"/> - The opacity of the layer</description></item>
///   <item><description><see cref="TilesetId"/> - The ID of the tileset used by this layer</description></item>
///   <item><description><see cref="TilesetPath"/> - The path to the tileset image</description></item>
///   <item><description><see cref="Instances"/> - The layer instances (entities, tiles, or int grid)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get a layer from a level
/// var layer = level.Layers.FirstOrDefault(l => l.Name == "Entities");
/// 
/// // Check the layer type
/// if (layer.Type == LDtkLayerType.Entities)
/// {
///     // Get all entities in the layer
///     var entities = layer.InstanceAs&lt;LDtkEntityInstance&gt;();
///     
///     foreach (var entity in entities)
///     {
///         Console.WriteLine($"Entity: {entity.Name} at {entity.Position}");
///     }
/// }
/// else if (layer.Type == LDtkLayerType.IntGrid)
/// {
///     // Get all int grid values
///     var gridValues = layer.InstanceAs&lt;LDtkIntGridInstance&gt;();
/// }
/// 
/// // Check layer visibility
/// if (layer.Visible)
/// {
///     // Render the layer
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class MapLayer
{
    /// <summary>
    /// Gets the display name of the layer.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the layer.
    /// </summary>
    public LDtkLayerType Type { get; }

    /// <summary>
    /// Gets the size of the layer grid in tiles.
    /// </summary>
    public Vect2 GridSize { get; }

    /// <summary>
    /// Gets the size of each tile in pixels.
    /// </summary>
    public int TileSize { get; }

    /// <summary>
    /// Gets the opacity of the layer.
    /// </summary>
    public float Opacity { get; }

    /// <summary>
    /// Gets the total pixel offset of the layer.
    /// </summary>
    public Vect2 TotalOffset { get; }

    /// <summary>
    /// Gets the ID of the tileset used by this layer.
    /// </summary>
    public uint TilesetId { get; }

    /// <summary>
    /// Gets the path to the tileset image.
    /// </summary>
    public string TilesetPath { get; }

    /// <summary>
    /// Gets the unique identifier of the layer.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the ID of the level containing this layer.
    /// </summary>
    public int LevelId { get; }

    /// <summary>
    /// Gets the pixel offset of the layer within the level.
    /// </summary>
    public Vect2 Offset { get; }

    /// <summary>
    /// Gets a value indicating whether the layer is visible.
    /// </summary>
    public bool Visible { get; }

    /// <summary>
    /// Gets the instances contained in this layer (entities, tiles, or int grid).
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
    /// Gets the instances in this layer cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of instances to retrieve.</typeparam>
    /// <returns>A read-only list of instances cast to type T.</returns>
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
                LDtkLayerType.IntGrid => LDtkIntGridInstance.Process(t.GetProperty("intGridCsv"), gridSize),
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