using System;
using System.Threading;
using Void.Engine.Audio;
using Void.Engine.Logs;
using Void.Engine.Sounds;

namespace Void.Engine.Assets.Loaders;

public enum SoundPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Encoded sound asset. Data remains the original encoded bytes while the decoded
/// backend buffer is created lazily and recreated after eviction when required.
/// </summary>
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

    internal AudioBuffer Buffer { get; private set; }

    internal Sound(uint id, byte[] data, string tag, SoundPriority priority)
    {
        Id = id;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Tag = tag;
        Priority = priority;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    ~Sound()
    {
        try
        {
            Buffer?.Release();
            Buffer = null;
            IsValid = false;
        }
        catch
        {
            // Finalizers are best-effort only.
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

            Buffer = AudioBuffer.Create(Data);
            LastAccessTime = DateTime.Now;
            IsValid = true;
        }
    }

    public void Unload()
    {
        lock (_lock)
        {
            if (!IsValid)
                return;

            Buffer?.Release();
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

            var instance = SoundInstancePool.Instance.GetInstance(Priority);
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
            Buffer?.Release();
            Buffer = null;
            IsValid = false;
        }

        GC.SuppressFinalize(this);
    }
}
