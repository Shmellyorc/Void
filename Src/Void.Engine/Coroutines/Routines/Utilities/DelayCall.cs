// ============================================================================
//  DelayCall.cs
// ============================================================================
//  Coroutine utility that invokes a callback after a scaled-time delay.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// Waits for a delay, then invokes a callback once.
/// </summary>
/// <remarks>
/// The delay advances with <see cref="FrameTime.DeltaTime"/>, so it is affected
/// by the game's time scale. A delay less than or equal to zero causes the
/// callback to run on the first call to <see cref="MoveNext"/>.
/// </remarks>
public class DelayCall : IEnumerator
{
    private readonly float _delay;
    private readonly Action _callback;
    private float _elapsed;

    /// <summary>
    /// Gets the value yielded by this routine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a delayed callback.
    /// </summary>
    /// <param name="delay">The scaled-time delay in seconds.</param>
    /// <param name="callback">The callback to invoke after the delay.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="callback"/> is <see langword="null"/>.
    /// </exception>
    public DelayCall(float delay, Action callback)
    {
        _delay = delay;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the wait and invokes the callback once the delay has elapsed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while waiting; otherwise, <see langword="false"/>
    /// after the callback has been invoked.
    /// </returns>
    public bool MoveNext()
    {
        if (_elapsed < _delay)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        _callback();
        return false;
    }

    /// <summary>
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases this routine. This implementation performs no work.
    /// </summary>
    public void Dispose() { }
}
