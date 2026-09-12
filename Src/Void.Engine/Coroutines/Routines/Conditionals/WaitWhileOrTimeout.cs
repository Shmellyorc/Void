// ============================================================================
//  WaitWhileOrTimeout.cs
// ============================================================================
//  A coroutine that waits while a condition remains true or a timeout occurs.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// Waits while a condition remains <see langword="true"/> or until a timeout elapses.
/// </summary>
/// <remarks>
/// The condition is checked before elapsed time is advanced on each call to
/// <see cref="MoveNext"/>. The coroutine completes when the condition becomes
/// false or when the accumulated frame time reaches the configured timeout.
/// </remarks>
public sealed class WaitWhileOrTimeout : IEnumerator
{
    private readonly Func<bool> _condition;
    private readonly float _timeout;
    private float _elapsed;

    /// <summary>
    /// Gets the value yielded by the coroutine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a conditional wait with a timeout.
    /// </summary>
    /// <param name="condition">
    /// The condition to evaluate. The wait continues while it returns
    /// <see langword="true"/>.
    /// </param>
    /// <param name="timeoutSeconds">The maximum accumulated wait time, in seconds.</param>
    public WaitWhileOrTimeout(Func<bool> condition, float timeoutSeconds)
    {
        _condition = condition;
        _timeout = timeoutSeconds;
        _elapsed = 0f;
    }

    /// <summary>
    /// Evaluates the condition and advances the timeout using the current frame time.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the condition is true and the timeout has
    /// not elapsed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        if (!_condition())
            return false;

        _elapsed += Game.Instance.FrameTime.DeltaTime;
        return _elapsed < _timeout;
    }

    /// <summary>
    /// Resetting this coroutine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases the coroutine. This implementation performs no work.
    /// </summary>
    public void Dispose() { }
}
