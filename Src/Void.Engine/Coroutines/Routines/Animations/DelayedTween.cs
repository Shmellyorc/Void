// ============================================================================
//  DelayedTween.cs
// ============================================================================
//  A tween with an initial delay before the animation begins.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A tween with an initial delay before the animation begins.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="DelayedTween{T}"/> class extends the standard tween by
/// adding a delay before the animation starts. This is useful for sequencing
/// animations or creating staggered effects.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a delayed tween
/// var tween = new DelayedTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     delay: 0.5f
/// );
/// 
/// // Run the tween (will wait 0.5s before starting)
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
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
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayedTween{T}"/> class.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The duration of the tween in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
    /// <param name="delay">The initial delay in seconds before the tween starts.</param>
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
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}