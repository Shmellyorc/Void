// ============================================================================
//  EverySeconds.cs
// ============================================================================
//  A coroutine that executes an action at a repeating scaled-time interval.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Executes an action at a repeating scaled-time interval until stopped.
/// </summary>
/// <remarks>
/// <para>
/// Elapsed time is accumulated from <see cref="FrameTime.DeltaTime"/>, so the
/// interval is affected by <see cref="FrameTime.TimeScale"/>. An interval of zero
/// or less invokes the action on every coroutine update.
/// </para>
/// <para>
/// When an update passes the interval, one interval is subtracted from the
/// accumulated time. At most one action invocation occurs per update.
/// </para>
/// <code>
/// CoroutineManager.Instance.Run(new EverySeconds(0.5f, UpdateStatus));
/// </code>
/// </remarks>
public class EverySeconds : IEnumerator
{
    private readonly float _interval;
    private readonly Action _action;
    private float _elapsed;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a repeating timed action.
    /// </summary>
    /// <param name="interval">The scaled-time interval in seconds. Negative values are treated as zero.</param>
    /// <param name="action">The action to invoke at each interval.</param>
    public EverySeconds(float interval, Action action)
    {
        _interval = Math.Max(0f, interval);
        _action = action;
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the timer and invokes the action when the interval is reached.
    /// </summary>
    /// <returns>Always <see langword="true"/> because this routine repeats until explicitly stopped.</returns>
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
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases the routine. This implementation has no resources to release.
    /// </summary>
    public void Dispose() { }
}
