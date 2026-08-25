// ============================================================================
//  GuillotinePacker.cs
// ============================================================================
//  A texture atlas packer that uses the Guillotine packing algorithm.
//  Selects the best free rectangle that fits the requested size,
//  prioritizing the smallest area that can contain the texture.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas.Packers;

/// <summary>
/// A texture atlas packer that uses the Guillotine packing algorithm.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GuillotinePacker"/> implements the Guillotine packing
/// algorithm for texture atlases. It selects the best free rectangle that
/// fits the requested size, prioritizing the smallest area that can contain
/// the texture (best area fit).
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Maintains a list of free rectangles in the atlas</description></item>
///   <item><description>When packing, finds the free rectangle with the smallest area that fits the texture</description></item>
///   <item><description>Splits the chosen rectangle into remaining free space</description></item>
///   <item><description>When freeing, merges adjacent free rectangles to reduce fragmentation</description></item>
///   <item><description>Defragmentation repacks all textures to optimize space</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Characteristics:</b>
/// <list type="bullet">
///   <item><description>O(n) search time where n is the number of free rectangles</description></item>
///   <item><description>Good packing efficiency for textures of varying sizes</description></item>
///   <item><description>Tends to create more fragmentation than Skyline algorithm</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var packer = new GuillotinePacker(2048, 2048);
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
public sealed class GuillotinePacker : IAtlasPacker
{
    private readonly int _width, _height;
    private readonly List<Rect2> _freeRects;
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
    /// Initializes a new instance of the <see cref="GuillotinePacker"/> class.
    /// </summary>
    /// <param name="width">The width of the atlas in pixels.</param>
    /// <param name="height">The height of the atlas in pixels.</param>
    /// <exception cref="ArgumentException">Thrown when width or height is less than or equal to zero.</exception>
    public GuillotinePacker(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than zero.", nameof(height));

        _width = width;
        _height = height;
        _freeRects = new List<Rect2> { new(0, 0, width, height) };
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
    /// This method uses the best area fit strategy, finding the free rectangle
    /// with the smallest area that can accommodate the requested size.
    /// </para>
    /// <para>
    /// After placing the rectangle, the remaining space is split into new
    /// free rectangles for future packing.
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
        int bestArea = int.MaxValue;

        for (int i = 0; i < _freeRects.Count; i++)
        {
            var rect = _freeRects[i];
            if (rect.Width >= width && rect.Height >= height)
            {
                int area = (int)(rect.Width * rect.Height);
                if (area < bestArea)
                {
                    bestArea = area;
                    bestIndex = i;
                }
            }
        }

        if (bestIndex == -1)
            return false;

        var freeRect = _freeRects[bestIndex];
        _freeRects.RemoveAt(bestIndex);

        packedRect = new Rect2(freeRect.Left, freeRect.Top, width, height);
        _usedSpace += width * height;
        _packedRects.Add(packedRect);

        float remainingWidth = freeRect.Width - width;
        float remainingHeight = freeRect.Height - height;

        if (remainingWidth > 0)
            _freeRects.Add(new Rect2(freeRect.Left + width, freeRect.Top, remainingWidth, height));
        if (remainingHeight > 0)
            _freeRects.Add(new Rect2(freeRect.Left, freeRect.Top + height, freeRect.Width, remainingHeight));

        return true;
    }

    /// <summary>
    /// Frees a previously packed rectangle, making its space available for reuse.
    /// </summary>
    /// <param name="rect">The rectangle to free, as returned from <see cref="TryPack"/>.</param>
    /// <remarks>
    /// <para>
    /// This method marks the specified rectangle as free space and then
    /// attempts to merge it with adjacent free rectangles to reduce fragmentation.
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
        _freeRects.Add(rect);
        _usedSpace -= (int)(rect.Width * rect.Height);
        if (_usedSpace < 0) _usedSpace = 0;

        MergeFreeRects();
    }

    private void MergeFreeRects()
    {
        bool merged;
        do
        {
            merged = false;
            for (int i = 0; i < _freeRects.Count; i++)
            {
                for (int j = i + 1; j < _freeRects.Count; j++)
                {
                    if (TryMerge(_freeRects[i], _freeRects[j], out var mergedRect))
                    {
                        _freeRects[i] = mergedRect;
                        _freeRects.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
                if (merged) break;
            }
        } while (merged);
    }

    private bool TryMerge(Rect2 a, Rect2 b, out Rect2 merged)
    {
        merged = default;

        if (a.Y == b.Y && a.Height == b.Height && a.X + a.Width == b.X)
        {
            merged = new Rect2(a.X, a.Y, a.Width + b.Width, a.Height);
            return true;
        }

        if (a.X == b.X && a.Width == b.Width && a.Y + a.Height == b.Y)
        {
            merged = new Rect2(a.X, a.Y, a.Width, a.Height + b.Height);
            return true;
        }

        return false;
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
        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
        _packedRects.Clear();
        _usedSpace = 0;
    }

    /// <summary>
    /// Defragments the atlas by repacking all textures in order of size.
    /// </summary>
    /// <returns>
    /// A list of moves, where each item contains the old rectangle and the
    /// new rectangle for textures that were relocated during defragmentation.
    /// Rectangles that did not move are not included in the list.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method sorts all packed rectangles by area (largest first) and
    /// repacks them into a clean atlas. This consolidates free space and
    /// reduces fragmentation.
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

        var sorted = _packedRects.OrderByDescending(r => r.Width * r.Height).ToList();

        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
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
                _freeRects.Add(oldRect);
                
                MergeFreeRects();
            }
        }

        _packedRects.Clear();
        _packedRects.AddRange(newRects);

        return moves;
    }
}