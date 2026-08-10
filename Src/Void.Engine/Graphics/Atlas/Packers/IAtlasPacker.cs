namespace Void.Engine.Graphics.Atlas;

public interface IAtlasPacker
{
    bool TryPack(int width, int height, out Rect2 packedRect);
    void Clear();
    void Free(Rect2 rect);
    int UsedSpace { get; }
    int TotalSpace { get; }
}
