namespace Void.Engine.Assets.Loaders;

public enum AssetType
{
    None,
    Normal,
    Instanced,
    Atlas
}

public interface IAsset : IDisposable
{
    // NOTE: Assets use LRU

    uint Id { get; }
    string Tag { get; }
    byte[] Data { get; }
    bool IsValid { get; }
    AssetType Type { get; }
    DateTime LastAccessTime { get; }
    void Load();
    void Unload();
}
