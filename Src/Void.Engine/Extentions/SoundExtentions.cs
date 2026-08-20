namespace System;

public static class SoundExtensions
{
    public static SoundInstance PlayOneShot(this Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, Enum category = null)
    {
        var instance = sound.CreateInstance(category);
        instance.Volume = volume;
        instance.ApplyCategoryVolume(category);
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    public static SoundInstance PlayAndForget(this Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, Enum category = null)
    {
        var instance = sound.PlayOneShot(volume, pan, pitch, category);

        // Proper cleanup
        instance.SoundCompleted += (_, _) => instance.Dispose();
        instance.SoundStopped += (_, _) => instance.Dispose();

        return instance;
    }

    public static SoundInstance PlayWithPitchVariation(this Sound sound, float pitchRange = 0.1f, float volume = 1f, float pan = 0f, Enum category = null)
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return sound.PlayOneShot(volume, pan, pitch, category);
    }

    // Fluent interface
    public static SoundInstance WithVolume(this SoundInstance instance, float volume)
    {
        instance.Volume = volume;
        return instance;
    }

    public static SoundInstance WithPan(this SoundInstance instance, float pan)
    {
        instance.Pan = pan;
        return instance;
    }

    public static SoundInstance WithPitch(this SoundInstance instance, float pitch)
    {
        instance.Pitch = pitch;
        return instance;
    }

    public static SoundInstance WithLooping(this SoundInstance instance, bool looping)
    {
        instance.Looping = looping;
        return instance;
    }

    public static SoundInstance PlayWith(this SoundInstance instance, float volume, float pan = 0f, float pitch = 1f)
    {
        instance.Volume = volume;
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    public static void StopAndDispose(this SoundInstance instance)
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.Stop();
            instance.Dispose();
        }
    }

    public static List<SoundInstance> PlayAll(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        return sounds.Select(s => s.PlayOneShot(volume, pan, pitch)).ToList();
    }

    public static void PlayAllAndForget(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        foreach (var sound in sounds)
            sound.PlayAndForget(volume, pan, pitch);
    }

    public static SoundInstance PlayRandom(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    public static SoundInstance PlayRandomWithVariation(this IEnumerable<Sound> sounds, float pitchRange = 0.1f, float volume = 1f, float pan = 0f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null;

        return list[FastRandom.Shared.Next(list.Count)].PlayWithPitchVariation(pitchRange, volume, pan);
    }

    public static SoundInstance PlayRandomAndForget(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var instance = sounds.PlayRandom(volume, pan, pitch);
        if (instance != null)
        {
            instance.SoundCompleted += (_, _) => instance.Dispose();
            instance.SoundStopped += (_, _) => instance.Dispose();
        }
        return instance;
    }

    public static SoundInstance PlayRandomWithVariationAndForget(this IEnumerable<Sound> sounds, float pitchRange = 0.1f, float volume = 1f, float pan = 0f)
    {
        var instance = sounds.PlayRandomWithVariation(pitchRange, volume, pan);
        if (instance != null)
        {
            instance.SoundCompleted += (_, _) => instance.Dispose();
            instance.SoundStopped += (_, _) => instance.Dispose();
        }
        return instance;
    }
}