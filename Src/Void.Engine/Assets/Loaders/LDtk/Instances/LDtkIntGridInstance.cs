// ============================================================================
//  LDtkIntGridInstance.cs
// ============================================================================
//  Represents one cell from an LDtk integer-grid layer.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents one value from an LDtk integer-grid layer.
/// </summary>
public sealed class LDtkIntGridInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the integer value stored in this grid cell.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Converts the cell value to the specified enum type.
    /// </summary>
    /// <typeparam name="T">The enum type represented by the integer-grid values.</typeparam>
    /// <returns>The enum value whose underlying numeric value matches <see cref="Index"/>.</returns>
    public T IndexAsEnum<T>() where T : Enum => (T)Enum.ToObject(typeof(T), Index);

    /// <summary>
    /// Gets whether the cell contains a value greater than zero.
    /// </summary>
    /// <remarks>
    /// This is a convenience interpretation of the integer-grid value. A project may
    /// use <see cref="Index"/> directly when nonzero values do not represent solidity.
    /// </remarks>
    public bool IsSolid => Index > 0;

    /// <summary>
    /// Gets the cell location in layer grid coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the calculated position stored for the cell.
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
