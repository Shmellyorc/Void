// ============================================================================
//  Tween.cs
// ============================================================================
//  A generic coroutine-based tween for animating values over time with easing.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A generic coroutine-based tween for animating values over time with easing.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="Tween{T}"/> class provides a flexible and performant way to
/// animate values over time using coroutines. It supports all easing types
/// from the <see cref="EaseType"/> enumeration and can be used with any type
/// that has a corresponding interpolation function.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Common Use Cases:</b>
/// <list type="bullet">
///   <item><description>Position, rotation, and scale animations</description></item>
///   <item><description>Color transitions</description></item>
///   <item><description>UI element animations</description></item>
///   <item><description>Camera movement</description></item>
///   <item><description>Any numeric or vector-based interpolation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a float tween
/// var tween = new Tween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value
/// );
/// 
/// // Create a Vect2 tween
/// var tween2 = new Tween&lt;Vect2&gt;(
///     from: Vect2.Zero,
///     to: new Vect2(100f, 50f),
///     duration: 0.5f,
///     type: EaseType.SineInOut,
///     lerpFunc: (a, b, t) => Vect2.Lerp(a, b, t),
///     onUpdate: value => entity.Position = value
/// );
/// 
/// // Create a Color tween
/// var tween3 = new Tween&lt;Color&gt;(
///     from: Color.Red,
///     to: Color.Blue,
///     duration: 2f,
///     type: EaseType.QuadInOut,
///     lerpFunc: (a, b, t) => Color.Lerp(a, b, t),
///     onUpdate: value => sprite.Color = value
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
public sealed class Tween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;

    /// <summary>
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tween{T}"/> class.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The duration of the tween in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
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
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}