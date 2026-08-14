namespace Void.Engine.Graphics.Atlas.Packers;

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

    public SkylinePacker(int width, int height)
    {
        _width = width;
        _height = height;
        _skyline = new List<SkylineNode> { new() { X = 0, Y = 0, Width = width } };
        _packedRects = new List<Rect2>();
        _usedSpace = 0;
    }

    public bool TryPack(int width, int height, out Rect2 packedRect)
    {
        packedRect = default;

        if (width > _width || height > _height)
            return false;

        int bestIndex = -1;
        int bestY = int.MaxValue;
        int bestWidth = int.MaxValue;

        for (int i = 0; i < _skyline.Count; i++)
        {
            int y = _skyline[i].Y;

            int currentX = _skyline[i].X;
            int currnetWidth = 0;
            int maxHeight = 0;

            for (int j = i; j < _skyline.Count; j++)
            {
                if (currentX + currnetWidth + width > _width)
                    break;

                currnetWidth += _skyline[j].Width;
                if (_skyline[j].Y > maxHeight)
                    maxHeight = _skyline[j].Y;

                if (currnetWidth >= width)
                    break;
            }

            if (currnetWidth >= width && y + height <= _height)
            {
                if (y < bestY || (y == bestY && currentX < bestWidth))
                {
                    bestY = y;
                    bestIndex = i;
                    bestWidth = currnetWidth;
                }
            }
        }

        if (bestIndex == -1)
            return false;

        int packX = _skyline[bestIndex].X;
        int packY = _skyline[bestIndex].Y;

        packedRect = new Rect2(packX, packY, width, height);
        _usedSpace += width * height;
        _packedRects.Add(packedRect);

        UpdateSkyline(bestIndex, packX, packY, width, height);

        return true;
    }

    public void Free(Rect2 rect)
    {
        _packedRects.Remove(rect);
        _usedSpace -= (int)(rect.Width * rect.Height);
        if (_usedSpace < 0) _usedSpace = 0;

        InsertFreeSpace(rect);
    }

    private void InsertFreeSpace(Rect2 rect)
    {
        int insertX = (int)rect.X;
        int insertY = (int)rect.Y;
        int insertWidth = (int)rect.Width;

        for (int i = 0; i < _skyline.Count; i++)
        {
            var node = _skyline[i];
            if (node.X <= insertX && node.X + node.Width >= insertX + insertWidth)
            {
                if (node.X < insertX)
                {
                    _skyline.Insert(i, new SkylineNode { X = node.X, Y = node.Y, Width = insertX - node.X });
                    i++;
                }

                _skyline.Insert(i, new SkylineNode { X = insertX, Y = insertY, Width = insertWidth });
                i++;

                int remainingX = insertX + insertWidth;
                if (remainingX < node.X + node.Width)
                {
                    _skyline.Insert(i, new SkylineNode { X = remainingX, Y = node.Y, Width = node.X + node.Width - remainingX });
                }

                _skyline.RemoveAt(i);
                break;
            }
        }

        MergeSkylineNodes();
    }

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

    public void Clear()
    {
        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
        _packedRects.Clear();
        _usedSpace = 0;
    }

    public void Defrag()
    {
        if (_packedRects.Count == 0) return;

        var sorted = _packedRects.OrderBy(r => r.Top).ThenBy(r => r.Left).ToList();

        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
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

    private void UpdateSkyline(int index, int x, int y, int width, int height)
    {
        var newNode = new SkylineNode { X = x, Y = y + height, Width = width };

        var existing = _skyline[index];
        if (existing.Width > width)
        {
            _skyline[index] = new SkylineNode { X = x + width, Y = existing.Y, Width = existing.Width - width };
            _skyline.Insert(index, newNode);
        }
        else
            _skyline[index] = newNode;

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
}