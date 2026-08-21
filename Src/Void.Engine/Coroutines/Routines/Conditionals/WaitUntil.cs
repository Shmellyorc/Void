// ============================================================================
//  WaitUntil.cs
// ============================================================================
//  A coroutine that waits until a given condition becomes true.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits until a given condition becomes true.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitUntil"/> class pauses the coroutine execution until
/// the specified predicate returns <see langword="true"/>. The predicate is
/// checked each frame.
/// </para>
/// <para>
/// This is useful for waiting for asynchronous events, such as a variable
/// being set, a flag being raised, or a condition being met in the game state.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for a flag to become true
/// yield return new WaitUntil(() => isReady);
/// 
/// // Wait for a value to reach a threshold
/// yield return new WaitUntil(() => health > 50);
/// 
/// // Wait for an object to be initialized
/// yield return new WaitUntil(() => player != null &amp;&amp; player.IsInitialized);
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new WaitUntil(() => hasLoaded),
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitUntil : IEnumerator
{
    private readonly Func<bool> _predicate;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitUntil"/> class.
    /// </summary>
    /// <param name="predicate">The condition to wait for. Returns <see langword="true"/> when the wait should end.</param>
    public WaitUntil(Func<bool> predicate) => _predicate = predicate;

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => !_predicate();

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}