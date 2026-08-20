using Void.Engine.Logs;

namespace Void.Engine.Assets.Loaders;

public enum SoundPriority
{
    Low = 0,      // Ambient, background noise
    Normal = 1,   // Default sounds (footsteps, gunshots)
    High = 2,     // UI sounds, important feedback
    Critical = 3  // Dialogue, quest updates, alarms
}

public sealed class Sound : IAsset
{
    private readonly Lock _lock = new();

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public AssetType Type { get; }
    public bool IsValid { get; private set; }
    public SoundPriority Priority { get; }
    public DateTime LastAccessTime { get; private set; }

    internal SFSoundBuffer Buffer { get; private set; }

    internal Sound(uint id, byte[] data, string tag, SoundPriority priority)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Priority = priority;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }
    ~Sound()
    {
        try
        {
            Buffer?.Dispose();
            Buffer = null;
            IsValid = false;
        }
        catch
        {
            // Ignore any exceptions during finalization
        }
    }

    public void Load()
    {
        lock (_lock)
        {
            if (IsValid)
            {
                LastAccessTime = DateTime.Now;
                return;
            }

            Buffer = new SFSoundBuffer(Data);

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

            Buffer?.Dispose();
            Buffer = null;

            IsValid = false;
        }
    }

    public SoundInstance CreateInstance(Enum category = null)
    {
        lock (_lock)
        {
            if (!IsValid)
            {
                Logger.Instance.WarningWithCategory("Sound",
                    "'{0}' was unloaded but CreateInstance() was called. Auto-loading...", Tag);
                Load();
            }

            LastAccessTime = DateTime.Now;

            var instance = SoundInstancePool.Instance.GetInstance();

            if (instance == null)
            {
                Logger.Instance.ErrorWithCategory("Sound",
                    "Sound pool exhausted! Cannot create instance for '{0}'", Tag);
                return null;
            }

            instance.Initialize(Buffer, category, Priority);
            instance.SoundName = Tag;
            return instance;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            Buffer?.Dispose();
            Buffer = null;
            IsValid = false;
        }

        GC.SuppressFinalize(this);
    }
}
