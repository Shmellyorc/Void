// ============================================================================
//  Repeat.cs
// ============================================================================
//  A coroutine that repeats another coroutine a specified number of times.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// A coroutine that repeats another coroutine a specified number of times.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Repeat"/> class takes a factory function that creates a
/// coroutine and executes it repeatedly. Each time the coroutine completes,
/// it is recreated and started again.
/// </para>
/// <para>
/// This is useful for repeating animations, effects, or operations that need
/// to run multiple times, such as a walking animation loop or a repeating
/// sound effect.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a tween factory
/// Func&lt;IEnumerator&gt; tweenFactory = () => new Tween&lt;float&gt;(
///     from: 0f,
///     to: 100f,
///     duration: 0.5f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value
/// );
/// 
/// // Repeat the tween 5 times
/// var repeat = new Repeat(tweenFactory, 5);
/// CoroutineManager.Instance.Run(repeat);
/// 
/// // Repeat indefinitely
/// var infinite = new Repeat(tweenFactory);
/// CoroutineManager.Instance.Run(infinite);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class Repeat : IEnumerator
{
    private readonly Func<IEnumerator> _factory;
    private IEnumerator _current;
    private int _count;

    /// <summary>
    /// Gets the current value from the currently running coroutine.
    /// </summary>
    public object Current => _current?.Current!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repeat"/> class.
    /// </summary>
    /// <param name="factory">A function that creates the coroutine to repeat.</param>
    /// <param name="count">The number of times to repeat, or -1 for infinite repetition.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
    public Repeat(Func<IEnumerator> factory, int count = -1)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _count = count;
        _current = _factory();
    }

    /// <summary>
    /// Advances the repeat coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the repeat is still running; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (_current == null)
            return false;

        if (_current.MoveNext())
            return true;

        if (_count > 0)
        {
            _count--;
            if (_count == 0)
                return false;
        }

        _current = _factory();
        return _current.MoveNext();
    }

    /// <summary>
    /// Resets the repeat coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}