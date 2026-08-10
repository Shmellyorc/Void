namespace Void.Engine.Graphics.Atlas;

public struct AtlasMetrics
{
    public int TotalPages { get; internal set; }
    public int UsedPages { get; internal set; }
    public int TotalSpaceBytes { get; internal set; }
    public int UsedSpaceBytes { get; internal set; }
    public float PercentageFull { get; internal set; }
    public int TextureCount { get; internal set; }
    public int EvictionCount { get; internal set; }
}
