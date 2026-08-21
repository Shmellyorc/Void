// ============================================================================
//  EveryFrames.cs
// ============================================================================
//  A coroutine that executes an action every N frames indefinitely.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that executes an action every N frames indefinitely.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EveryFrames"/> class calls a specified action at a regular
/// frame interval. It runs indefinitely until the coroutine is stopped.
/// </para>
/// <para>
/// This is useful for frame-rate independent periodic tasks such as
/// updating UI, checking conditions, or performing maintenance tasks.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Call an action every 60 frames (approximately once per second at 60 FPS)
/// var everyFrame = new EveryFrames(60, () => Console.WriteLine("Tick!"));
/// CoroutineManager.Instance.Run(everyFrame);
/// 
/// // Call an action every 30 frames
/// CoroutineManager.Instance.Run(new EveryFrames(30, UpdateUI));
/// 
/// // Stop after a condition
/// var handle = CoroutineManager.Instance.Run(new EveryFrames(10, () => 
/// {
///     if (someCondition)
///         CoroutineManager.Instance.Stop(handle);
/// }));
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class EveryFrames : IEnumerator
{
    private readonly int _interval;
    private readonly Action _action;
    private int _elapsed;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EveryFrames"/> class.
    /// </summary>
    /// <param name="interval">The number of frames between each action execution.</param>
    /// <param name="action">The action to execute at the specified interval.</param>
    public EveryFrames(int interval, Action action)
    {
        _interval = Math.Max(1, interval);
        _action = action;
        _elapsed = 0;
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns>Always returns <see langword="true"/> (runs indefinitely).</returns>
    public bool MoveNext()
    {
        _elapsed++;
        if (_elapsed >= _interval)
        {
            _elapsed = 0;
            _action?.Invoke();
        }
        return true;
    }

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}