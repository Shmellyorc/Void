// ============================================================================
//  MapNeighbour.cs
// ============================================================================
//  Represents the neighboring levels of an LDtk level in the world map.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents the possible directions for neighboring levels in the LDtk world map.
/// </summary>
public enum LDtkNeighbourDirection
{
    /// <summary>
    /// No direction; used when there is no neighboring level.
    /// </summary>
    None,

    /// <summary>
    /// The level directly to the north (up).
    /// </summary>
    North,

    /// <summary>
    /// The level to the northeast (up and right).
    /// </summary>
    NorthEast,

    /// <summary>
    /// The level directly to the east (right).
    /// </summary>
    East,

    /// <summary>
    /// The level to the southeast (down and right).
    /// </summary>
    SouthEast,

    /// <summary>
    /// The level directly to the south (down).
    /// </summary>
    South,

    /// <summary>
    /// The level to the southwest (down and left).
    /// </summary>
    SouthWest,

    /// <summary>
    /// The level directly to the west (left).
    /// </summary>
    West,

    /// <summary>
    /// The level to the northwest (up and left).
    /// </summary>
    NorthWest
}

/// <summary>
/// Represents the neighboring levels of an LDtk level in the world map.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MapNeighbour"/> class provides access to the IDs of
/// neighboring levels in each of the eight cardinal and intercardinal directions.
/// This is used to navigate between connected levels in the LDtk world map.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="North"/> - ID of the level to the north</description></item>
///   <item><description><see cref="NorthEast"/> - ID of the level to the northeast</description></item>
///   <item><description><see cref="East"/> - ID of the level to the east</description></item>
///   <item><description><see cref="SouthEast"/> - ID of the level to the southeast</description></item>
///   <item><description><see cref="South"/> - ID of the level to the south</description></item>
///   <item><description><see cref="SouthWest"/> - ID of the level to the southwest</description></item>
///   <item><description><see cref="West"/> - ID of the level to the west</description></item>
///   <item><description><see cref="NorthWest"/> - ID of the level to the northwest</description></item>
///   <item><description><see cref="Neighbours"/> - Dictionary of all neighboring levels by direction hash</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the neighbours for a level
/// var neighbours = level.Neighbours;
/// 
/// // Check for a specific neighbour
/// if (!string.IsNullOrEmpty(neighbours.North))
/// {
///     // Get the level to the north
///     var northLevel = map.GetLevelById(neighbours.North);
/// }
/// 
/// // Iterate over all neighbours
/// foreach (var (directionHash, levelId) in neighbours.Neighbours)
/// {
///     // Convert hash back to direction if needed
///     Console.WriteLine($"Level {levelId} is in direction {directionHash}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class MapNeighbour
{
    /// <summary>
    /// Gets the ID of the level to the north.
    /// </summary>
    public string North => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.North), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the northeast.
    /// </summary>
    public string NorthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthEast), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the east.
    /// </summary>
    public string East => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.East), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the southeast.
    /// </summary>
    public string SouthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthEast), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the south.
    /// </summary>
    public string South => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.South), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the southwest.
    /// </summary>
    public string SouthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthWest), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the west.
    /// </summary>
    public string West => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.West), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the ID of the level to the northwest.
    /// </summary>
    public string NorthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthWest), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets a dictionary of all neighboring levels keyed by direction hash.
    /// </summary>
    public IReadOnlyDictionary<uint, string> Neighbours { get; }

    internal MapNeighbour(Dictionary<uint, string> neighbours) =>
        Neighbours = neighbours;

    internal static MapNeighbour Process(JsonElement e)
    {
        var result = new Dictionary<uint, string>(e.GetArrayLength());

        foreach (var element in e.EnumerateArray())
        {
            (LDtkNeighbourDirection dir, string id) data = element.GetPropertyOrDefault("dir", string.Empty) switch
            {
                var v when v == "n" => (LDtkNeighbourDirection.North, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "ne" => (LDtkNeighbourDirection.NorthEast, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "e" => (LDtkNeighbourDirection.East, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "se" => (LDtkNeighbourDirection.SouthEast, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "s" => (LDtkNeighbourDirection.South, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "sw" => (LDtkNeighbourDirection.SouthWest, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "w" => (LDtkNeighbourDirection.West, element.GetPropertyOrDefault("levelIid", string.Empty)),
                var v when v == "nw" => (LDtkNeighbourDirection.NorthWest, element.GetPropertyOrDefault("levelIid", string.Empty)),
                _ => (LDtkNeighbourDirection.None, string.Empty)
            };

            if (data.dir == LDtkNeighbourDirection.None || string.IsNullOrWhiteSpace(data.id))
                continue;

            result[HashHelper.Cache32(data.dir)] = data.id;
        }

        return new MapNeighbour(result);
    }
}