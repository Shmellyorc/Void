// ============================================================================
//  CallbackTween.cs
// ============================================================================
//  Tween wrapper that invokes a callback when the tween completes.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Runs a <see cref="Tween{T}"/> and invokes a callback when it completes.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// The completion callback is invoked when the wrapped tween first reports that
/// it has finished during normal coroutine execution.
/// </remarks>
public sealed class CallbackTween<T> : IEnumerator
{
    private readonly Tween<T> _inner;
    private readonly Action _onComplete;

    /// <summary>
    /// Gets the value yielded by the enumerator. Callback tweens do not yield a
    /// value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a tween with a completion callback.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The tween duration in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    /// <param name="onComplete">The callback invoked when the wrapped tween completes.</param>
    public CallbackTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, Action onComplete)
    {
        _inner = new Tween<T>(from, to, duration, type, lerpFunc, onUpdate);
        _onComplete = onComplete;
    }

    /// <summary>
    /// Advances the wrapped tween and invokes the completion callback when it finishes.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the wrapped tween is running;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        bool running = _inner.MoveNext();
        if (!running)
            _onComplete?.Invoke();
        return running;
    }

    /// <summary>
    /// Resetting a callback tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
