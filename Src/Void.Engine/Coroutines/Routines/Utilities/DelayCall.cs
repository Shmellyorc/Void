// ============================================================================
//  DelayCall.cs
// ============================================================================
//  A coroutine that executes a callback after a specified delay.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// A coroutine that executes a callback after a specified delay.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DelayCall"/> class waits for the specified duration and then
/// invokes the provided callback. It is a convenient way to schedule a single
/// action to occur after a delay.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Delayed actions and events</description></item>
///   <item><description>Timed callbacks</description></item>
///   <item><description>Simple single-use timers</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Call a method after 2 seconds
/// CoroutineManager.Instance.Run(new DelayCall(2f, () => Console.WriteLine("Delayed!")));
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new DelayCall(0.5f, () => Console.WriteLine("Half second after tween")),
///     new Tween&lt;float&gt;(100f, 200f, 1f, EaseType.QuadOut, Lerp, value => x = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public class DelayCall : IEnumerator
{
    private readonly float _delay;
    private readonly Action _callback;
    private float _elapsed;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayCall"/> class.
    /// </summary>
    /// <param name="delay">The delay in seconds before the callback is invoked.</param>
    /// <param name="callback">The action to invoke after the delay.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
    public DelayCall(float delay, Action callback)
    {
        _delay = delay;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (_elapsed < _delay)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        _callback();
        return false;
    }

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}