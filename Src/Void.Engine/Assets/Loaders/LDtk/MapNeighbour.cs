// ============================================================================
//  MapNeighbour.cs
// ============================================================================
//  Neighbouring-level references parsed from LDtk world data.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Identifies the direction of a neighbouring LDtk level.
/// </summary>
public enum LDtkNeighbourDirection
{
    /// <summary>
    /// No recognized direction.
    /// </summary>
    None,

    /// <summary>
    /// North.
    /// </summary>
    North,

    /// <summary>
    /// Northeast.
    /// </summary>
    NorthEast,

    /// <summary>
    /// East.
    /// </summary>
    East,

    /// <summary>
    /// Southeast.
    /// </summary>
    SouthEast,

    /// <summary>
    /// South.
    /// </summary>
    South,

    /// <summary>
    /// Southwest.
    /// </summary>
    SouthWest,

    /// <summary>
    /// West.
    /// </summary>
    West,

    /// <summary>
    /// Northwest.
    /// </summary>
    NorthWest
}

/// <summary>
/// Provides the neighbouring level instance IDs reported by LDtk.
/// </summary>
/// <remarks>
/// Direction properties return an empty string when no neighbour exists in that
/// direction. <see cref="Neighbours"/> exposes the same data keyed by VOID's
/// hash of <see cref="LDtkNeighbourDirection"/>.
/// </remarks>
public sealed class MapNeighbour
{
    /// <summary>
    /// Gets the level instance ID to the north, or an empty string when absent.
    /// </summary>
    public string North => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.North), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the northeast, or an empty string when absent.
    /// </summary>
    public string NorthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthEast), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the east, or an empty string when absent.
    /// </summary>
    public string East => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.East), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the southeast, or an empty string when absent.
    /// </summary>
    public string SouthEast => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthEast), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the south, or an empty string when absent.
    /// </summary>
    public string South => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.South), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the southwest, or an empty string when absent.
    /// </summary>
    public string SouthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.SouthWest), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the west, or an empty string when absent.
    /// </summary>
    public string West => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.West), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets the level instance ID to the northwest, or an empty string when absent.
    /// </summary>
    public string NorthWest => Neighbours.TryGetValue(HashHelper.Cache32(LDtkNeighbourDirection.NorthWest), out var v) ? v : string.Empty;

    /// <summary>
    /// Gets all neighbouring level IDs keyed by hashed direction.
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
