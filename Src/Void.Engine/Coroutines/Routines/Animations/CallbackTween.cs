// ============================================================================
//  CallbackTween.cs
// ============================================================================
//  A tween that invokes a completion callback when the animation finishes.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A tween that invokes a completion callback when the animation finishes.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="CallbackTween{T}"/> class wraps a <see cref="Tween{T}"/>
/// and adds a callback that is invoked when the tween completes. This allows
/// for chaining actions or triggering events after an animation finishes.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a callback tween
/// var tween = new CallbackTween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     onComplete: () => Console.WriteLine("Animation complete!")
/// );
/// 
/// // Run the tween as a coroutine
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class CallbackTween<T> : IEnumerator
{
    private readonly Tween<T> _inner;
    private readonly Action _onComplete;

    /// <summary>
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackTween{T}"/> class.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The duration of the tween in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
    /// <param name="onComplete">The action to invoke when the tween completes.</param>
    public CallbackTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, Action onComplete)
    {
        _inner = new Tween<T>(from, to, duration, type, lerpFunc, onUpdate);
        _onComplete = onComplete;
    }

    /// <summary>
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        bool running = _inner.MoveNext();
        if (!running)
            _onComplete?.Invoke();
        return running;
    }

    /// <summary>
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}