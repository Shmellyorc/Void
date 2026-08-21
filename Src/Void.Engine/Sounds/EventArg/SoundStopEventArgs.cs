// ============================================================================
//  SoundStoppedEventArgs.cs
// ============================================================================
//  Event arguments for sound stopped events, providing playback state information.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides event data for sound stopped events.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when a sound stops playing, either by natural completion,
/// manual stop, or pausing. It provides information about the state of the
/// sound before it stopped.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// SoundInstance.SoundStopped += (sender, args) =>
/// {
///     if (args.WasPlaying)
///         Console.WriteLine("Sound stopped while playing");
///     else if (args.WasPaused)
///         Console.WriteLine("Sound was paused before stopping");
/// };
/// </code>
/// </para>
/// </remarks>
public class SoundStoppedEventArgs : SoundEventArgs
{
    /// <summary>
    /// Gets a value indicating whether the sound was playing before it stopped.
    /// </summary>
    public bool WasPlaying { get; }

    /// <summary>
    /// Gets a value indicating whether the sound was paused before it stopped.
    /// </summary>
    public bool WasPaused { get; }

    internal SoundStoppedEventArgs(SoundInstance instance, bool wasPlaying, bool wasPaused) : base(instance)
    {
        WasPlaying = wasPlaying;
        WasPaused = wasPaused;
    }
}