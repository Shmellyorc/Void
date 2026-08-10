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
    private int _usedSpace;

    public int UsedSpace => _usedSpace;
    public int TotalSpace => _width * _height;

    public SkylinePacker(int width, int height)
    {
        _width = width;
        _height = height;
        _skyline = new List<SkylineNode> { new() { X = 0, Y = 0, Width = width } };
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

        UpdateSkyline(bestIndex, packX, packY, width, height);

        return true;
    }

    public void Free(Rect2 rect)
    {
        // Mark space as free, for simplicity, we just reduce useds space
        // the skyline will naturally resue space when repacking
        _usedSpace -= (int)(rect.Width * rect.Height);

        if (_usedSpace < 0)
            _usedSpace = 0;
    }

    public void Clear()
    {
        _skyline.Clear();
        _skyline.Add(new SkylineNode { X = 0, Y = 0, Width = _width });
        _usedSpace = 0;
    }

    private void UpdateSkyline(int index, int x, int y, int width, int height)
    {
        // Insert new node at the packed position
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
