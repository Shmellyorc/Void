// ============================================================================
//  LDtkIntGridInstance.cs
// ============================================================================
//  Represents an integer grid value within an LDtk level layer.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents an integer grid value within an LDtk level layer.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkIntGridInstance"/> class represents a single cell in an
/// integer grid layer. Each cell contains an integer value that can be used
/// to represent terrain types, obstacles, or other grid-based data.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Index"/> - The integer value at this grid position</description></item>
///   <item><description><see cref="IsSolid"/> - True if the value is greater than zero</description></item>
///   <item><description><see cref="Location"/> - The grid location of the cell</description></item>
///   <item><description><see cref="Position"/> - The pixel position of the cell</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get all int grid values from a layer
/// var gridValues = layer.InstanceAs&lt;LDtkIntGridInstance&gt;();
/// 
/// foreach (var cell in gridValues)
/// {
///     // Check if the cell is solid
///     if (cell.IsSolid)
///     {
///         // Handle solid cell
///     }
///     
///     // Get the index as an enum
///     var terrainType = cell.IndexAsEnum&lt;TerrainType&gt;();
///     
///     // Access grid position
///     Vect2 gridPos = cell.Location;
///     Vect2 worldPos = cell.Position;
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class LDtkIntGridInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the integer value at this grid position.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the index value as an enum.
    /// </summary>
    /// <typeparam name="T">The enum type to convert the index to.</typeparam>
    /// <returns>The index value cast to the specified enum type.</returns>
    public T IndexAsEnum<T>() where T : Enum => (T)Enum.ToObject(typeof(T), Index);

    /// <summary>
    /// Gets a value indicating whether this cell is solid (value > 0).
    /// </summary>
    public bool IsSolid => Index > 0;

    /// <summary>
    /// Gets the grid location of the cell in tile coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the pixel position of the cell in world coordinates.
    /// </summary>
    public Vect2 Position { get; }

    internal LDtkIntGridInstance(int index, Vect2 location, Vect2 position)
    {
        Index = index;
        Location = location;
        Position = position;
    }

    internal static List<ILDtkInstance> Process(JsonElement e, Vect2 gridSize)
    {
        var result = new List<ILDtkInstance>(e.GetArrayLength());
        var index = 0;

        foreach (var t in e.EnumerateArray())
        {
            var location = new Vect2(index % (int)gridSize.X, index / (int)gridSize.X);
            var position = gridSize * location;

            result.Add(new LDtkIntGridInstance(t.GetInt32(), location, position));

            index++;
        }

        return result;
    }
}