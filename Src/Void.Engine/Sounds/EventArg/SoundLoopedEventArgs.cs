// ============================================================================
//  SoundLoopedEventArgs.cs
// ============================================================================
//  Event arguments for sound loop events, providing loop count information.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides event data for sound loop events.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised each time a looping sound completes a loop iteration.
/// It provides the current loop count, allowing tracking of how many times
/// the sound has looped.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// SoundInstance.SoundLooped += (sender, args) =>
/// {
///     if (args.LoopCount % 5 == 0)
///         Console.WriteLine($"Sound looped {args.LoopCount} times");
/// };
/// </code>
/// </para>
/// </remarks>
public class SoundLoopedEventArgs : SoundEventArgs
{
    /// <summary>
    /// Gets the number of times the sound has looped.
    /// </summary>
    public int LoopCount { get; }

    internal SoundLoopedEventArgs(SoundInstance instance, int loopCount) : base(instance)
    {
        LoopCount = loopCount;
    }
}