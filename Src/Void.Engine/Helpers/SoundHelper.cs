// ============================================================================
//  SoundHelper.cs
// ============================================================================
//  Utility methods for sound playback including pool management, volume
//  control, category volumes, and sound group playback.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides utility methods for sound playback including pool management,
/// volume control, category volumes, and sound group playback.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SoundHelper"/> class provides a convenient interface for
/// playing sounds through the <see cref="SoundInstancePool"/> with additional
/// features including category-based volume control and sound groups.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Pool access and management</description></item>
///   <item><description>Master volume control via SFML listener</description></item>
///   <item><description>Category-based volume control for different sound types</description></item>
///   <item><description>Pooled sound playback with optional variation</description></item>
///   <item><description>Sound groups for random selection</description></item>
///   <item><description>Batch operations (stop all, pause all, resume all)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Set master volume
/// SoundHelper.MasterVolume = 0.8f;
/// 
/// // Set category volumes
/// SoundHelper.SetCategoryVolume(SoundCategory.SFX, 0.9f);
/// SoundHelper.SetCategoryVolume(SoundCategory.Music, 0.5f);
/// 
/// // Play a sound from the pool
/// var sound = SoundHelper.PlayPooled(mySound, 0.8f, 0f, 1f, SoundCategory.SFX);
/// 
/// // Play with random pitch variation
/// var varied = SoundHelper.PlayPooledWithVariation(mySound, 0.15f, 0.8f, 0f, SoundCategory.SFX);
/// 
/// // Register and play from a sound group
/// SoundHelper.RegisterSoundGroup("footsteps", footstep1, footstep2, footstep3);
/// SoundHelper.PlayFromGroup("footsteps", 0.7f, 0f, 1f, SoundCategory.SFX, true);
/// 
/// // Batch operations
/// SoundHelper.StopAll();
/// SoundHelper.PauseAll();
/// SoundHelper.ResumeAll();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe for category volume operations. Other methods
/// should be called from the main thread.
/// </para>
/// </remarks>
public static class SoundHelper
{
    /// <summary>
    /// Gets the sound instance pool.
    /// </summary>
    public static SoundInstancePool Pool => SoundInstancePool.Instance;

    /// <summary>
    /// Gets or sets the master volume of all sounds.
    /// </summary>
    public static float MasterVolume
    {
        get => SFML.Audio.Listener.GlobalVolume / 100f;
        set => SFML.Audio.Listener.GlobalVolume = Math.Clamp(value, 0f, 1f) * 100f;
    }

    private static readonly Dictionary<object, float> CategoryVolumes = new();
    private static readonly object _volumeLock = new();

    /// <summary>
    /// Gets the volume for the specified category.
    /// </summary>
    /// <typeparam name="T">The category enum type.</typeparam>
    /// <param name="category">The category to get the volume for.</param>
    /// <returns>The category volume between 0 and 1, or 1 if not set.</returns>
    public static float GetCategoryVolume<T>(T category) where T : struct, Enum
    {
        lock (_volumeLock)
        {
            return CategoryVolumes.TryGetValue(category, out float volume) ? volume : 1f;
        }
    }

    /// <summary>
    /// Sets the volume for the specified category and updates all active instances.
    /// </summary>
    /// <typeparam name="T">The category enum type.</typeparam>
    /// <param name="category">The category to set the volume for.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
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

    /// <summary>
    /// Plays a sound from the pool.
    /// </summary>
    /// <typeparam name="T">The category enum type.</typeparam>
    /// <param name="sound">The sound asset to play.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <returns>The sound instance, or null if the pool is exhausted.</returns>
    public static SoundInstance PlayPooled<T>(Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, T category = default) where T : struct, Enum
    {
        var instance = Pool.GetInstance();
        if (instance == null)
            return null!;

        instance.Initialize(sound.Buffer, category, sound.Priority);
        instance.SoundName = sound.Tag;
        instance.Volume = volume;
        instance.ApplyCategoryVolume(category);
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    /// <summary>
    /// Plays a sound from the pool with random pitch variation.
    /// </summary>
    /// <typeparam name="T">The category enum type.</typeparam>
    /// <param name="sound">The sound asset to play.</param>
    /// <param name="pitchRange">The pitch variation range (± from 1).</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <returns>The sound instance, or null if the pool is exhausted.</returns>
    public static SoundInstance PlayPooledWithVariation<T>(Sound sound, float pitchRange = 0.1f, float volume = 1f, float pan = 0f, T category = default) where T : struct, Enum
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return PlayPooled(sound, volume, pan, pitch, category);
    }

    /// <summary>
    /// Stops all active sounds.
    /// </summary>
    public static void StopAll() => Pool.StopAll();

    /// <summary>
    /// Stops all active sounds with the specified name.
    /// </summary>
    /// <param name="soundName">The name of the sound to stop.</param>
    public static void StopAll(string soundName) => Pool.StopAllInstances(soundName);

    /// <summary>
    /// Pauses all active sounds.
    /// </summary>
    public static void PauseAll() => Pool.PauseAll();

    /// <summary>
    /// Resumes all paused sounds.
    /// </summary>
    public static void ResumeAll() => Pool.ResumeAll();

    /// <summary>
    /// Generates a random pitch value within the specified range.
    /// </summary>
    /// <param name="pitchRange">The pitch variation range (± from 1).</param>
    /// <returns>A random pitch value.</returns>
    public static float RandomPitch(float pitchRange = 0.1f)
        => FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);

    /// <summary>
    /// Generates a random pan value within the specified range.
    /// </summary>
    /// <param name="panRange">The pan range (0 to 1).</param>
    /// <returns>A random pan value between -<paramref name="panRange"/> and <paramref name="panRange"/>.</returns>
    public static float RandomPan(float panRange = 1f)
        => FastRandom.Shared.RangeFloat(-panRange, panRange);

    /// <summary>
    /// Gets the number of currently active sounds.
    /// </summary>
    public static int ActiveSoundCount => Pool.ActiveCount;

    /// <summary>
    /// Gets the number of available sound instances in the pool.
    /// </summary>
    public static int AvailableSoundCount => Pool.AvailableCount;

    /// <summary>
    /// Gets the total number of sound instances in the pool.
    /// </summary>
    public static int TotalSoundCount => Pool.TotalInstances;

    /// <summary>
    /// Gets a value indicating whether the sound pool is exhausted.
    /// </summary>
    public static bool IsPoolExhausted => Pool.IsExhausted;

    private static readonly Dictionary<string, Sound[]> SoundGroups = new();

    /// <summary>
    /// Registers a sound group for random selection.
    /// </summary>
    /// <param name="groupName">The name of the group.</param>
    /// <param name="sounds">The sounds in the group.</param>
    public static void RegisterSoundGroup(string groupName, params Sound[] sounds)
    {
        SoundGroups[groupName] = sounds;
    }

    /// <summary>
    /// Plays a random sound from an array of sounds.
    /// </summary>
    /// <param name="sounds">The array of sounds to choose from.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>The sound instance, or null if the array is empty.</returns>
    public static SoundInstance PlayRandom(Sound[] sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null!;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    /// <summary>
    /// Plays a random sound from a registered group.
    /// </summary>
    /// <typeparam name="T">The category enum type.</typeparam>
    /// <param name="groupName">The name of the sound group.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <param name="withVariation">If true, applies random pitch variation.</param>
    /// <param name="pitchRange">The pitch variation range when <paramref name="withVariation"/> is true.</param>
    /// <returns>The sound instance, or null if the group was not found.</returns>
    public static SoundInstance PlayFromGroup<T>(string groupName, float volume = 1f, float pan = 0f, float pitch = 1f, T category = default, bool withVariation = false,
    float pitchRange = 0.1f) where T : struct, Enum
    {
        if (!SoundGroups.TryGetValue(groupName, out var sounds))
            return null!;

        var sound = sounds[FastRandom.Shared.Next(sounds.Length)];

        if (withVariation)
        {
            float randomPitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
            return sound.PlayOneShot(volume, pan, randomPitch, category);
        }

        return sound.PlayOneShot(volume, pan, pitch, category);
    }
}