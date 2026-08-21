// ============================================================================
//  Sound.cs
// ============================================================================
//  Sound asset that wraps audio data and provides factory methods for creating
//  playable sound instances with priority and category support.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Defines priority levels for sound instances to manage voice allocation
/// when the audio system reaches its maximum concurrent voices.
/// </summary>
public enum SoundPriority
{
    /// <summary>
    /// Low priority sounds such as ambient noise and background audio.
    /// These are the first to be culled when voice limits are reached.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority for default sounds including footsteps, gunshots,
    /// and other standard gameplay audio.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority for UI sounds and important feedback that should
    /// not be easily interrupted.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority for dialogue, quest updates, alarms, and other
    /// essential audio that must play whenever possible.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents a sound asset that can be loaded, unloaded, and used to create
/// playable sound instances.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Sound"/> class implements <see cref="IAsset"/> and manages
/// the underlying audio data buffer. It serves as a factory for creating
/// <see cref="SoundInstance"/> objects that can be played through the
/// audio system.
/// </para>
/// <para>
/// Sound assets are typically loaded through the <see cref="AssetManager"/>
/// using the <c>Load&lt;Sound&gt;()</c> method. Once loaded, the asset can be
/// reused to create multiple independent sound instances without duplicating
/// the audio data in memory.
/// </para>
/// <para>
/// <b>Loading Process:</b>
/// <list type="number">
///   <item><description>AssetManager searches mounts for the requested audio file</description></item>
///   <item><description>File data is read as a byte array</description></item>
///   <item><description>Sound constructor stores the data but does not decode it</description></item>
///   <item><description><see cref="Load()"/> creates the SFML sound buffer from the data</description></item>
///   <item><description>Sound is cached in the AssetManager for future use</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Supported Audio Formats:</b>
/// .ogg, .wav, .flac, .mp3, .aiff, .au, .raw, .paf, .svx, .nist, .voc,
/// .ircam, .w64, .mat4, .mat5, .pvf, .htk, .sds, .avr, .sd2, .caf, .wve,
/// .mpc2k, .rf64
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Load sound asset from the content system
/// var soundAsset = AssetManager.Instance.Load&lt;Sound&gt;("explosion.wav");
/// 
/// // Create and play an instance
/// var instance = soundAsset.CreateInstance(SoundCategory.SFX);
/// instance.Volume = 0.8f;
/// instance.Play();
/// 
/// // Create multiple instances from the same asset
/// var instance2 = soundAsset.CreateInstance(SoundCategory.SFX);
/// instance2.Pitch = 1.2f;
/// instance2.Play();
/// </code>
/// </para>
/// <para>
/// <b>Asset Caching and Eviction:</b>
/// Sound assets are cached by the AssetManager and automatically evicted after
/// a configurable idle time (<see cref="GameSettings.AssetEvictionMinutes"/>).
/// When evicted, <see cref="Unload()"/> is called to release the audio buffer.
/// The asset will be reloaded automatically if <see cref="CreateInstance"/>
/// is called while unloaded.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class uses a lock to ensure thread-safe access to the underlying
/// buffer during load, unload, and instance creation operations.
/// </para>
/// </remarks>
public sealed class Sound : IAsset
{
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets the unique identifier of the sound asset.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the asset.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the raw audio data bytes of the sound.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the asset type.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the sound is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the priority level of the sound.
    /// </summary>
    public SoundPriority Priority { get; }

    /// <summary>
    /// Gets the last access time of the asset for eviction tracking.
    /// </summary>
    public DateTime LastAccessTime { get; private set; }

    /// <summary>
    /// Gets the underlying SFML sound buffer containing the decoded audio data.
    /// </summary>
    internal SFSoundBuffer Buffer { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sound"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the asset.</param>
    /// <param name="data">The raw audio data bytes.</param>
    /// <param name="tag">The normalized path or tag used to identify the asset.</param>
    /// <param name="priority">The priority level of the sound.</param>
    internal Sound(uint id, byte[] data, string tag, SoundPriority priority)
    {
        Id = id;
        Data = data;
        Tag = tag;
        Priority = priority;
        Type = AssetType.Normal;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Finalizer that ensures resources are cleaned up if <see cref="Dispose"/> wasn't called.
    /// </summary>
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

    /// <summary>
    /// Loads the sound data into memory by creating the SFML sound buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method decodes the raw audio data and creates the native sound buffer.
    /// It is called automatically by the AssetManager when the asset is loaded
    /// or when a previously unloaded asset is accessed.
    /// </para>
    /// <para>
    /// If the sound is already loaded, this method simply updates the last access time.
    /// </para>
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

            Buffer = new SFSoundBuffer(Data);

            IsValid = true;
            LastAccessTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Unloads the sound data from memory by disposing the sound buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method releases the native sound buffer to free memory. It is called
    /// automatically by the AssetManager during asset eviction.
    /// </para>
    /// <para>
    /// The raw audio data remains in memory and can be reloaded later if needed.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Creates a new playable sound instance from this asset.
    /// </summary>
    /// <param name="category">The category of the sound for volume grouping.</param>
    /// <returns>A new <see cref="SoundInstance"/> ready to play, or <see langword="null"/> if the sound pool is exhausted.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a new sound instance that can be played independently.
    /// If the sound is currently unloaded, it will be automatically reloaded.
    /// </para>
    /// <para>
    /// The instance is obtained from the <see cref="SoundInstancePool"/> to avoid
    /// frequent allocations. If the pool is exhausted, this method returns
    /// <see langword="null"/> and logs an error.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var instance = soundAsset.CreateInstance(SoundCategory.Music);
    /// instance.Volume = 0.5f;
    /// instance.Looping = true;
    /// instance.Play();
    /// </code>
    /// </para>
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

    /// <summary>
    /// Disposes the sound asset and releases all resources.
    /// </summary>
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