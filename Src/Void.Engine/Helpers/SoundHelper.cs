// ============================================================================
//  SoundHelper.cs
// ============================================================================
//  High-level helpers for pooled playback, volume categories, sound groups,
//  and common randomized playback values.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Void.Engine.Assets.Loaders;
using Void.Engine.Sounds;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides convenience APIs for pooled sound playback, global and category
/// volume control, sound groups, and randomized pitch or pan values.
/// </summary>
/// <remarks>
/// <para>
/// Category values are arbitrary enum values. A category that has not been assigned
/// a volume uses <c>1.0</c>. Master and category volumes are clamped to the range
/// zero through one.
/// </para>
/// <code>
/// SoundHelper.MasterVolume = 0.8f;
/// SoundHelper.SetCategoryVolume(AudioCategory.Ui, 0.6f);
/// SoundHelper.PlayPooled(clickSound, category: AudioCategory.Ui);
/// </code>
/// </remarks>
public static class SoundHelper
{
    private static float _masterVolume = 1f;
    private static readonly Dictionary<object, float> CategoryVolumes = new();
    private static readonly object VolumeLock = new();
    private static readonly Dictionary<string, Sound[]> SoundGroups = new();

    /// <summary>Gets the shared sound-instance pool.</summary>
    public static SoundInstancePool Pool => SoundInstancePool.Instance;

    /// <summary>
    /// Gets or sets the master volume applied to pooled sound instances.
    /// </summary>
    /// <remarks>The assigned value is clamped to the range zero through one.</remarks>
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

    /// <summary>Gets the configured volume for a sound category.</summary>
    /// <typeparam name="T">Enum type used to identify categories.</typeparam>
    /// <param name="category">Category whose volume should be read.</param>
    /// <returns>The configured category volume, or <c>1.0</c> when no value has been set.</returns>
    public static float GetCategoryVolume<T>(T category) where T : struct, Enum
    {
        lock (VolumeLock)
            return CategoryVolumes.TryGetValue(category, out float volume) ? volume : 1f;
    }

    /// <summary>Sets the volume for a sound category and updates active instances in that category.</summary>
    /// <typeparam name="T">Enum type used to identify categories.</typeparam>
    /// <param name="category">Category whose volume should be changed.</param>
    /// <param name="volume">Volume value, clamped to the range zero through one.</param>
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

    /// <summary>Creates and starts a pooled sound instance.</summary>
    /// <typeparam name="T">Enum type used to identify the sound category.</typeparam>
    /// <param name="sound">Sound asset to play.</param>
    /// <param name="volume">Instance volume.</param>
    /// <param name="pan">Stereo pan value.</param>
    /// <param name="pitch">Playback pitch.</param>
    /// <param name="category">Category assigned to the instance.</param>
    /// <returns>The started pooled instance, or <see langword="null"/> when no instance could be created.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sound"/> is <see langword="null"/>.</exception>
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

    /// <summary>Creates and starts a pooled sound with randomized pitch.</summary>
    /// <typeparam name="T">Enum type used to identify the sound category.</typeparam>
    /// <param name="sound">Sound asset to play.</param>
    /// <param name="pitchRange">Amount added to and subtracted from a base pitch of one.</param>
    /// <param name="volume">Instance volume.</param>
    /// <param name="pan">Stereo pan value.</param>
    /// <param name="category">Category assigned to the instance.</param>
    /// <returns>The started pooled instance, or <see langword="null"/> when no instance could be created.</returns>
    public static SoundInstance PlayPooledWithVariation<T>(Sound sound, float pitchRange = 0.1f,
        float volume = 1f, float pan = 0f, T category = default) where T : struct, Enum
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return PlayPooled(sound, volume, pan, pitch, category);
    }

    /// <summary>Stops every active pooled sound instance.</summary>
    public static void StopAll() => Pool.StopAll();

    /// <summary>Stops every active pooled instance with the specified sound name.</summary>
    /// <param name="soundName">Sound name to stop.</param>
    public static void StopAll(string soundName) => Pool.StopAllInstances(soundName);

    /// <summary>Pauses every active pooled sound instance.</summary>
    public static void PauseAll() => Pool.PauseAll();

    /// <summary>Resumes every paused pooled sound instance.</summary>
    public static void ResumeAll() => Pool.ResumeAll();

    /// <summary>Returns a randomized pitch around a base value of one.</summary>
    /// <param name="pitchRange">Amount added to and subtracted from one.</param>
    /// <returns>A random pitch in the requested range.</returns>
    public static float RandomPitch(float pitchRange = 0.1f)
        => FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);

    /// <summary>Returns a randomized stereo pan value around zero.</summary>
    /// <param name="panRange">Maximum absolute pan value.</param>
    /// <returns>A random pan value between negative and positive <paramref name="panRange"/>.</returns>
    public static float RandomPan(float panRange = 1f)
        => FastRandom.Shared.RangeFloat(-panRange, panRange);

    /// <summary>Gets the number of active pooled sound instances.</summary>
    public static int ActiveSoundCount => Pool.ActiveCount;

    /// <summary>Gets the number of currently available pooled sound instances.</summary>
    public static int AvailableSoundCount => Pool.AvailableCount;

    /// <summary>Gets the total number of instances owned by the sound pool.</summary>
    public static int TotalSoundCount => Pool.TotalInstances;

    /// <summary>Gets whether the sound pool currently has no available instance.</summary>
    public static bool IsPoolExhausted => Pool.IsExhausted;

    /// <summary>Registers or replaces a named collection of sounds.</summary>
    /// <param name="groupName">Name used to retrieve the group.</param>
    /// <param name="sounds">Sounds stored in the group.</param>
    public static void RegisterSoundGroup(string groupName, params Sound[] sounds)
        => SoundGroups[groupName] = sounds;

    /// <summary>Plays one randomly selected sound from an array.</summary>
    /// <param name="sounds">Candidate sounds.</param>
    /// <param name="volume">Playback volume.</param>
    /// <param name="pan">Stereo pan value.</param>
    /// <param name="pitch">Playback pitch.</param>
    /// <returns>The started instance, or <see langword="null"/> when the array is null or empty.</returns>
    public static SoundInstance PlayRandom(Sound[] sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds?.ToList();
        if (list == null || list.Count == 0)
            return null;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    /// <summary>Plays one randomly selected sound from a registered group.</summary>
    /// <typeparam name="T">Enum type used to identify the sound category.</typeparam>
    /// <param name="groupName">Registered sound-group name.</param>
    /// <param name="volume">Playback volume.</param>
    /// <param name="pan">Stereo pan value.</param>
    /// <param name="pitch">Base playback pitch.</param>
    /// <param name="category">Category assigned to the instance.</param>
    /// <param name="withVariation">Whether to randomize pitch before playback.</param>
    /// <param name="pitchRange">Amount added to and subtracted from the base pitch when variation is enabled.</param>
    /// <returns>The started instance, or <see langword="null"/> when the group is missing or empty.</returns>
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
