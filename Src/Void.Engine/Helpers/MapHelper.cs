// ============================================================================
//  MapHelper.cs
// ============================================================================
//  Tile-grid coordinate conversion and spatial query helpers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides coordinate conversion and spatial-query helpers for tile-based maps.
/// </summary>
/// <remarks>
/// Tile coordinates are expected to represent whole grid cells even though VOID
/// uses <see cref="Vect2"/> as the coordinate container.
/// </remarks>
public static class MapHelper
{
    private static readonly Vect2[] CardinalDirections =
    [
        Vect2.Up,
        Vect2.Right,
        Vect2.Down,
        Vect2.Left
    ];

    /// <summary>Converts a tile coordinate to world-space pixels.</summary>
    /// <param name="location">Tile coordinate.</param>
    /// <param name="tilesize">Positive tile size in world units.</param>
    /// <returns>The tile's world-space position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tilesize"/> is not positive.</exception>
    public static Vect2 MapToWorld(in Vect2 location, int tilesize)
    {
        ValidateTileSize(tilesize, nameof(tilesize));
        return Vect2.Floor(location * tilesize);
    }

    /// <summary>Converts a world-space position to a tile coordinate.</summary>
    /// <param name="position">World-space position.</param>
    /// <param name="tilesize">Positive tile size in world units.</param>
    /// <returns>The tile containing the position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tilesize"/> is not positive.</exception>
    public static Vect2 WorldToMap(in Vect2 position, int tilesize)
    {
        ValidateTileSize(tilesize, nameof(tilesize));
        return Vect2.Floor(position / tilesize);
    }

    /// <summary>Converts a flat tile index to a two-dimensional tile coordinate.</summary>
    /// <param name="index">Flat tile index.</param>
    /// <param name="mapWidth">Positive map width in tiles.</param>
    /// <returns>The corresponding tile coordinate.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mapWidth"/> is not positive.</exception>
    public static Vect2 To2D(int index, int mapWidth)
    {
        ValidateMapWidth(mapWidth);
        return new Vect2(index % mapWidth, index / mapWidth);
    }

    /// <summary>Converts a two-dimensional tile coordinate to a flat tile index.</summary>
    /// <param name="location">Tile coordinate.</param>
    /// <param name="mapWidth">Positive map width in tiles.</param>
    /// <returns>The corresponding flat tile index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mapWidth"/> is not positive.</exception>
    public static int To1D(Vect2 location, int mapWidth)
    {
        ValidateMapWidth(mapWidth);
        return (int)location.Y * mapWidth + (int)location.X;
    }

    /// <summary>Converts a world-space position directly to a flat tile index.</summary>
    public static int WorldToIndex(Vect2 position, int tileSize, int mapWidth)
    {
        Vect2 tile = WorldToMap(position, tileSize);
        return To1D(tile, mapWidth);
    }

    /// <summary>Converts a flat tile index to the world-space top-left of that tile.</summary>
    public static Vect2 IndexToWorld(int index, int tileSize, int mapWidth)
    {
        Vect2 tile = To2D(index, mapWidth);
        return MapToWorld(tile, tileSize);
    }

    /// <summary>Gets every tile touched by a world-space size starting at a tile coordinate.</summary>
    /// <param name="size">World-space width and height to cover.</param>
    /// <param name="location">Top-left tile coordinate.</param>
    /// <param name="tileSize">Positive tile size in world units.</param>
    /// <returns>Tile coordinates covering the requested area, including partially covered edge tiles.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tileSize"/> is not positive.</exception>
    public static List<Vect2> ToMap(Vect2 size, Vect2 location, int tileSize)
    {
        ValidateTileSize(tileSize, nameof(tileSize));

        if (size.X <= 0f || size.Y <= 0f)
            return [];

        int xSize = (int)MathF.Ceiling(size.X / tileSize);
        int ySize = (int)MathF.Ceiling(size.Y / tileSize);
        var result = new List<Vect2>(xSize * ySize);

        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
                result.Add(location + new Vect2(x, y));
        }

        return result;
    }

    /// <summary>Gets all tiles whose centers lie within a circular tile radius.</summary>
    /// <param name="center">Center tile.</param>
    /// <param name="radius">Nonnegative radius in tiles.</param>
    /// <returns>Tiles inside or on the radius.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radius"/> is negative.</exception>
    public static List<Vect2> ToCircle(Vect2 center, int radius)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        var result = new List<Vect2>((radius * 2 + 1) * (radius * 2 + 1));
        int radiusSquared = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    result.Add(center + new Vect2(x, y));
            }
        }

        return result;
    }

    /// <summary>Gets all tiles in a circular ring.</summary>
    /// <param name="center">Center tile.</param>
    /// <param name="innerRadius">Nonnegative exclusive inner radius.</param>
    /// <param name="outerRadius">Inclusive outer radius greater than or equal to <paramref name="innerRadius"/>.</param>
    /// <returns>Tiles in the requested ring.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid radius values.</exception>
    public static List<Vect2> ToRing(Vect2 center, int innerRadius, int outerRadius)
    {
        if (innerRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(innerRadius));
        if (outerRadius < innerRadius)
            throw new ArgumentOutOfRangeException(nameof(outerRadius));

        var result = new List<Vect2>();
        int outerSquared = outerRadius * outerRadius;
        int innerSquared = innerRadius * innerRadius;

        for (int y = -outerRadius; y <= outerRadius; y++)
        {
            for (int x = -outerRadius; x <= outerRadius; x++)
            {
                int distanceSquared = x * x + y * y;
                if (distanceSquared <= outerSquared && distanceSquared > innerSquared)
                    result.Add(center + new Vect2(x, y));
            }
        }

        return result;
    }

    /// <summary>Gets the tiles on a Bresenham line between two tile coordinates.</summary>
    public static List<Vect2> ToLine(Vect2 start, Vect2 end)
    {
        var result = new List<Vect2>();
        int x0 = (int)start.X;
        int y0 = (int)start.Y;
        int x1 = (int)end.X;
        int y1 = (int)end.Y;
        int dx = Math.Abs(x1 - x0);
        int dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            result.Add(new Vect2(x0, y0));
            if (x0 == x1 && y0 == y1)
                break;

            int doubledError = error * 2;
            if (doubledError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubledError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }

        return result;
    }

    /// <summary>Gets the unique border tiles of a rectangular tile area.</summary>
    /// <param name="start">Top-left tile.</param>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
    /// <returns>The border tiles, or an empty list for a nonpositive size.</returns>
    public static List<Vect2> ToEdge(Vect2 start, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return [];

        int capacity = width == 1 || height == 1
            ? width * height
            : 2 * width + 2 * height - 4;
        var result = new List<Vect2>(capacity);

        for (int x = 0; x < width; x++)
        {
            result.Add(start + new Vect2(x, 0));
            if (height > 1)
                result.Add(start + new Vect2(x, height - 1));
        }

        for (int y = 1; y < height - 1; y++)
        {
            result.Add(start + new Vect2(0, y));
            if (width > 1)
                result.Add(start + new Vect2(width - 1, y));
        }

        return result;
    }

    /// <summary>Checks whether a tile coordinate lies inside a map.</summary>
    public static bool IsInBounds(Vect2 tile, int mapWidth, int mapHeight)
        => mapWidth > 0 && mapHeight > 0 &&
           tile.X >= 0 && tile.X < mapWidth && tile.Y >= 0 && tile.Y < mapHeight;

    /// <summary>Calculates Manhattan distance between two tile coordinates.</summary>
    public static int ManhattanDistance(Vect2 a, Vect2 b)
        => (int)(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));

    /// <summary>Calculates Chebyshev distance between two tile coordinates.</summary>
    public static int ChebyshevDistance(Vect2 a, Vect2 b)
        => (int)Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>Checks whether two distinct tiles are immediate neighbors.</summary>
    /// <param name="aLocation">Reference tile.</param>
    /// <param name="bLocation">Candidate neighboring tile.</param>
    /// <param name="includeCorners">Whether diagonal neighbors count.</param>
    /// <returns><see langword="true"/> only for a distinct immediate neighbor.</returns>
    public static bool IsUnitAround(Vect2 aLocation, Vect2 bLocation, bool includeCorners)
    {
        float dx = MathF.Abs(aLocation.X - bLocation.X);
        float dy = MathF.Abs(aLocation.Y - bLocation.Y);

        if (dx == 0f && dy == 0f)
            return false;

        if (includeCorners)
            return dx <= 1f && dy <= 1f;

        return dx + dy == 1f;
    }

    /// <summary>Flood-fills orthogonally connected walkable tiles.</summary>
    /// <param name="start">Starting tile.</param>
    /// <param name="isWalkable">Predicate that must also enforce any desired map bounds.</param>
    /// <returns>All connected walkable tiles reachable from <paramref name="start"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="isWalkable"/> is null.</exception>
    public static List<Vect2> FloodFill(Vect2 start, Func<Vect2, bool> isWalkable)
    {
        ArgumentNullException.ThrowIfNull(isWalkable);

        var result = new List<Vect2>();
        var visited = new HashSet<Vect2>();
        var queue = new Queue<Vect2>();

        if (!isWalkable(start))
            return result;

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vect2 current = queue.Dequeue();
            result.Add(current);

            foreach (Vect2 direction in CardinalDirections)
            {
                Vect2 neighbor = current + direction;
                if (visited.Add(neighbor) && isWalkable(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return result;
    }

    private static void ValidateTileSize(int tileSize, string parameterName)
    {
        if (tileSize <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Tile size must be greater than zero.");
    }

    private static void ValidateMapWidth(int mapWidth)
    {
        if (mapWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(mapWidth), "Map width must be greater than zero.");
    }
}
