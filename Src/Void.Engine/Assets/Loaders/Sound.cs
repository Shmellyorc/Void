// ============================================================================
//  Sound.cs
// ============================================================================
//  Encoded sound asset with lazy backend audio-buffer creation.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Audio;

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Defines the relative importance of a sound when audio instances are limited.
/// </summary>
public enum SoundPriority
{
    /// <summary>
    /// Marks a sound as low priority.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Marks a sound as normal priority.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Marks a sound as high priority.
    /// </summary>
    High = 2,

    /// <summary>
    /// Marks a sound as critical priority.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents an encoded sound asset that can create playable <see cref="SoundInstance"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// The original encoded bytes remain in <see cref="Data"/> so the backend audio buffer can be recreated after eviction.
/// Calling <see cref="CreateInstance"/> automatically reloads the sound when necessary.
/// </para>
/// <code>
/// Sound sound = AssetManager.Instance.LoadSound("Audio/hit.wav", SoundPriority.High);
/// SoundInstance instance = sound.CreateInstance();
/// instance?.Play();
/// </code>
/// </remarks>
public sealed class Sound : IAsset
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets the identifier assigned to this sound asset.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized asset tag or source path.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the original encoded sound bytes retained for reloading.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the lifecycle type for this asset.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets whether the decoded backend audio buffer is currently available.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the priority used when requesting instances from the sound pool.
    /// </summary>
    public SoundPriority Priority { get; }

    /// <summary>
    /// Gets the most recent time this sound was loaded or used to create an instance.
    /// </summary>
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

    /// <summary>
    /// Releases the backend audio buffer if the asset was not disposed explicitly.
    /// </summary>
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

    /// <summary>
    /// Decodes the source data and creates the backend audio buffer.
    /// </summary>
    /// <remarks>
    /// Calling this method on an already loaded sound only refreshes <see cref="LastAccessTime"/>.
    /// </remarks>
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

    /// <summary>
    /// Releases the backend audio buffer while retaining the encoded source data for later reloading.
    /// </summary>
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

    /// <summary>
    /// Creates a pooled playback instance for this sound.
    /// </summary>
    /// <param name="category">An optional game-defined category associated with the created instance.</param>
    /// <returns>A configured sound instance, or <see langword="null"/> when the sound pool cannot provide one.</returns>
    /// <remarks>
    /// The asset is loaded automatically if it was previously evicted or unloaded.
    /// </remarks>
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

    /// <summary>
    /// Releases resources owned by this sound asset.
    /// </summary>
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
