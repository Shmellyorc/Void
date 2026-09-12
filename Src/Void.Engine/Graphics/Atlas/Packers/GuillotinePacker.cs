// ============================================================================
//  GuillotinePacker.cs
// ============================================================================
//  Texture-atlas packing using a Guillotine free-rectangle layout.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Atlas.Packers;

/// <summary>
/// Packs rectangles with a best-area-fit Guillotine allocator.
/// </summary>
/// <remarks>
/// Each successful allocation consumes one free rectangle and splits the
/// remainder into non-overlapping free regions. Released regions are merged when
/// they share a complete edge. Defragmentation rebuilds into temporary state and
/// commits only after every live rectangle has been placed successfully.
/// </remarks>
public sealed class GuillotinePacker : IAtlasPacker
{
    private readonly int _width;
    private readonly int _height;
    private readonly List<Rect2> _freeRects;
    private readonly List<Rect2> _packedRects;
    private int _usedSpace;

    /// <summary>
    /// Gets the pixel area currently occupied by live allocations.
    /// </summary>
    public int UsedSpace => _usedSpace;

    /// <summary>
    /// Gets the total pixel area of the atlas page.
    /// </summary>
    public int TotalSpace => _width * _height;

    /// <summary>
    /// Gets the current external allocation fragmentation.
    /// </summary>
    /// <remarks>
    /// The value is <c>1 - (largest free rectangle area / total free area)</c>.
    /// An empty or completely full page reports 0.
    /// </remarks>
    public float Fragmentation
    {
        get
        {
            int freeArea = TotalSpace - _usedSpace;
            if (freeArea <= 0)
                return 0f;

            int largestFreeArea = 0;
            for (int i = 0; i < _freeRects.Count; i++)
            {
                int area = (int)(_freeRects[i].Width * _freeRects[i].Height);
                if (area > largestFreeArea)
                    largestFreeArea = area;
            }

            if (largestFreeArea <= 0)
                return 1f;

            return Math.Clamp(1f - ((float)largestFreeArea / freeArea), 0f, 1f);
        }
    }

    /// <summary>
    /// Initializes a packer for an atlas page of the specified size.
    /// </summary>
    /// <param name="width">The atlas width in pixels.</param>
    /// <param name="height">The atlas height in pixels.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public GuillotinePacker(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than zero.", nameof(height));

        _width = width;
        _height = height;
        _freeRects = [new Rect2(0, 0, width, height)];
        _packedRects = [];
        _usedSpace = 0;
    }

    /// <summary>
    /// Attempts to reserve a rectangle using best-area-fit placement.
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
            Rect2 rect = _freeRects[i];
            if (rect.Width < width || rect.Height < height)
                continue;

            int area = (int)(rect.Width * rect.Height);
            if (area >= bestArea)
                continue;

            bestArea = area;
            bestIndex = i;
        }

        if (bestIndex < 0)
            return false;

        Rect2 freeRect = _freeRects[bestIndex];
        _freeRects.RemoveAt(bestIndex);

        packedRect = new Rect2(freeRect.Left, freeRect.Top, width, height);
        _packedRects.Add(packedRect);
        _usedSpace += width * height;

        float remainingWidth = freeRect.Width - width;
        float remainingHeight = freeRect.Height - height;

        if (remainingWidth > 0)
            _freeRects.Add(new Rect2(freeRect.Left + width, freeRect.Top, remainingWidth, height));
        if (remainingHeight > 0)
            _freeRects.Add(new Rect2(freeRect.Left, freeRect.Top + height, freeRect.Width, remainingHeight));

        ValidateState();
        return true;
    }

    /// <summary>
    /// Releases an exact previously packed rectangle.
    /// </summary>
    /// <param name="rect">The packed rectangle to release.</param>
    /// <remarks>
    /// Unknown rectangles are ignored. A released region is merged repeatedly
    /// with complete-edge neighbors when doing so produces another rectangle.
    /// </remarks>
    public void Free(Rect2 rect)
    {
        if (!_packedRects.Remove(rect))
            return;

        _freeRects.Add(rect);
        _usedSpace -= (int)(rect.Width * rect.Height);
        if (_usedSpace < 0)
            _usedSpace = 0;

        MergeFreeRects();
        ValidateState();
    }

    /// <summary>
    /// Resets the packer to one free rectangle covering the entire page.
    /// </summary>
    public void Clear()
    {
        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
        _packedRects.Clear();
        _usedSpace = 0;
        ValidateState();
    }

    /// <summary>
    /// Rebuilds the layout by packing larger rectangles first.
    /// </summary>
    /// <returns>
    /// The old and new rectangle for each allocation whose position changed.
    /// </returns>
    /// <remarks>
    /// The rebuild is transactional. All allocations are first packed into a
    /// temporary Guillotine layout. If any rectangle cannot be placed, this
    /// instance remains unchanged and an empty move list is returned.
    /// </remarks>
    public List<(Rect2 OldRect, Rect2 NewRect)> Defrag()
    {
        if (_packedRects.Count == 0)
            return [];

        var sorted = _packedRects
            .OrderByDescending(r => r.Width * r.Height)
            .ThenBy(r => r.Top)
            .ThenBy(r => r.Left)
            .ToList();

        var rebuilt = new GuillotinePacker(_width, _height);
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

        _freeRects.Clear();
        _freeRects.AddRange(rebuilt._freeRects);

        _packedRects.Clear();
        _packedRects.AddRange(rebuilt._packedRects);

        _usedSpace = rebuilt._usedSpace;
        ValidateState();

        return moves;
    }

    private void MergeFreeRects()
    {
        bool merged;
        do
        {
            merged = false;

            for (int i = 0; i < _freeRects.Count && !merged; i++)
            {
                for (int j = i + 1; j < _freeRects.Count; j++)
                {
                    if (!TryMerge(_freeRects[i], _freeRects[j], out Rect2 mergedRect))
                        continue;

                    _freeRects[i] = mergedRect;
                    _freeRects.RemoveAt(j);
                    merged = true;
                    break;
                }
            }
        } while (merged);
    }

    private static bool TryMerge(Rect2 a, Rect2 b, out Rect2 merged)
    {
        merged = default;

        if (a.Y == b.Y && a.Height == b.Height &&
            (a.Right == b.Left || b.Right == a.Left))
        {
            float left = MathF.Min(a.Left, b.Left);
            merged = new Rect2(left, a.Y, a.Width + b.Width, a.Height);
            return true;
        }

        if (a.X == b.X && a.Width == b.Width &&
            (a.Bottom == b.Top || b.Bottom == a.Top))
        {
            float top = MathF.Min(a.Top, b.Top);
            merged = new Rect2(a.X, top, a.Width, a.Height + b.Height);
            return true;
        }

        return false;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void ValidateState()
    {
        int calculatedUsedSpace = 0;

        for (int i = 0; i < _packedRects.Count; i++)
        {
            Rect2 packed = _packedRects[i];
            ValidateBounds(packed, "packed", i);
            calculatedUsedSpace += (int)(packed.Width * packed.Height);

            for (int j = i + 1; j < _packedRects.Count; j++)
            {
                if (packed.Intersects(_packedRects[j]))
                {
                    throw new InvalidOperationException(
                        $"GuillotinePacker produced overlapping packed rectangles at indices {i} and {j}.");
                }
            }

            for (int j = 0; j < _freeRects.Count; j++)
            {
                if (packed.Intersects(_freeRects[j]))
                {
                    throw new InvalidOperationException(
                        $"GuillotinePacker has packed rectangle {i} overlapping free rectangle {j}.");
                }
            }
        }

        if (calculatedUsedSpace != _usedSpace)
        {
            throw new InvalidOperationException(
                $"GuillotinePacker used-space mismatch: tracked {_usedSpace}, calculated {calculatedUsedSpace}.");
        }

        int calculatedFreeSpace = 0;
        for (int i = 0; i < _freeRects.Count; i++)
        {
            Rect2 free = _freeRects[i];
            ValidateBounds(free, "free", i);
            calculatedFreeSpace += (int)(free.Width * free.Height);

            for (int j = i + 1; j < _freeRects.Count; j++)
            {
                if (free.Intersects(_freeRects[j]))
                {
                    throw new InvalidOperationException(
                        $"GuillotinePacker produced overlapping free rectangles at indices {i} and {j}.");
                }
            }
        }

        if (calculatedUsedSpace + calculatedFreeSpace != TotalSpace)
        {
            throw new InvalidOperationException(
                $"GuillotinePacker area mismatch: used {calculatedUsedSpace}, free {calculatedFreeSpace}, total {TotalSpace}.");
        }
    }

    private void ValidateBounds(Rect2 rect, string kind, int index)
    {
        if (rect.Width <= 0 || rect.Height <= 0 ||
            rect.Left < 0 || rect.Top < 0 ||
            rect.Right > _width || rect.Bottom > _height)
        {
            throw new InvalidOperationException(
                $"GuillotinePacker {kind} rectangle {index} is outside the atlas bounds.");
        }
    }
}
