// ============================================================================
//  WaitWhile.cs
// ============================================================================
//  A coroutine that waits while a given condition remains true.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits while a given condition remains true.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitWhile"/> class pauses the coroutine execution while
/// the specified predicate returns <see langword="true"/>. The predicate is
/// checked each frame.
/// </para>
/// <para>
/// This is the inverse of <see cref="WaitUntil"/> and is useful for waiting
/// for a condition to become false, such as waiting for a state to change,
/// a timer to expire, or an animation to complete.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait while a flag is true
/// yield return new WaitWhile(() => isAnimating);
/// 
/// // Wait while health is above zero (i.e., while alive)
/// yield return new WaitWhile(() => health > 0);
/// 
/// // Wait while an object exists
/// yield return new WaitWhile(() => enemy != null);
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new WaitWhile(() => isPlaying),
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
public sealed class WaitWhile : IEnumerator
{
    private readonly Func<bool> _predicate;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitWhile"/> class.
    /// </summary>
    /// <param name="predicate">The condition to wait for. Returns <see langword="true"/> while waiting should continue.</param>
    public WaitWhile(Func<bool> predicate) => _predicate = predicate;

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => _predicate();

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}