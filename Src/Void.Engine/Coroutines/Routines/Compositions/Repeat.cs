// ============================================================================
//  Repeat.cs
// ============================================================================
//  Coroutine composition that recreates and repeats another routine.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// Repeats a coroutine created by a factory function.
/// </summary>
/// <remarks>
/// <para>
/// The factory is invoked once when the composition is created and again after
/// each completed iteration. It should return a fresh coroutine when an iteration
/// needs to restart from its initial state.
/// </para>
/// <para>
/// A positive <c>count</c> runs exactly that many iterations. A count of zero or
/// less repeats indefinitely.
/// </para>
/// </remarks>
public sealed class Repeat : IEnumerator
{
    private readonly Func<IEnumerator> _factory;
    private IEnumerator _current;
    private int _count;

    /// <summary>
    /// Gets the value yielded by the currently active coroutine.
    /// </summary>
    public object Current => _current?.Current!;

    /// <summary>
    /// Initializes a repeating coroutine composition.
    /// </summary>
    /// <param name="factory">
    /// A function that creates the coroutine used for each iteration.
    /// </param>
    /// <param name="count">
    /// The number of iterations to run. Values less than or equal to zero repeat indefinitely.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    public Repeat(Func<IEnumerator> factory, int count = -1)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _count = count;
        _current = _factory();
    }

    /// <summary>
    /// Advances the current iteration and starts a new one when required.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the repeated composition remains active;
    /// otherwise, <see langword="false"/>.
    /// </returns>
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
    /// Resetting a repeated composition is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
