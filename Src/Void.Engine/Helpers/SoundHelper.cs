namespace Void.Engine.Helpers;

public static class SoundHelper
{
    // Pool access
    public static SoundInstancePool Pool => SoundInstancePool.Instance;

    // Master volume - use SFML's built-in Listener
    public static float MasterVolume
    {
        get => SFML.Audio.Listener.GlobalVolume / 100f;
        set => SFML.Audio.Listener.GlobalVolume = Math.Clamp(value, 0f, 1f) * 100f;
    }

    // Category volumes dictionary
    private static readonly Dictionary<object, float> CategoryVolumes = new();
    private static readonly object _volumeLock = new();

    public static float GetCategoryVolume<T>(T category) where T : struct, Enum
    {
        lock (_volumeLock)
        {
            return CategoryVolumes.TryGetValue(category, out float volume) ? volume : 1f;
        }
    }

    public static void SetCategoryVolume<T>(T category, float volume) where T : struct, Enum
    {
        lock (_volumeLock)
        {
            CategoryVolumes[category] = Math.Clamp(volume, 0f, 1f);
        }

        var activeInstances = Pool.GetActiveInstances();
        foreach (var instance in activeInstances)
        {
            if (instance.Category?.Equals(category) == true)
            {
                instance.ApplyCategoryVolume(category);
            }
        }
    }

    // Pool-based playback
    public static SoundInstance PlayPooled<T>(Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, T category = default) where T : struct, Enum
    {
        var instance = Pool.GetInstance();
        if (instance == null)
            return null;

        instance.Initialize(sound.Buffer, category, sound.Priority); 
        instance.SoundName = sound.Tag;
        instance.Volume = volume;
        instance.ApplyCategoryVolume(category);
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    public static SoundInstance PlayPooledWithVariation<T>(Sound sound, float pitchRange = 0.1f, float volume = 1f, float pan = 0f, T category = default) where T : struct, Enum
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return PlayPooled(sound, volume, pan, pitch, category);
    }

    // Batch operations
    public static void StopAll() => Pool.StopAll();
    public static void StopAll(string soundName) => Pool.StopAllInstances(soundName);
    public static void PauseAll() => Pool.PauseAll();
    public static void ResumeAll() => Pool.ResumeAll();

    // Utility methods
    public static float RandomPitch(float pitchRange = 0.1f)
        => FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);

    public static float RandomPan(float panRange = 1f)
        => FastRandom.Shared.RangeFloat(-panRange, panRange);

    // Debug/analytics
    public static int ActiveSoundCount => Pool.ActiveCount;
    public static int AvailableSoundCount => Pool.AvailableCount;
    public static int TotalSoundCount => Pool.TotalInstances;
    public static bool IsPoolExhausted => Pool.IsExhausted;

    // Sound groups
    private static readonly Dictionary<string, Sound[]> SoundGroups = new();

    public static void RegisterSoundGroup(string groupName, params Sound[] sounds)
    {
        SoundGroups[groupName] = sounds;
    }

    public static SoundInstance PlayFromGroup<T>(string groupName, float volume = 1f, float pan = 0f, float pitch = 1f, T category = default, bool withVariation = false,
    float pitchRange = 0.1f) where T : struct, Enum
    {
        if (!SoundGroups.TryGetValue(groupName, out var sounds))
            return null;

        var sound = sounds[FastRandom.Shared.Next(sounds.Length)];

        if (withVariation)
        {
            float randomPitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
            return sound.PlayOneShot(volume, pan, randomPitch, category);
        }

        return sound.PlayOneShot(volume, pan, pitch, category);
    }
}