// ============================================================================
//  SoundStoppedEventArgs.cs
// ============================================================================
//  Event data raised when a sound instance is explicitly stopped.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides playback-state information captured when <see cref="SoundInstance.Stop"/> is called.
/// </summary>
public class SoundStoppedEventArgs : SoundEventArgs
{
    /// <summary>Gets whether the instance was playing immediately before it was stopped.</summary>
    public bool WasPlaying { get; }

    /// <summary>Gets whether the instance was paused immediately before it was stopped.</summary>
    public bool WasPaused { get; }

    internal SoundStoppedEventArgs(SoundInstance instance, bool wasPlaying, bool wasPaused) : base(instance)
    {
        WasPlaying = wasPlaying;
        WasPaused = wasPaused;
    }
}
