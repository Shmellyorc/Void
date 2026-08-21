// ============================================================================
//  SoundEventArgs.cs
// ============================================================================
//  Base event argument class for all sound playback events.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds.EventArg;

/// <summary>
/// Base event argument class for sound playback events.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as the base for all sound event argument types and provides
/// common information about the sound instance, including its name, current
/// playback time, and total duration.
/// </para>
/// <para>
/// Derived event types include:
/// <list type="bullet">
///   <item><description><see cref="SoundCompletedEventArgs"/></description></item>
///   <item><description><see cref="SoundLoopedEventArgs"/></description></item>
///   <item><description><see cref="SoundStoppedEventArgs"/></description></item>
/// </list>
/// </para>
/// </remarks>
public class SoundEventArgs : EventArgs
{
    /// <summary>
    /// Gets the sound instance that triggered the event.
    /// </summary>
    public SoundInstance Instance { get; }

    /// <summary>
    /// Gets the name of the sound.
    /// </summary>
    public string SoundName { get; }

    /// <summary>
    /// Gets the current playback time of the sound in seconds.
    /// </summary>
    public float PlayTime { get; }

    /// <summary>
    /// Gets the total duration of the sound in seconds.
    /// </summary>
    public float Duration { get; }

    internal SoundEventArgs(SoundInstance instance)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknown";
        PlayTime = instance?.PlayTime ?? 0f;
        Duration = instance?.Duration ?? 0f;
    }
}