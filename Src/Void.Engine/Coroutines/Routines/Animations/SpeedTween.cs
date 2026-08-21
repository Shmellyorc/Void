// ============================================================================
//  SpeedTween.cs
// ============================================================================
//  A tween with a speed multiplier for fast-forward or slow-motion effects.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A tween with a speed multiplier for fast-forward or slow-motion effects.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="SpeedTween{T}"/> class extends the standard tween by
/// adding a speed multiplier that affects the animation speed. This allows
/// for fast-forward, slow-motion, or time-scaling effects on individual tweens.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a speed tween (normal speed)
/// var tween = new SpeedTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     speed: 1f
/// );
/// 
/// // Create a speed tween (double speed)
/// var tween2 = new SpeedTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     speed: 2f
/// );
/// 
/// // Create a speed tween (half speed)
/// var tween3 = new SpeedTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     speed: 0.5f
/// );
/// 
/// // Run the tween
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
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
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeedTween{T}"/> class.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The base duration of the tween in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
    /// <param name="speed">The speed multiplier (1 = normal, 2 = double speed, 0.5 = half speed).</param>
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
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}