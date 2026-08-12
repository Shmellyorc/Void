namespace Void.Engine.Helpers;

/// <summary>
/// Provides helper methods for converting between tile-based map coordinates
/// and world-space positions.
/// </summary>
/// <remarks>
/// This static utility class includes methods for:
/// <list type="bullet">
///   <item>
///     <description>Mapping grid locations to world-space coordinates (<see cref="MapToWorld"/>).</description>
///   </item>
///   <item>
///     <description>Mapping world-space positions back to grid coordinates (<see cref="WorldToMap"/>).</description>
///   </item>
///   <item>
///     <description>Converting between 1D tile indices and 2D coordinates (<see cref="To2D"/> and <see cref="To1D"/>).</description>
///   </item>
///   <item>
///     <description>Converting between world positions and tile indices (<see cref="WorldToIndex"/> and <see cref="IndexToWorld"/>).</description>
///   </item>
/// </list>
/// These conversions are useful in tile-based games or applications where
/// positions need to be translated between logical grid space and pixel space.
/// </remarks>
public static class MapHelper
{
    /// <summary>
    /// Converts a tile-based grid location into world-space coordinates.
    /// </summary>
    /// <param name="location">The grid location.</param>
    /// <param name="tilesize">The size of one tile.</param>
    /// <returns>World-space coordinates in pixels.</returns>
    public static Vect2 MapToWorld(in Vect2 location, int tilesize)
        => Vect2.Floor(location * tilesize);

    /// <summary>
    /// Converts a world-space position into map grid coordinates.
    /// </summary>
    /// <param name="position">World-space position.</param>
    /// <param name="tilesize">The size of one tile.</param>
    /// <returns>Tile-based grid coordinates.</returns>
    public static Vect2 WorldToMap(in Vect2 position, int tilesize)
        => Vect2.Floor(position / tilesize);

    /// <summary>
    /// Converts a 1‑dimensional tile index into a 2D coordinate.
    /// </summary>
    /// <param name="index">The flat index.</param>
    /// <param name="mapWidth">The width of the tile grid in tiles.</param>
    /// <returns>A <see cref="Vect2"/> representing the (x, y) tile position.</returns>
    public static Vect2 To2D(int index, int mapWidth) =>
        new(index % mapWidth, index / mapWidth);

    /// <summary>
    /// Converts a 2D tile coordinate into a 1‑dimensional index.
    /// </summary>
    /// <param name="location">The (x, y) tile position.</param>
    /// <param name="mapWidth">The width of the tile grid in tiles.</param>
    /// <returns>The flat index corresponding to <paramref name="location"/>.</returns>
    public static int To1D(Vect2 location, int mapWidth) =>
        (int)location.Y * mapWidth + (int)location.X;

    /// <summary>
    /// Converts a world-space position into a 1D tile index.
    /// </summary>
    /// <param name="position">World-space position.</param>
    /// <param name="tileSize">The size of one tile in world units.</param>
    /// <param name="mapWidth">The width of the tile grid in tiles.</param>
    /// <returns>The flat tile index at the given world position.</returns>
    public static int WorldToIndex(Vect2 position, int tileSize, int mapWidth)
    {
        var tile = WorldToMap(position, tileSize);
        return To1D(tile, mapWidth);
    }

    /// <summary>
    /// Converts a 1D tile index into a world-space position (top-left corner of the tile).
    /// </summary>
    /// <param name="index">The flat tile index.</param>
    /// <param name="tileSize">The size of one tile in world units.</param>
    /// <param name="mapWidth">The width of the tile grid in tiles.</param>
    /// <returns>The world-space position of the tile's top-left corner.</returns>
    public static Vect2 IndexToWorld(int index, int tileSize, int mapWidth)
    {
        var tile = To2D(index, mapWidth);
        return MapToWorld(tile, tileSize);
    }

    /// <summary>
    /// Converts a world‑space rectangle into a list of tile coordinates on a grid.
    /// </summary>
    /// <param name="size">
    /// The size of the area in world units.  
    /// Only whole tiles that fully fit within this size are included.
    /// </param>
    /// <param name="location">
    /// The top‑left tile coordinate where the area begins.  
    /// This acts as the origin for the generated tile positions.
    /// </param>
    /// <param name="tileSize">
    /// The size of a single tile in world units.  
    /// Used to determine how many tiles fit horizontally and vertically.
    /// </param>
    /// <returns>
    /// A list of <see cref="Vect2"/> tile coordinates covering the specified area,  
    /// starting at <paramref name="location"/> and extending for as many whole tiles  
    /// as fit within <paramref name="size"/>.
    /// </returns>
    /// <remarks>
    /// This method floors the tile count in each dimension, ensuring that only tiles
    /// fully contained within the given world‑space size are returned.  
    /// Useful for collision queries, region checks, and tile‑based spatial iteration.
    /// </remarks>
    public static List<Vect2> ToMap(Vect2 size, Vect2 location, int tileSize)
    {
        var xSize = (int)MathF.Floor(size.X / tileSize);
        var ySize = (int)MathF.Floor(size.Y / tileSize);
        var result = new List<Vect2>(xSize * ySize);

        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
                result.Add(location + new Vect2(x, y));
        }

        return result;
    }

    /// <summary>
    /// Returns all tile coordinates within a circular radius of a center point.
    /// </summary>
    /// <param name="center">The center tile coordinate.</param>
    /// <param name="radius">The radius in tiles.</param>
    /// <returns>A list of tile coordinates within the circle.</returns>
    public static List<Vect2> ToCircle(Vect2 center, int radius)
    {
        var result = new List<Vect2>((radius * 2 + 1) * (radius * 2 + 1));
        float radiusSquared = radius * radius;

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

    /// <summary>
    /// Returns all tile coordinates within a circular ring (donut shape).
    /// </summary>
    /// <param name="center">The center tile coordinate.</param>
    /// <param name="innerRadius">The inner radius in tiles (exclusive).</param>
    /// <param name="outerRadius">The outer radius in tiles (inclusive).</param>
    /// <returns>A list of tile coordinates within the ring.</returns>
    public static List<Vect2> ToRing(Vect2 center, int innerRadius, int outerRadius)
    {
        var result = new List<Vect2>();
        float outerSquared = outerRadius * outerRadius;
        float innerSquared = innerRadius * innerRadius;

        for (int y = -outerRadius; y <= outerRadius; y++)
        {
            for (int x = -outerRadius; x <= outerRadius; x++)
            {
                float distSquared = x * x + y * y;
                if (distSquared <= outerSquared && distSquared > innerSquared)
                    result.Add(center + new Vect2(x, y));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns all tile coordinates along a line between two tile positions using Bresenham's algorithm.
    /// </summary>
    /// <param name="start">The starting tile coordinate.</param>
    /// <param name="end">The ending tile coordinate.</param>
    /// <returns>A list of tile coordinates forming a line from <paramref name="start"/> to <paramref name="end"/>.</returns>
    public static List<Vect2> ToLine(Vect2 start, Vect2 end)
    {
        var result = new List<Vect2>();
        int x0 = (int)start.X, y0 = (int)start.Y;
        int x1 = (int)end.X, y1 = (int)end.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            result.Add(new Vect2(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }

        return result;
    }

    /// <summary>
    /// Returns all tiles on the border of a rectangular area.
    /// </summary>
    /// <param name="start">The top-left tile coordinate of the area.</param>
    /// <param name="width">The width of the area in tiles.</param>
    /// <param name="height">The height of the area in tiles.</param>
    /// <returns>A list of tile coordinates forming the border of the rectangle.</returns>
    public static List<Vect2> ToEdge(Vect2 start, int width, int height)
    {
        var result = new List<Vect2>(2 * width + 2 * height - 4);

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

    /// <summary>
    /// Determines whether a tile coordinate is within the bounds of a map.
    /// </summary>
    /// <param name="tile">The tile coordinate to check.</param>
    /// <param name="mapWidth">The width of the map in tiles.</param>
    /// <param name="mapHeight">The height of the map in tiles.</param>
    /// <returns><c>true</c> if the tile is within bounds; otherwise, <c>false</c>.</returns>
    public static bool IsInBounds(Vect2 tile, int mapWidth, int mapHeight)
        => tile.X >= 0 && tile.X < mapWidth && tile.Y >= 0 && tile.Y < mapHeight;

    /// <summary>
    /// Calculates the Manhattan distance between two tile coordinates.
    /// </summary>
    /// <param name="a">The first tile coordinate.</param>
    /// <param name="b">The second tile coordinate.</param>
    /// <returns>The Manhattan distance (sum of absolute differences).</returns>
    public static int ManhattanDistance(Vect2 a, Vect2 b)
        => (int)(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));

    /// <summary>
    /// Calculates the Chebyshev distance between two tile coordinates.
    /// </summary>
    /// <param name="a">The first tile coordinate.</param>
    /// <param name="b">The second tile coordinate.</param>
    /// <returns>The Chebyshev distance (maximum of absolute differences).</returns>
    public static int ChebyshevDistance(Vect2 a, Vect2 b)
        => (int)Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>
    /// Determines whether a unit located at <paramref name="bLocation"/> is adjacent to <paramref name="aLocation"/> on a tile grid.
    /// </summary>
    /// <remarks>
    /// <para>This method operates on tile coordinates, not pixel coordinates.</para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       If <paramref name="includeCorners"/> is <c>false</c>, adjacency is restricted to the 
    ///       four cardinal directions (up, down, left, right).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       If <paramref name="includeCorners"/> is <c>true</c>, diagonal tiles are also considered 
    ///       adjacent.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="aLocation">The tile location of the reference unit.</param>
    /// <param name="bLocation">The tile location of the unit being checked.</param>
    /// <param name="includeCorners">
    /// If <c>true</c>, diagonal tiles are considered adjacent in addition to orthogonal tiles.  
    /// If <c>false</c>, only orthogonal tiles are considered.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="bLocation"/> is the same tile as <paramref name="aLocation"/> 
    /// or is an adjacent tile (depending on <paramref name="includeCorners"/>); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsUnitAround(Vect2 aLocation, Vect2 bLocation, bool includeCorners)
    {
        if (aLocation == bLocation)
            return true;

        if (includeCorners)
        {
            if (aLocation.Distance(bLocation) > 2)
                return false;
        }
        else
        {
            if (aLocation.Distance(bLocation) > 1)
                return false;
        }

        Vect2[] neighbours = includeCorners
            ?
                [
                    aLocation + Vect2.Up,
                    aLocation + Vect2.Right,
                    aLocation + Vect2.Down,
                    aLocation + Vect2.Left,

                    aLocation + new Vect2(-1),       // Top Left
                    aLocation + new Vect2(1, -1),    // Top Right
                    aLocation + new Vect2(-1, 1),    // Bottom Left
                    aLocation + new Vect2(1),        // Bottom Right
                ]
            :
                [
                    aLocation + Vect2.Up,
                    aLocation + Vect2.Right,
                    aLocation + Vect2.Down,
                    aLocation + Vect2.Left,
                ];

        for (int i = neighbours.Length - 1; i >= 0; i--)
        {
            var neighbour = neighbours[i];

            if (bLocation != neighbour)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a flood fill starting from a tile coordinate.
    /// </summary>
    /// <param name="start">The starting tile coordinate.</param>
    /// <param name="isWalkable">A function that returns <c>true</c> if the tile is walkable.</param>
    /// <returns>A list of all connected walkable tile coordinates.</returns>
    public static List<Vect2> FloodFill(Vect2 start, Func<Vect2, bool> isWalkable)
    {
        var result = new List<Vect2>();
        var visited = new HashSet<Vect2>();
        var queue = new Queue<Vect2>();

        if (!isWalkable(start))
            return result;

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            Vect2[] directions = [Vect2.Up, Vect2.Right, Vect2.Down, Vect2.Left];
            foreach (var dir in directions)
            {
                var neighbor = current + dir;
                if (!visited.Contains(neighbor) && isWalkable(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }
}