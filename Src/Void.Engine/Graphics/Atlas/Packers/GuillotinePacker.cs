namespace Void.Engine.Graphics.Atlas.Packers;

public sealed class GuillotinePacker : IAtlasPacker
{
    private readonly int _width, _height;
    private readonly List<Rect2> _freeRects;
    private int _usedSpace;

    public int UsedSpace => _usedSpace;
    public int TotalSpace => _width * _height;

    public GuillotinePacker(int width, int height)
    {
        _width = width;
        _height = height;
        _freeRects = new List<Rect2> { new(0, 0, width, height) };
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
        _freeRects.Add(rect);
        _usedSpace -= (int)(rect.Width * rect.Height);

        if (_usedSpace < 0)
            _usedSpace = 0;
    }

    public void Clear()
    {
        _freeRects.Clear();
        _freeRects.Add(new Rect2(0, 0, _width, _height));
        _usedSpace = 0;
    }
}
