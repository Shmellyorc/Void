// ============================================================================
//  SkylinePacker.cs
// ============================================================================
//  A texture atlas packer that uses the Skyline packing algorithm.
//  Maintains a skyline of the topmost occupied pixels and places new
//  textures in the lowest available position that fits.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas.Packers;

/// <summary>
/// A texture atlas packer that uses the Skyline packing algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SkylinePacker"/> implements the Skyline packing algorithm
/// for texture atlases. It maintains a skyline of the topmost occupied pixels
/// and places new textures in the lowest available position that fits.
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Maintains a skyline (topmost occupied Y position for each X coordinate)</description></item>
///   <item><description>When packing, finds the lowest Y position that can accommodate the texture width</description></item>
///   <item><description>Places the texture at that position and updates the skyline</description></item>
///   <item><description>When freeing, inserts free space back into the skyline</description></item>
///   <item><description>Defragmentation repacks all textures in order of position</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Characteristics:</b>
/// <list type="bullet">
///   <item><description>O(n²) search time where n is the number of skyline nodes</description></item>
///   <item><description>Better packing efficiency and less fragmentation than Guillotine</description></item>
///   <item><description>Good for textures of similar sizes</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var packer = new SkylinePacker(2048, 2048);
/// 
/// // Pack textures
/// if (packer.TryPack(128, 128, out var rect1))
///     // Packed at rect1.X, rect1.Y
/// 
/// if (packer.TryPack(256, 256, out var rect2))
///     // Packed at rect2.X, rect2.Y
/// 
/// // Check fragmentation
/// float frag = packer.Fragmentation;
/// 
/// // Free a texture
/// packer.Free(rect1);
/// 
/// // Defrag if fragmentation is high
/// if (packer.Fragmentation > 0.3f)
/// {
///     var moves = packer.Defrag();
///     // moves contains (oldRect, newRect) pairs for relocated textures
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used from a single thread.
/// </para>
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
    /// Gets the total amount of space currently used by packed rectangles.
    /// </summary>
    /// <value>The total area (in pixels) occupied by packed textures.</value>
    public int UsedSpace => _usedSpace;

    /// <summary>
    /// Gets the total available space in the atlas.
    /// </summary>
    /// <value>The total area (in pixels) of the atlas (width × height).</value>
    public int TotalSpace => _width * _height;

    /// <summary>
    /// Gets the fragmentation percentage of the atlas.
    /// </summary>
    /// <value>
    /// A value between 0 and 1 representing the percentage of wasted space
    /// due to fragmentation. Higher values indicate more wasted space.
    /// </value>
    /// <remarks>
    /// Fragmentation is calculated as: <c>1 - (UsedSpace / TotalSpace)</c>
    /// </remarks>
    public float Fragmentation
    {
        get
        {
            if (TotalSpace == 0) return 0f;
            return 1f - ((float)_usedSpace / TotalSpace);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkylinePacker"/> class.
    /// </summary>
    /// <param name="width">The width of the atlas in pixels.</param>
    /// <param name="height">The height of the atlas in pixels.</param>
    /// <exception cref="ArgumentException">Thrown when width or height is less than or equal to zero.</exception>
    public SkylinePacker(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than zero.", nameof(height));

        _width = width;
        _height = height;
        _skyline = new List<SkylineNode> { new() { X = 0, Y = 0, Width = width } };
        _packedRects = new List<Rect2>();
        _usedSpace = 0;
    }

    /// <summary>
    /// Attempts to pack a rectangle of the specified size into the atlas.
    /// </summary>
    /// <param name="width">The width of the rectangle to pack.</param>
    /// <param name="height">The height of the rectangle to pack.</param>
    /// <param name="packedRect">
    /// When this method returns, contains the packed position and size if successful;
    /// otherwise, <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the rectangle was successfully packed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses the lowest Y position strategy, finding the position
    /// with the lowest available Y coordinate that can accommodate the requested
    /// width and height.
    /// </para>
    /// <para>
    /// After placing the rectangle, the skyline is updated to reflect the new
    /// topmost occupied pixels.
    /// </para>
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
            int maxY = 0;  // Start at 0, not _skyline[i].Y

            for (int j = i; j < _skyline.Count; j++)
            {
                if (_skyline[j].Y > maxY)
                    maxY = _skyline[j].Y;

                currentWidth += _skyline[j].Width;

                if (currentWidth >= width)
                {
                    if (maxY + height <= _height)
                    {
                        if (maxY < bestY || (maxY == bestY && currentX < bestX))
                        {
                            bestY = maxY;
                            bestX = currentX;
                            bestIndex = i;
                        }
                    }
                    break;
                }

                if (currentX + currentWidth >= _width)
                    break;
            }
        }

        if (bestIndex == -1)
            return false;

        packedRect = new Rect2(bestX, bestY, width, height);
        _usedSpace += width * height;
        _packedRects.Add(packedRect);

        UpdateSkyline(bestIndex, bestX, bestY, width, height);

        return true;
    }

    /// <summary>
    /// Frees a previously packed rectangle, making its space available for reuse.
    /// </summary>
    /// <param name="rect">The rectangle to free, as returned from <see cref="TryPack"/>.</param>
    /// <remarks>
    /// <para>
    /// This method marks the specified rectangle as free space and inserts it
    /// back into the skyline for future packing operations.
    /// </para>
    /// <para>
    /// The rectangle must match exactly the rectangle that was returned from
    /// a previous successful call to <see cref="TryPack"/>.
    /// </para>
    /// </remarks>
    public void Free(Rect2 rect)
    {
        if (!_packedRects.Contains(rect))
            return;

        _packedRects.Remove(rect);
        _usedSpace -= (int)(rect.Width * rect.Height);
        if (_usedSpace < 0) _usedSpace = 0;

        InsertFreeSpace(rect);
    }

    /// <summary>
    /// Inserts freed space back into the skyline, lowering the skyline at the freed rectangle's position.
    /// </summary>
    /// <param name="rect">The rectangle being freed.</param>
    private void InsertFreeSpace(Rect2 rect)
    {
        int insertX = (int)rect.X;
        int insertY = (int)rect.Y;
        int insertWidth = (int)rect.Width;
        int insertEndX = insertX + insertWidth;

        // Find nodes that overlap with the freed rectangle and lower them
        for (int i = 0; i < _skyline.Count; i++)
        {
            var node = _skyline[i];
            int nodeEndX = node.X + node.Width;

            // Check if this node overlaps with the freed rectangle
            if (nodeEndX > insertX && node.X < insertEndX)
            {
                // Lower the overlapping portion to the freed rectangle's Y
                if (node.Y > insertY)
                {
                    _skyline[i] = new SkylineNode
                    {
                        X = node.X,
                        Y = insertY,
                        Width = node.Width
                    };
                }
            }
        }

        MergeSkylineNodes();
    }

    /// <summary>
    /// Merges adjacent skyline nodes that have the same Y position.
    /// </summary>
    private void MergeSkylineNodes()
    {
        for (int i = 0; i < _skyline.Count - 1; i++)
        {
            if (_skyline[i].Y == _skyline[i + 1].Y)
            {
                _skyline[i] = new SkylineNode
                {
                    X = _skyline[i].X,
                    Y = _skyline[i].Y,
                    Width = _skyline[i].Width + _skyline[i + 1].Width
                };
                _skyline.RemoveAt(i + 1);
                i--;
            }
        }
    }

    /// <summary>
    /// Clears all packed rectangles from the atlas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method resets the packer to its initial state, removing all
    /// packed rectangles and resetting used space to zero. The entire atlas
    /// becomes available for new textures.
    /// </para>
    /// <para>
    /// This does not dispose or release any underlying GPU resources.
    /// It only resets the packer's internal tracking state.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
        _packedRects.Clear();
        _usedSpace = 0;
    }

    /// <summary>
    /// Defragments the atlas by repacking all textures in order of position.
    /// </summary>
    /// <returns>
    /// A list of moves, where each item contains the old rectangle and the
    /// new rectangle for textures that were relocated during defragmentation.
    /// Rectangles that did not move are not included in the list.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method sorts all packed rectangles by their Y position (top to bottom)
    /// and then by X position (left to right), and repacks them into a clean atlas.
    /// This consolidates free space and reduces fragmentation.
    /// </para>
    /// <para>
    /// The returned move list is essential for the <see cref="AtlasManager"/> to
    /// physically move texture data on the render texture. Without this, the
    /// atlas texture will contain stale pixel data at old positions.
    /// </para>
    /// <para>
    /// This is an expensive operation that should be performed sparingly,
    /// such as when <see cref="Fragmentation"/> exceeds a threshold.
    /// </para>
    /// <para>
    /// After defragmentation, existing references to packed rectangles become
    /// invalid and must be updated. The <see cref="AtlasManager"/> handles
    /// this automatically.
    /// </para>
    /// </remarks>
    public List<(Rect2 OldRect, Rect2 NewRect)> Defrag()
    {
        var moves = new List<(Rect2 OldRect, Rect2 NewRect)>();

        if (_packedRects.Count == 0)
            return moves;

        var sorted = _packedRects.OrderBy(r => r.Top).ThenBy(r => r.Left).ToList();

        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
        _usedSpace = 0;

        var newRects = new List<Rect2>();
        foreach (var oldRect in sorted)
        {
            if (TryPack((int)oldRect.Width, (int)oldRect.Height, out var newRect))
            {
                if (oldRect != newRect)
                {
                    moves.Add((oldRect, newRect));
                }
                newRects.Add(newRect);
            }
            else
            {
                newRects.Add(oldRect);
                _usedSpace += (int)(oldRect.Width * oldRect.Height);
                InsertFreeSpace(oldRect);
            }
        }

        _packedRects.Clear();
        _packedRects.AddRange(newRects);

        return moves;
    }

    /// <summary>
    /// Updates the skyline after placing a new rectangle at the specified position.
    /// </summary>
    /// <param name="index">The index of the skyline node where packing started.</param>
    /// <param name="x">The X position of the packed rectangle.</param>
    /// <param name="y">The Y position of the packed rectangle.</param>
    /// <param name="width">The width of the packed rectangle.</param>
    /// <param name="height">The height of the packed rectangle.</param>
    /// <remarks>
    /// <para>
    /// This method raises the skyline to the top of the newly packed rectangle
    /// for the span of X coordinates that the rectangle occupies.
    /// </para>
    /// <para>
    /// The skyline is represented as a list of nodes where each node has an X position,
    /// a Y position (the top of occupied space), and a width. After packing a rectangle,
    /// the skyline must be raised to at least the bottom of the new rectangle for the
    /// X span it occupies.
    /// </para>
    /// </remarks>
    private void UpdateSkyline(int index, int x, int y, int width, int height)
    {
        int newY = y + height;
        int endX = x + width;

        // Collect all nodes that overlap with the rectangle
        var overlappingNodes = new List<(int Index, SkylineNode Node)>();
        int currentIndex = 0;

        // Find all nodes that overlap with [x, endX)
        while (currentIndex < _skyline.Count)
        {
            var node = _skyline[currentIndex];
            int nodeEndX = node.X + node.Width;

            if (nodeEndX > x && node.X < endX)
            {
                overlappingNodes.Add((currentIndex, node));
            }
            else if (node.X >= endX)
            {
                break; // Past the rectangle, stop searching
            }
            currentIndex++;
        }

        if (overlappingNodes.Count == 0)
            return;

        // Remove all overlapping nodes from the skyline
        for (int i = overlappingNodes.Count - 1; i >= 0; i--)
        {
            _skyline.RemoveAt(overlappingNodes[i].Index);
        }

        // Create new nodes for the parts not covered by the rectangle
        var newNodes = new List<SkylineNode>();

        foreach (var (_, node) in overlappingNodes)
        {
            int nodeEndX = node.X + node.Width;

            // Left part (before rectangle) - keeps old Y
            if (node.X < x)
            {
                int leftWidth = x - node.X;
                newNodes.Add(new SkylineNode
                {
                    X = node.X,
                    Y = node.Y,
                    Width = leftWidth
                });
            }

            // Right part (after rectangle) - keeps old Y
            if (nodeEndX > endX)
            {
                int rightWidth = nodeEndX - endX;
                newNodes.Add(new SkylineNode
                {
                    X = endX,
                    Y = node.Y,
                    Width = rightWidth
                });
            }
        }

        // Add the raised part for the rectangle span
        newNodes.Add(new SkylineNode
        {
            X = x,
            Y = newY,
            Width = width
        });

        // Sort new nodes by X position
        newNodes.Sort((a, b) => a.X.CompareTo(b.X));

        // Insert new nodes back into the skyline
        int insertIndex = overlappingNodes.Count > 0 ? overlappingNodes[0].Index : _skyline.Count;
        _skyline.InsertRange(insertIndex, newNodes);

        // Merge adjacent nodes with same Y
        MergeSkylineNodes();
    }
}