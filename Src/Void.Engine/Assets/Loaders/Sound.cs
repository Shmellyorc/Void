namespace Void.Engine.Assets.Loaders;

public sealed class Sound : IAsset
{
    private SFSoundBuffer _buffer;
    private readonly Lock _lock = new();

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public AssetType Type { get; }
    public bool IsValid { get; private set; }
    public DateTime LastAccessTime { get; private set; }

    internal Sound(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }
    ~Sound() => Dispose();

    public void Load()
    {
        lock (_lock)
        {
            if (IsValid)
            {
                LastAccessTime = DateTime.Now;
                return;
            }

            _buffer = new SFSoundBuffer(Data);

            IsValid = true;
            LastAccessTime = DateTime.Now;
        }
    }

    public void Unload()
    {
        lock (_lock)
        {
            if (!IsValid)
                return;

            _buffer?.Dispose();
            _buffer = null;

            IsValid = false;
        }
    }

    public SoundInstance CreateInstance()
    {
        lock (_lock)
        {
            if (!IsValid)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[Sound] Warning: '{Tag}' was unloaded but CreateInstance() was called. Auto-loading..."
                );
#endif
                Load();
            }

            var instance = SoundInstancePool.Instance.GetInstance();

            instance.Initialize(_buffer);
            instance.SoundName = Tag;
            return instance;
        }

    }

    public void Dispose()
    {
        lock (_lock)
        {
            _buffer?.Dispose();
            _buffer = null;
            IsValid = false;
        }

        GC.SuppressFinalize(this);
    }
}
