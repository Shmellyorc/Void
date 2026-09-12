// ============================================================================
//  Tween.cs
// ============================================================================
//  Generic coroutine-based interpolation with easing.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Interpolates a value from one state to another over a fixed duration.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Tween{T}"/> implements <see cref="IEnumerator"/> so it can be run
/// directly by <see cref="CoroutineManager"/>. Each update reads
/// <see cref="Game.FrameTime"/>, applies the selected <see cref="EaseType"/>,
/// interpolates through the supplied function, and passes the result to the
/// update callback.
/// </para>
/// <code>
/// float opacity = 0f;
/// var tween = new Tween&lt;float&gt;(
///     0f,
///     1f,
///     0.25f,
///     EaseType.QuadOut,
///     static (a, b, t) =&gt; a + ((b - a) * t),
///     value =&gt; opacity = value);
///
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </remarks>
public sealed class Tween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;

    /// <summary>
    /// Gets the value yielded by the enumerator. Tweens do not yield a value,
    /// so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a tween between two values.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The tween duration in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    public Tween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _onUpdate = onUpdate;
        _lerp = lerpFunc;
        _type = type;
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the tween using the current frame delta time.
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
            _elapsed += deltaTime;

            float normalized = Math.Clamp(_elapsed / _duration, 0f, 1f);
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);

            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    /// <summary>
    /// Resetting a tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
