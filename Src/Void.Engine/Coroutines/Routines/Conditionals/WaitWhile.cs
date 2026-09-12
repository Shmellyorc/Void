// ============================================================================
//  WaitWhile.cs
// ============================================================================
//  A coroutine that waits while a given condition remains true.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// Waits while a predicate continues to return <see langword="true"/>.
/// </summary>
/// <remarks>
/// The predicate is evaluated each time the coroutine advances. The coroutine
/// completes as soon as the predicate returns <see langword="false"/>.
/// </remarks>
public sealed class WaitWhile : IEnumerator
{
    private readonly Func<bool> _predicate;

    /// <summary>
    /// Gets the value yielded by the coroutine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a wait that continues while the specified predicate is true.
    /// </summary>
    /// <param name="predicate">
    /// The condition to evaluate. The wait continues while it returns
    /// <see langword="true"/>.
    /// </param>
    public WaitWhile(Func<bool> predicate) => _predicate = predicate;

    /// <summary>
    /// Evaluates the predicate and advances the wait.
    /// </summary>
    /// <returns>The current result of the predicate.</returns>
    public bool MoveNext() => _predicate();

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
