// ============================================================================
//  WaitUntil.cs
// ============================================================================
//  A coroutine that waits until a given condition becomes true.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// Waits until a predicate returns <see langword="true"/>.
/// </summary>
/// <remarks>
/// The predicate is evaluated each time the coroutine advances. The coroutine
/// completes as soon as the predicate returns <see langword="true"/>.
/// <code>
/// yield return new WaitUntil(() => isReady);
/// </code>
/// </remarks>
public sealed class WaitUntil : IEnumerator
{
    private readonly Func<bool> _predicate;

    /// <summary>
    /// Gets the value yielded by the coroutine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a wait for the specified predicate to become <see langword="true"/>.
    /// </summary>
    /// <param name="predicate">
    /// The condition to evaluate. The wait ends when it returns <see langword="true"/>.
    /// </param>
    public WaitUntil(Func<bool> predicate) => _predicate = predicate;

    /// <summary>
    /// Evaluates the predicate and advances the wait.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the predicate is <see langword="false"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool MoveNext() => !_predicate();

    /// <summary>
    /// Resetting this coroutine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases the coroutine. This implementation performs no work.
    /// </summary>
    public void Dispose() { }
}
