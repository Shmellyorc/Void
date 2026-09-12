// ============================================================================
//  SoundExtensions.cs
// ============================================================================
//  Playback and fluent configuration helpers for Sound and SoundInstance.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace System;

/// <summary>
/// Provides convenience playback and configuration helpers for <see cref="Sound"/> and <see cref="SoundInstance"/>.
/// </summary>
public static class SoundExtensions
{
    /// <summary>
    /// Creates, configures, and starts a sound instance.
    /// </summary>
    /// <param name="sound">The sound asset used to create the instance.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The stereo pan value.</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The optional category used for category volume.</param>
    /// <returns>The playing sound instance.</returns>
    public static SoundInstance PlayOneShot(this Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, Enum category = null!)
    {
        var instance = sound.CreateInstance(category);
        instance.Volume = volume;
        instance.ApplyCategoryVolume(category);
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    /// <summary>
    /// Plays a one-shot sound and disposes its instance after completion or an explicit stop.
    /// </summary>
    /// <param name="sound">The sound asset used to create the instance.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The stereo pan value.</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The optional category used for category volume.</param>
    /// <returns>The playing sound instance.</returns>
    public static SoundInstance PlayAndForget(this Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, Enum category = null!)
    {
        var instance = sound.PlayOneShot(volume, pan, pitch, category);

        instance.SoundCompleted += (_, _) => instance.Dispose();
        instance.SoundStopped += (_, _) => instance.Dispose();

        return instance;
    }

    /// <summary>
    /// Plays a sound with a random pitch centered on 1.
    /// </summary>
    /// <param name="sound">The sound asset used to create the instance.</param>
    /// <param name="pitchRange">The amount subtracted from and added to 1 when generating the pitch.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The stereo pan value.</param>
    /// <param name="category">The optional category used for category volume.</param>
    /// <returns>The playing sound instance.</returns>
    public static SoundInstance PlayWithPitchVariation(this Sound sound, float pitchRange = 0.1f, float volume = 1f, float pan = 0f, Enum category = null!)
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return sound.PlayOneShot(volume, pan, pitch, category);
    }

    /// <summary>
    /// Sets the instance volume and returns the same instance.
    /// </summary>
    /// <param name="instance">The instance to configure.</param>
    /// <param name="volume">The new volume.</param>
    /// <returns><paramref name="instance"/>.</returns>
    public static SoundInstance WithVolume(this SoundInstance instance, float volume)
    {
        instance.Volume = volume;
        return instance;
    }

    /// <summary>
    /// Sets the instance pan and returns the same instance.
    /// </summary>
    /// <param name="instance">The instance to configure.</param>
    /// <param name="pan">The new stereo pan value.</param>
    /// <returns><paramref name="instance"/>.</returns>
    public static SoundInstance WithPan(this SoundInstance instance, float pan)
    {
        instance.Pan = pan;
        return instance;
    }

    /// <summary>
    /// Sets the instance pitch and returns the same instance.
    /// </summary>
    /// <param name="instance">The instance to configure.</param>
    /// <param name="pitch">The new pitch multiplier.</param>
    /// <returns><paramref name="instance"/>.</returns>
    public static SoundInstance WithPitch(this SoundInstance instance, float pitch)
    {
        instance.Pitch = pitch;
        return instance;
    }

    /// <summary>
    /// Sets the instance looping state and returns the same instance.
    /// </summary>
    /// <param name="instance">The instance to configure.</param>
    /// <param name="looping"><see langword="true"/> to loop playback; otherwise, <see langword="false"/>.</param>
    /// <returns><paramref name="instance"/>.</returns>
    public static SoundInstance WithLooping(this SoundInstance instance, bool looping)
    {
        instance.Looping = looping;
        return instance;
    }

    /// <summary>
    /// Applies volume, pan, and pitch, starts playback, and returns the same instance.
    /// </summary>
    /// <param name="instance">The instance to configure and play.</param>
    /// <param name="volume">The new volume.</param>
    /// <param name="pan">The new stereo pan value.</param>
    /// <param name="pitch">The new pitch multiplier.</param>
    /// <returns><paramref name="instance"/>.</returns>
    public static SoundInstance PlayWith(this SoundInstance instance, float volume, float pan = 0f, float pitch = 1f)
    {
        instance.Volume = volume;
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    /// <summary>
    /// Stops and disposes an instance when it is non-null and has not already been disposed.
    /// </summary>
    /// <param name="instance">The sound instance to stop and dispose.</param>
    public static void StopAndDispose(this SoundInstance instance)
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.Stop();
            instance.Dispose();
        }
    }

    /// <summary>
    /// Plays every sound in the sequence as a one-shot instance.
    /// </summary>
    /// <param name="sounds">The sounds to play.</param>
    /// <param name="volume">The volume applied to each instance.</param>
    /// <param name="pan">The pan applied to each instance.</param>
    /// <param name="pitch">The pitch applied to each instance.</param>
    /// <returns>The created instances in sequence order.</returns>
    public static List<SoundInstance> PlayAll(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        return sounds.Select(s => s.PlayOneShot(volume, pan, pitch)).ToList();
    }

    /// <summary>
    /// Plays every sound in the sequence and arranges for each instance to dispose after completion or stop.
    /// </summary>
    /// <param name="sounds">The sounds to play.</param>
    /// <param name="volume">The volume applied to each instance.</param>
    /// <param name="pan">The pan applied to each instance.</param>
    /// <param name="pitch">The pitch applied to each instance.</param>
    public static void PlayAllAndForget(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        foreach (var sound in sounds)
            sound.PlayAndForget(volume, pan, pitch);
    }

    /// <summary>
    /// Selects and plays one random sound from the sequence.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The instance pan.</param>
    /// <param name="pitch">The instance pitch.</param>
    /// <returns>The created instance, or <see langword="null"/> when the sequence is empty.</returns>
    public static SoundInstance PlayRandom(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null!;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    /// <summary>
    /// Selects and plays one random sound with random pitch variation.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="pitchRange">The amount subtracted from and added to 1 when generating pitch.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The instance pan.</param>
    /// <returns>The created instance, or <see langword="null"/> when the sequence is empty.</returns>
    public static SoundInstance PlayRandomWithVariation(this IEnumerable<Sound> sounds, float pitchRange = 0.1f, float volume = 1f, float pan = 0f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null!;

        return list[FastRandom.Shared.Next(list.Count)].PlayWithPitchVariation(pitchRange, volume, pan);
    }

    /// <summary>
    /// Selects and plays one random sound and disposes the instance after completion or stop.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The instance pan.</param>
    /// <param name="pitch">The instance pitch.</param>
    /// <returns>The created instance, or <see langword="null"/> when the sequence is empty.</returns>
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

    /// <summary>
    /// Selects and plays one random sound with pitch variation and disposes the instance after completion or stop.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="pitchRange">The amount subtracted from and added to 1 when generating pitch.</param>
    /// <param name="volume">The instance volume.</param>
    /// <param name="pan">The instance pan.</param>
    /// <returns>The created instance, or <see langword="null"/> when the sequence is empty.</returns>
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
