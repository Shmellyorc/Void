// ============================================================================
//  Delay.cs
// ============================================================================
//  A coroutine that waits for a specified number of seconds.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for a specified number of seconds.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Delay"/> class provides a simple way to pause coroutine
/// execution for a specified duration. It is a thin wrapper around
/// <see cref="WaitForSeconds"/>.
/// </para>
/// <para>
/// This is useful for creating delays between actions, such as waiting before
/// spawning an enemy, showing a message, or transitioning between states.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for 2 seconds
/// yield return new Delay(2f);
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new Delay(0.5f),
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
public sealed class Delay : IEnumerator
{
    private readonly WaitForSeconds _wait;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Delay"/> class.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait.</param>
    public Delay(float seconds)
    {
        _wait = new WaitForSeconds(seconds);
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => _wait.MoveNext();

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}