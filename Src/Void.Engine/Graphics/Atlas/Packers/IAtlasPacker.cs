namespace Void.Engine.Graphics.Atlas;

public interface IAtlasPacker
{
    bool TryPack(int width, int height, out Rect2 packedRect);
    void Clear();
    void Defrag();
    void Free(Rect2 rect);
    float Fragmentation { get; }
    int UsedSpace { get; }
    int TotalSpace { get; }
}
