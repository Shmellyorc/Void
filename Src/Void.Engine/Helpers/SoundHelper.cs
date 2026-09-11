using System;
using System.Collections.Generic;
using System.Linq;
using Void.Engine.Assets.Loaders;
using Void.Engine.Sounds;

namespace Void.Engine.Helpers;

/// <summary>
/// High-level sound helpers. Public behavior is unchanged; the underlying playback
/// implementation is now OpenAL instead of SFML.Audio.
/// </summary>
public static class SoundHelper
{
    private static float _masterVolume = 1f;
    private static readonly Dictionary<object, float> CategoryVolumes = new();
    private static readonly object VolumeLock = new();
    private static readonly Dictionary<string, Sound[]> SoundGroups = new();

    public static SoundInstancePool Pool => SoundInstancePool.Instance;

    public static float MasterVolume
    {
        get => _masterVolume;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (MathHelper.AlmostEquals(clamped, _masterVolume, MathHelper.Epsilon))
                return;

            _masterVolume = clamped;
            foreach (var instance in Pool.GetActiveInstances())
                instance.ApplyCategoryVolume(instance.Category);
        }
    }

    public static float GetCategoryVolume<T>(T category) where T : struct, Enum
    {
        lock (VolumeLock)
            return CategoryVolumes.TryGetValue(category, out float volume) ? volume : 1f;
    }

    public static void SetCategoryVolume<T>(T category, float volume) where T : struct, Enum
    {
        lock (VolumeLock)
            CategoryVolumes[category] = Math.Clamp(volume, 0f, 1f);

        foreach (var instance in Pool.GetActiveInstances())
        {
            if (instance.Category?.Equals(category) == true)
                instance.ApplyCategoryVolume(category);
        }
    }

    public static SoundInstance PlayPooled<T>(Sound sound, float volume = 1f, float pan = 0f,
        float pitch = 1f, T category = default) where T : struct, Enum
    {
        if (sound == null)
            throw new ArgumentNullException(nameof(sound));

        var instance = sound.CreateInstance(category);
        if (instance == null)
            return null;

        instance.Volume = volume;
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    public static SoundInstance PlayPooledWithVariation<T>(Sound sound, float pitchRange = 0.1f,
        float volume = 1f, float pan = 0f, T category = default) where T : struct, Enum
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return PlayPooled(sound, volume, pan, pitch, category);
    }

    public static void StopAll() => Pool.StopAll();
    public static void StopAll(string soundName) => Pool.StopAllInstances(soundName);
    public static void PauseAll() => Pool.PauseAll();
    public static void ResumeAll() => Pool.ResumeAll();

    public static float RandomPitch(float pitchRange = 0.1f)
        => FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);

    public static float RandomPan(float panRange = 1f)
        => FastRandom.Shared.RangeFloat(-panRange, panRange);

    public static int ActiveSoundCount => Pool.ActiveCount;
    public static int AvailableSoundCount => Pool.AvailableCount;
    public static int TotalSoundCount => Pool.TotalInstances;
    public static bool IsPoolExhausted => Pool.IsExhausted;

    public static void RegisterSoundGroup(string groupName, params Sound[] sounds)
        => SoundGroups[groupName] = sounds;

    public static SoundInstance PlayRandom(Sound[] sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds?.ToList();
        if (list == null || list.Count == 0)
            return null;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    public static SoundInstance PlayFromGroup<T>(string groupName, float volume = 1f, float pan = 0f,
        float pitch = 1f, T category = default, bool withVariation = false, float pitchRange = 0.1f)
        where T : struct, Enum
    {
        if (!SoundGroups.TryGetValue(groupName, out var sounds) || sounds.Length == 0)
            return null;

        var sound = sounds[FastRandom.Shared.Next(sounds.Length)];
        if (withVariation)
            pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);

        return sound.PlayOneShot(volume, pan, pitch, category);
    }
}
