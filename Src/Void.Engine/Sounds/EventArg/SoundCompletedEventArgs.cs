// ============================================================================
//  SoundCompletedEventArgs.cs
// ============================================================================
//  Event data for natural sound playback completion.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides event data when a sound instance reaches natural playback completion.
/// </summary>
public class SoundCompletedEventArgs : SoundEventArgs
{
    /// <summary>Gets whether the completion was reported as occurring from looping playback.</summary>
    public bool WasLooping { get; }

    /// <summary>Gets the number of completed loop iterations recorded before completion.</summary>
    public int LoopCount { get; }

    internal SoundCompletedEventArgs(SoundInstance instance, bool wasLooping, int loopCount) : base(instance)
    {
        WasLooping = wasLooping;
        LoopCount = loopCount;
    }
}
