// ============================================================================
//  SkylinePacker.cs
// ============================================================================
//  Texture atlas packing using a Skyline layout.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas.Packers;

/// <summary>
/// Packs rectangles by maintaining the upper boundary of occupied atlas space.
/// </summary>
/// <remarks>
/// Placements prefer the lowest available Y position and then the leftmost X
/// position. Freeing an allocation rebuilds the skyline from the surviving
/// rectangles so occupied space is never exposed as reusable space.
/// </remarks>
public sealed class SkylinePacker : IAtlasPacker
{
    private struct SkylineNode
    {
        public int X;
        public int Y;
        public int Width;
    }

    private readonly int _width;
    private readonly int _height;
    private readonly List<SkylineNode> _skyline;
    private readonly List<Rect2> _packedRects;
    private int _usedSpace;

    /// <summary>
    /// Gets the pixel area currently occupied by packed rectangles.
    /// </summary>
    public int UsedSpace => _usedSpace;

    /// <summary>
    /// Gets the total pixel area of the atlas.
    /// </summary>
    public int TotalSpace => _width * _height;

    /// <summary>
    /// Gets the current external allocation fragmentation.
    /// </summary>
    /// <remarks>
    /// The value is <c>1 - (largest currently allocatable free rectangle area / total free area)</c>.
    /// Free holes trapped beneath the skyline increase fragmentation because they
    /// cannot be reused safely until defragmentation moves the surviving allocations.
    /// An empty or completely full page reports 0.
    /// </remarks>
    public float Fragmentation
    {
        get
        {
            int freeArea = TotalSpace - _usedSpace;
            if (freeArea <= 0)
                return 0f;

            int largestFreeArea = GetLargestAllocatableFreeArea();
            if (largestFreeArea <= 0)
                return 1f;

            return Math.Clamp(1f - ((float)largestFreeArea / freeArea), 0f, 1f);
        }
    }

    /// <summary>
    /// Initializes a packer for an atlas of the specified size.
    /// </summary>
    /// <param name="width">The atlas width in pixels.</param>
    /// <param name="height">The atlas height in pixels.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public SkylinePacker(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than zero.", nameof(height));

        _width = width;
        _height = height;
        _skyline = [new SkylineNode { X = 0, Y = 0, Width = width }];
        _packedRects = [];
        _usedSpace = 0;
    }

    /// <summary>
    /// Attempts to pack a rectangle at the lowest available skyline position.
    /// </summary>
    /// <param name="width">The requested width in pixels.</param>
    /// <param name="height">The requested height in pixels.</param>
    /// <param name="packedRect">
    /// Receives the packed rectangle when successful; otherwise,
    /// <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when space was reserved; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Non-positive sizes and sizes larger than the atlas return
    /// <see langword="false"/> rather than throwing. Equal-height candidates
    /// prefer the smaller X coordinate.
    /// </remarks>
    public bool TryPack(int width, int height, out Rect2 packedRect)
    {
        packedRect = default;

        if (width <= 0 || height <= 0)
            return false;
        if (width > _width || height > _height)
            return false;

        int bestIndex = -1;
        int bestY = int.MaxValue;
        int bestX = int.MaxValue;

        for (int i = 0; i < _skyline.Count; i++)
        {
            int currentX = _skyline[i].X;
            int currentWidth = 0;
            int maxY = 0;

            for (int j = i; j < _skyline.Count; j++)
            {
                if (_skyline[j].Y > maxY)
                    maxY = _skyline[j].Y;

                currentWidth += _skyline[j].Width;

                if (currentWidth >= width)
                {
                    if (maxY + height <= _height &&
                        (maxY < bestY || (maxY == bestY && currentX < bestX)))
                    {
                        bestY = maxY;
                        bestX = currentX;
                        bestIndex = i;
                    }
                    break;
                }

                if (currentX + currentWidth >= _width)
                    break;
            }
        }

        if (bestIndex < 0)
            return false;

        packedRect = new Rect2(bestX, bestY, width, height);
        _usedSpace += width * height;
        _packedRects.Add(packedRect);

        UpdateSkyline(bestX, bestY, width, height);
        ValidateState();

        return true;
    }

    /// <summary>
    /// Releases an exact previously packed rectangle.
    /// </summary>
    /// <param name="rect">The packed rectangle to release.</param>
    /// <remarks>
    /// Rectangles that are not currently tracked as packed are ignored. The
    /// skyline is rebuilt from the surviving allocations after a successful
    /// release. Free holes beneath surviving rectangles remain unavailable until
    /// <see cref="Defrag"/> moves those allocations and reports their new positions.
    /// </remarks>
    public void Free(Rect2 rect)
    {
        if (!_packedRects.Remove(rect))
            return;

        _usedSpace -= (int)(rect.Width * rect.Height);
        if (_usedSpace < 0)
            _usedSpace = 0;

        RebuildSkylineFromPackedRects();
        ValidateState();
    }

    /// <summary>
    /// Resets the packer to a flat skyline covering the entire atlas width.
    /// </summary>
    public void Clear()
    {
        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
        _packedRects.Clear();
        _usedSpace = 0;
        ValidateState();
    }

    /// <summary>
    /// Rebuilds the layout by repacking rectangles in their current spatial order.
    /// </summary>
    /// <returns>
    /// The old and new rectangle for each allocation whose position changed.
    /// </returns>
    /// <remarks>
    /// Rectangles are processed from top to bottom and then left to right. The
    /// rebuild is transactional: if every existing rectangle cannot be repacked,
    /// the current layout is left unchanged and an empty move list is returned.
    /// </remarks>
    public List<(Rect2 OldRect, Rect2 NewRect)> Defrag()
    {
        if (_packedRects.Count == 0)
            return [];

        var sorted = _packedRects
            .OrderBy(r => r.Top)
            .ThenBy(r => r.Left)
            .ToList();

        var rebuilt = new SkylinePacker(_width, _height);
        var moves = new List<(Rect2 OldRect, Rect2 NewRect)>();

        for (int i = 0; i < sorted.Count; i++)
        {
            Rect2 oldRect = sorted[i];
            if (!rebuilt.TryPack((int)oldRect.Width, (int)oldRect.Height, out Rect2 newRect))
                return [];

            if (oldRect != newRect)
                moves.Add((oldRect, newRect));
        }

        if (moves.Count == 0)
            return [];

        _skyline.Clear();
        _skyline.AddRange(rebuilt._skyline);

        _packedRects.Clear();
        _packedRects.AddRange(rebuilt._packedRects);

        _usedSpace = rebuilt._usedSpace;
        ValidateState();

        return moves;
    }

    private int GetLargestAllocatableFreeArea()
    {
        int largestArea = 0;

        for (int i = 0; i < _skyline.Count; i++)
        {
            int width = 0;
            int maxY = 0;

            for (int j = i; j < _skyline.Count; j++)
            {
                SkylineNode node = _skyline[j];
                width += node.Width;
                if (node.Y > maxY)
                    maxY = node.Y;

                int availableHeight = _height - maxY;
                if (availableHeight <= 0)
                    continue;

                int area = width * availableHeight;
                if (area > largestArea)
                    largestArea = area;
            }
        }

        return largestArea;
    }

    private void RebuildSkylineFromPackedRects()
    {
        _skyline.Clear();

        if (_packedRects.Count == 0)
        {
            _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
            return;
        }

        var boundaries = new List<int>(_packedRects.Count * 2 + 2)
        {
            0,
            _width
        };

        for (int i = 0; i < _packedRects.Count; i++)
        {
            Rect2 rect = _packedRects[i];
            int left = (int)rect.Left;
            int right = (int)rect.Right;

            if (left > 0 && left < _width)
                boundaries.Add(left);
            if (right > 0 && right < _width)
                boundaries.Add(right);
        }

        boundaries.Sort();

        int uniqueCount = 1;
        for (int i = 1; i < boundaries.Count; i++)
        {
            if (boundaries[i] == boundaries[uniqueCount - 1])
                continue;

            boundaries[uniqueCount++] = boundaries[i];
        }

        for (int i = 0; i < uniqueCount - 1; i++)
        {
            int x = boundaries[i];
            int endX = boundaries[i + 1];
            int width = endX - x;

            if (width <= 0)
                continue;

            int y = 0;

            for (int j = 0; j < _packedRects.Count; j++)
            {
                Rect2 rect = _packedRects[j];
                int left = (int)rect.Left;
                int right = (int)rect.Right;

                if (right <= x || left >= endX)
                    continue;

                int bottom = (int)rect.Bottom;
                if (bottom > y)
                    y = bottom;
            }

            _skyline.Add(new SkylineNode
            {
                X = x,
                Y = y,
                Width = width
            });
        }

        MergeSkylineNodes();
    }

    private void MergeSkylineNodes()
    {
        for (int i = 0; i < _skyline.Count - 1; i++)
        {
            SkylineNode current = _skyline[i];
            SkylineNode next = _skyline[i + 1];

            if (current.Y != next.Y || current.X + current.Width != next.X)
                continue;

            _skyline[i] = new SkylineNode
            {
                X = current.X,
                Y = current.Y,
                Width = current.Width + next.Width
            };

            _skyline.RemoveAt(i + 1);
            i--;
        }
    }

    private void UpdateSkyline(int x, int y, int width, int height)
    {
        int newY = y + height;
        int endX = x + width;

        var overlappingNodes = new List<(int Index, SkylineNode Node)>();
        int currentIndex = 0;

        while (currentIndex < _skyline.Count)
        {
            SkylineNode node = _skyline[currentIndex];
            int nodeEndX = node.X + node.Width;

            if (nodeEndX > x && node.X < endX)
            {
                overlappingNodes.Add((currentIndex, node));
            }
            else if (node.X >= endX)
            {
                break;
            }

            currentIndex++;
        }

        if (overlappingNodes.Count == 0)
            return;

        for (int i = overlappingNodes.Count - 1; i >= 0; i--)
            _skyline.RemoveAt(overlappingNodes[i].Index);

        var newNodes = new List<SkylineNode>(overlappingNodes.Count + 1);

        foreach (var (_, node) in overlappingNodes)
        {
            int nodeEndX = node.X + node.Width;

            if (node.X < x)
            {
                newNodes.Add(new SkylineNode
                {
                    X = node.X,
                    Y = node.Y,
                    Width = x - node.X
                });
            }

            if (nodeEndX > endX)
            {
                newNodes.Add(new SkylineNode
                {
                    X = endX,
                    Y = node.Y,
                    Width = nodeEndX - endX
                });
            }
        }

        newNodes.Add(new SkylineNode
        {
            X = x,
            Y = newY,
            Width = width
        });

        newNodes.Sort((a, b) => a.X.CompareTo(b.X));

        int insertIndex = overlappingNodes[0].Index;
        _skyline.InsertRange(insertIndex, newNodes);

        MergeSkylineNodes();
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void ValidateState()
    {
        int calculatedUsedSpace = 0;

        for (int i = 0; i < _packedRects.Count; i++)
        {
            Rect2 rect = _packedRects[i];

            if (rect.Width <= 0 || rect.Height <= 0 ||
                rect.Left < 0 || rect.Top < 0 ||
                rect.Right > _width || rect.Bottom > _height)
            {
                throw new InvalidOperationException(
                    $"SkylinePacker packed rectangle {i} is outside the atlas bounds.");
            }

            if (rect.X != MathF.Truncate(rect.X) || rect.Y != MathF.Truncate(rect.Y) ||
                rect.Width != MathF.Truncate(rect.Width) || rect.Height != MathF.Truncate(rect.Height))
            {
                throw new InvalidOperationException(
                    $"SkylinePacker packed rectangle {i} is not aligned to integral pixels.");
            }

            calculatedUsedSpace += (int)(rect.Width * rect.Height);

            for (int j = i + 1; j < _packedRects.Count; j++)
            {
                if (rect.Intersects(_packedRects[j]))
                {
                    throw new InvalidOperationException(
                        $"SkylinePacker produced overlapping rectangles at indices {i} and {j}.");
                }
            }
        }

        if (calculatedUsedSpace != _usedSpace)
        {
            throw new InvalidOperationException(
                $"SkylinePacker used-space mismatch: tracked {_usedSpace}, calculated {calculatedUsedSpace}.");
        }

        int skylineWidth = 0;
        for (int i = 0; i < _skyline.Count; i++)
        {
            SkylineNode node = _skyline[i];
            if (node.Width <= 0 || node.X != skylineWidth || node.Y < 0 || node.Y > _height)
            {
                throw new InvalidOperationException(
                    $"SkylinePacker skyline node {i} does not form a valid page boundary.");
            }

            skylineWidth += node.Width;
        }

        if (skylineWidth != _width)
        {
            throw new InvalidOperationException(
                $"SkylinePacker skyline width mismatch: tracked {skylineWidth}, expected {_width}.");
        }
    }
}
