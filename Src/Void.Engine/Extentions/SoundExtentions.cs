// ============================================================================
//  SoundExtensions.cs
// ============================================================================
//  Extension methods for Sound and SoundInstance providing convenient
//  playback options, fluent configuration, and collection-based playback.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides extension methods for <see cref="Sound"/> and <see cref="SoundInstance"/>
/// providing convenient playback options, fluent configuration, and collection-based playback.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SoundExtensions"/> class provides a comprehensive set of
/// extension methods for sound playback, making common sound operations
/// more intuitive and expressive.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>One-shot playback with optional parameters</description></item>
///   <item><description>Play-and-forget with automatic cleanup</description></item>
///   <item><description>Pitch variation for natural variation</description></item>
///   <item><description>Fluent interface for sound instance configuration</description></item>
///   <item><description>Collection-based playback (play all, random selection)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a sound
/// var sound = AssetManager.Instance.Load&lt;Sound&gt;("explosion.wav");
/// 
/// // Play one-shot
/// sound.PlayOneShot(0.8f, 0f, 1f, SoundCategory.SFX);
/// 
/// // Play and forget (auto-disposes when done)
/// sound.PlayAndForget(0.9f);
/// 
/// // Play with pitch variation
/// sound.PlayWithPitchVariation(0.15f, 0.8f, 0f, SoundCategory.SFX);
/// 
/// // Fluent interface
/// sound.CreateInstance()
///     .WithVolume(0.8f)
///     .WithPan(-0.5f)
///     .WithPitch(1.2f)
///     .WithLooping(true)
///     .PlayWith(0.8f, -0.5f, 1.2f);
/// 
/// // Stop and dispose
/// instance.StopAndDispose();
/// 
/// // Play all sounds in a collection
/// var sounds = new[] { sound1, sound2, sound3 };
/// sounds.PlayAll(0.7f);
/// sounds.PlayAllAndForget(0.7f);
/// 
/// // Play random sound from collection
/// var randomInstance = sounds.PlayRandom(0.8f);
/// var randomVaried = sounds.PlayRandomWithVariation(0.15f, 0.8f);
/// 
/// // Random with auto-dispose
/// sounds.PlayRandomAndForget(0.8f);
/// sounds.PlayRandomWithVariationAndForget(0.15f, 0.8f);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// These extension methods are not thread-safe and should be called from
/// the main thread.
/// </para>
/// </remarks>
public static class SoundExtensions
{
    /// <summary>
    /// Plays the sound as a one-shot instance.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <returns>The sound instance that was created.</returns>
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
    /// Plays the sound and automatically disposes it when playback completes.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <returns>The sound instance that was created.</returns>
    public static SoundInstance PlayAndForget(this Sound sound, float volume = 1f, float pan = 0f, float pitch = 1f, Enum category = null!)
    {
        var instance = sound.PlayOneShot(volume, pan, pitch, category);

        instance.SoundCompleted += (_, _) => instance.Dispose();
        instance.SoundStopped += (_, _) => instance.Dispose();

        return instance;
    }

    /// <summary>
    /// Plays the sound with random pitch variation.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="pitchRange">The pitch variation range (± from 1).</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="category">The sound category for volume grouping.</param>
    /// <returns>The sound instance that was created.</returns>
    public static SoundInstance PlayWithPitchVariation(this Sound sound, float pitchRange = 0.1f, float volume = 1f, float pan = 0f, Enum category = null!)
    {
        float pitch = FastRandom.Shared.RangeFloat(1f - pitchRange, 1f + pitchRange);
        return sound.PlayOneShot(volume, pan, pitch, category);
    }

    /// <summary>
    /// Sets the volume of the sound instance.
    /// </summary>
    /// <param name="instance">The sound instance.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <returns>The sound instance for method chaining.</returns>
    public static SoundInstance WithVolume(this SoundInstance instance, float volume)
    {
        instance.Volume = volume;
        return instance;
    }

    /// <summary>
    /// Sets the pan of the sound instance.
    /// </summary>
    /// <param name="instance">The sound instance.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <returns>The sound instance for method chaining.</returns>
    public static SoundInstance WithPan(this SoundInstance instance, float pan)
    {
        instance.Pan = pan;
        return instance;
    }

    /// <summary>
    /// Sets the pitch of the sound instance.
    /// </summary>
    /// <param name="instance">The sound instance.</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>The sound instance for method chaining.</returns>
    public static SoundInstance WithPitch(this SoundInstance instance, float pitch)
    {
        instance.Pitch = pitch;
        return instance;
    }

    /// <summary>
    /// Sets whether the sound instance should loop.
    /// </summary>
    /// <param name="instance">The sound instance.</param>
    /// <param name="looping">Whether the sound should loop.</param>
    /// <returns>The sound instance for method chaining.</returns>
    public static SoundInstance WithLooping(this SoundInstance instance, bool looping)
    {
        instance.Looping = looping;
        return instance;
    }

    /// <summary>
    /// Sets the volume, pan, and pitch, then plays the sound.
    /// </summary>
    /// <param name="instance">The sound instance.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>The sound instance for method chaining.</returns>
    public static SoundInstance PlayWith(this SoundInstance instance, float volume, float pan = 0f, float pitch = 1f)
    {
        instance.Volume = volume;
        instance.Pan = pan;
        instance.Pitch = pitch;
        instance.Play();
        return instance;
    }

    /// <summary>
    /// Stops the sound instance and disposes it.
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
    /// Plays all sounds in the collection as one-shot instances.
    /// </summary>
    /// <param name="sounds">The sounds to play.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>A list of all sound instances that were created.</returns>
    public static List<SoundInstance> PlayAll(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        return sounds.Select(s => s.PlayOneShot(volume, pan, pitch)).ToList();
    }

    /// <summary>
    /// Plays all sounds in the collection and automatically disposes them when playback completes.
    /// </summary>
    /// <param name="sounds">The sounds to play.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    public static void PlayAllAndForget(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        foreach (var sound in sounds)
            sound.PlayAndForget(volume, pan, pitch);
    }

    /// <summary>
    /// Plays a random sound from the collection.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>The sound instance that was created, or null if the collection is empty.</returns>
    public static SoundInstance PlayRandom(this IEnumerable<Sound> sounds, float volume = 1f, float pan = 0f, float pitch = 1f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null!;

        return list[FastRandom.Shared.Next(list.Count)].PlayOneShot(volume, pan, pitch);
    }

    /// <summary>
    /// Plays a random sound from the collection with random pitch variation.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="pitchRange">The pitch variation range (± from 1).</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <returns>The sound instance that was created, or null if the collection is empty.</returns>
    public static SoundInstance PlayRandomWithVariation(this IEnumerable<Sound> sounds, float pitchRange = 0.1f, float volume = 1f, float pan = 0f)
    {
        var list = sounds as IList<Sound> ?? sounds.ToList();
        if (list.Count == 0)
            return null!;

        return list[FastRandom.Shared.Next(list.Count)].PlayWithPitchVariation(pitchRange, volume, pan);
    }

    /// <summary>
    /// Plays a random sound from the collection and automatically disposes it when playback completes.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <param name="pitch">The pitch multiplier.</param>
    /// <returns>The sound instance that was created, or null if the collection is empty.</returns>
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
    /// Plays a random sound from the collection with random pitch variation and automatically disposes it when playback completes.
    /// </summary>
    /// <param name="sounds">The sounds to choose from.</param>
    /// <param name="pitchRange">The pitch variation range (± from 1).</param>
    /// <param name="volume">The volume between 0 and 1.</param>
    /// <param name="pan">The pan between -1 (left) and 1 (right).</param>
    /// <returns>The sound instance that was created, or null if the collection is empty.</returns>
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