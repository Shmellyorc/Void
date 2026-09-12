// ============================================================================
//  DelayedTween.cs
// ============================================================================
//  Tween that waits before interpolation begins.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Interpolates a value after an initial delay.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// While the delay is active, <see cref="MoveNext"/> advances elapsed time
/// without invoking the update callback. Interpolation then proceeds using
/// <see cref="Game.FrameTime"/> and the selected easing function.
/// </remarks>
public sealed class DelayedTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly float _delay;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;

    /// <summary>
    /// Gets the value yielded by the enumerator. Delayed tweens do not yield a
    /// value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a delayed tween.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The interpolation duration in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    /// <param name="delay">The delay in seconds before interpolation begins.</param>
    public DelayedTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, float delay)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _delay = delay;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
        _elapsed = -delay;
    }

    /// <summary>
    /// Advances the delay or tween using the current frame delta time.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the delay or tween is active;
    /// otherwise, <see langword="false"/> after applying the ending value.
    /// </returns>
    public bool MoveNext()
    {
        float deltaTime = Game.Instance.FrameTime.DeltaTime;

        if (_elapsed < 0f)
        {
            _elapsed += deltaTime;
            return true;
        }

        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += deltaTime;
            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    /// <summary>
    /// Resetting a delayed tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
