// ============================================================================
//  SpeedTween.cs
// ============================================================================
//  Tween whose elapsed time is scaled by a speed multiplier.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Interpolates a value while scaling elapsed time by a speed multiplier.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// A speed of <c>1</c> advances by normal frame time, values above <c>1</c>
/// advance faster, and values between <c>0</c> and <c>1</c> advance more slowly.
/// The speed value is not validated; zero or negative values do not produce a
/// normal completing tween.
/// </remarks>
public sealed class SpeedTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly float _speed;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;

    /// <summary>
    /// Gets the value yielded by the enumerator. Speed tweens do not yield a
    /// value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a speed-scaled tween.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The base tween duration in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    /// <param name="speed">The multiplier applied to frame delta time.</param>
    public SpeedTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, float speed = 1f)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _speed = speed;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
    }

    /// <summary>
    /// Advances the tween by frame delta time multiplied by the configured speed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the tween should continue running;
    /// otherwise, <see langword="false"/> after applying the ending value.
    /// </returns>
    public bool MoveNext()
    {
        float deltaTime = Game.Instance.FrameTime.DeltaTime;

        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += deltaTime * _speed;
            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    /// <summary>
    /// Resetting a speed tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
