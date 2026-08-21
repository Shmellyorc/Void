// ============================================================================
//  EverySeconds.cs
// ============================================================================
//  A coroutine that executes an action at a regular time interval indefinitely.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that executes an action at a regular time interval indefinitely.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EverySeconds"/> class calls a specified action at a regular
/// time interval. It runs indefinitely until the coroutine is stopped.
/// </para>
/// <para>
/// Unlike <see cref="EveryFrames"/>, this class uses time-based intervals,
/// making it frame-rate independent and consistent regardless of frame rate.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Call an action every 1 second
/// var everySecond = new EverySeconds(1f, () => Console.WriteLine("Tick!"));
/// CoroutineManager.Instance.Run(everySecond);
/// 
/// // Call an action every 0.5 seconds
/// CoroutineManager.Instance.Run(new EverySeconds(0.5f, UpdateHealthBar));
/// 
/// // Call an action every frame (effectively)
/// CoroutineManager.Instance.Run(new EverySeconds(0f, () => UpdateUI()));
/// 
/// // Stop after a condition
/// var handle = CoroutineManager.Instance.Run(new EverySeconds(2f, () => 
/// {
///     if (gameOver)
///         CoroutineManager.Instance.Stop(handle);
/// }));
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public class EverySeconds : IEnumerator
{
    private readonly float _interval;
    private readonly Action _action;
    private float _elapsed;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EverySeconds"/> class.
    /// </summary>
    /// <param name="interval">The time in seconds between each action execution.</param>
    /// <param name="action">The action to execute at the specified interval.</param>
    public EverySeconds(float interval, Action action)
    {
        _interval = Math.Max(0f, interval);
        _action = action;
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns>Always returns <see langword="true"/> (runs indefinitely).</returns>
    public bool MoveNext()
    {
        if (_interval <= 0f)
        {
            _action?.Invoke();
            return true;
        }

        _elapsed += Game.Instance.FrameTime.DeltaTime;
        if (_elapsed >= _interval)
        {
            _elapsed -= _interval;
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