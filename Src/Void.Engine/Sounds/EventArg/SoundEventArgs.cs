// ============================================================================
//  SoundEventArgs.cs
// ============================================================================
//  Base event data shared by sound playback events.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds.EventArg;

/// <summary>
/// Provides a snapshot of common sound-instance data captured when an event is created.
/// </summary>
public class SoundEventArgs : EventArgs
{
    /// <summary>Gets the sound instance associated with the event.</summary>
    public SoundInstance Instance { get; }

    /// <summary>Gets the sound name captured for the event.</summary>
    public string SoundName { get; }

    /// <summary>Gets the playback time, in seconds, captured for the event.</summary>
    public float PlayTime { get; }

    /// <summary>Gets the sound duration, in seconds, captured for the event.</summary>
    public float Duration { get; }

    internal SoundEventArgs(SoundInstance instance)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknown";
        PlayTime = instance?.PlayTime ?? 0f;
        Duration = instance?.Duration ?? 0f;
    }
}
