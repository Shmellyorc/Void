// ============================================================================
//  SoundCompletedEventArgs.cs
// ============================================================================
//  Event arguments for sound playback completion events, including
//  looping information.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Sounds;

/// <summary>
/// Provides event data for sound playback completion events.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when a sound instance finishes playing, either naturally
/// or when looping ends. It provides information about the looping behavior
/// that occurred during playback.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// soundInstance.SoundCompleted += (sender, args) =>
/// {
///     if (args.WasLooping)
///         Console.WriteLine($"Sound looped {args.LoopCount} times");
///     else
///         Console.WriteLine("Sound played once");
/// };
/// </code>
/// </para>
/// </remarks>
public class SoundCompletedEventArgs : SoundEventArgs
{
    /// <summary>
    /// Gets a value indicating whether the sound was looping when it completed.
    /// </summary>
    public bool WasLooping { get; }

    /// <summary>
    /// Gets the number of times the sound looped before completing.
    /// </summary>
    public int LoopCount { get; }

    internal SoundCompletedEventArgs(SoundInstance instance, bool wasLooping, int loopCount) : base(instance)
    {
        WasLooping = wasLooping;
        LoopCount = loopCount;
    }
}