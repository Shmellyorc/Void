// ============================================================================
//  PingPongTween.cs
// ============================================================================
//  Tween that travels forward and then back to its starting value.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Interpolates from one value to another, then reverses back to the start.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// A <see cref="PingPongTween{T}"/> performs one forward traversal followed by
/// one reverse traversal. After the reverse traversal completes, the starting
/// value is applied and the enumerator finishes.
/// </remarks>
public sealed class PingPongTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;
    private bool _reverse;

    /// <summary>
    /// Gets the value yielded by the enumerator. Ping-pong tweens do not yield
    /// a value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a forward-and-reverse tween.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The value reached before reversing.</param>
    /// <param name="duration">The duration of each direction in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    public PingPongTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
    }

    /// <summary>
    /// Advances the current direction using the current frame delta time.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while either direction is active;
    /// otherwise, <see langword="false"/> after the starting value is restored.
    /// </returns>
    public bool MoveNext()
    {
        float deltaTime = Game.Instance.FrameTime.DeltaTime;

        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            if (_reverse)
                normalized = 1f - normalized;

            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += deltaTime;
            return true;
        }

        if (!_reverse)
        {
            _reverse = true;
            _elapsed = 0f;
            return true;
        }

        _onUpdate?.Invoke(_from);
        return false;
    }

    /// <summary>
    /// Resetting a ping-pong tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
