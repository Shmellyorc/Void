// ============================================================================
//  EveryFrames.cs
// ============================================================================
//  A coroutine that executes an action at a repeating frame interval.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Executes an action every specified number of coroutine updates until stopped.
/// </summary>
/// <remarks>
/// This routine is frame based rather than time based. An interval less than one
/// is clamped to one, causing the action to run on every update.
/// <code>
/// CoroutineManager.Instance.Run(new EveryFrames(30, UpdateUI));
/// </code>
/// </remarks>
public sealed class EveryFrames : IEnumerator
{
    private readonly int _interval;
    private readonly Action _action;
    private int _elapsed;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a repeating frame action.
    /// </summary>
    /// <param name="interval">The number of updates between action invocations. Values below one are treated as one.</param>
    /// <param name="action">The action to invoke at each interval.</param>
    public EveryFrames(int interval, Action action)
    {
        _interval = Math.Max(1, interval);
        _action = action;
        _elapsed = 0;
    }

    /// <summary>
    /// Advances the frame counter and invokes the action when the interval is reached.
    /// </summary>
    /// <returns>Always <see langword="true"/> because this routine repeats until explicitly stopped.</returns>
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
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases the routine. This implementation has no resources to release.
    /// </summary>
    public void Dispose() { }
}
