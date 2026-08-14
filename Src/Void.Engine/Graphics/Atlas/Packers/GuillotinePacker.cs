namespace Void.Engine.Graphics.Atlas.Packers;

public sealed class GuillotinePacker : IAtlasPacker
{
    private readonly int _width, _height;
    private readonly List<Rect2> _freeRects;
    private readonly List<Rect2> _packedRects;
    private int _usedSpace;

    public int UsedSpace => _usedSpace;
    public int TotalSpace => _width * _height;

    public float Fragmentation
    {
        get
        {
            if (TotalSpace == 0) return 0f;
            return 1f - ((float)_usedSpace / TotalSpace);
        }
    }

    public GuillotinePacker(int width, int height)
    {
        _width = width;
        _height = height;
        _freeRects = new List<Rect2> { new(0, 0, width, height) };
        _packedRects = new List<Rect2>();
        _usedSpace = 0;
    }

    public bool TryPack(int width, int height, out Rect2 packedRect)
    {
        packedRect = default;

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
            _freeRects.Add(new Rect2(freeRect.Left + width, freeRect.Top, remainingHeight, height));
        if (remainingHeight > 0)
            _freeRects.Add(new Rect2(freeRect.Left, freeRect.Top + height, freeRect.Width, remainingHeight));

        return true;
    }

    public void Free(Rect2 rect)
    {
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

    public void Clear()
    {
        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
        _packedRects.Clear();
        _usedSpace = 0;
    }

    public void Defrag()
    {
        if (_packedRects.Count == 0) return;

        var sorted = _packedRects.OrderByDescending(r => r.Width * r.Height).ToList();

        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
        _usedSpace = 0;

        var newRects = new List<Rect2>();
        foreach (var rect in sorted)
        {
            if (TryPack((int)rect.Width, (int)rect.Height, out var newRect))
            {
                newRects.Add(newRect);
            }
        }

        _packedRects.Clear();
        _packedRects.AddRange(newRects);
    }
}