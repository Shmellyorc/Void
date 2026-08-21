// ============================================================================
//  PingPongTween.cs
// ============================================================================
//  A tween that oscillates back and forth between two values.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A tween that oscillates back and forth between two values.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="PingPongTween{T}"/> class extends the standard tween by
/// automatically reversing direction when it reaches the end. This creates
/// a continuous oscillation between the start and end values.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a ping-pong tween
/// var tween = new PingPongTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value
/// );
/// 
/// // Run the tween (oscillates indefinitely)
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
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
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PingPongTween{T}"/> class.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The duration of each direction in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
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
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}