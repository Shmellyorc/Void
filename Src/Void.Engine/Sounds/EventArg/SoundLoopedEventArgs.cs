// ============================================================================
//  SoundLoopedEventArgs.cs
// ============================================================================
//  Event data raised after a looping sound completes an iteration.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides event data for a completed loop iteration.
/// </summary>
public class SoundLoopedEventArgs : SoundEventArgs
{
    /// <summary>Gets the total number of loop iterations completed by the instance.</summary>
    public int LoopCount { get; }

    internal SoundLoopedEventArgs(SoundInstance instance, int loopCount) : base(instance)
    {
        LoopCount = loopCount;
    }
}
