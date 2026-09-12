// ============================================================================
//  WaitWhileNotNull.cs
// ============================================================================
//  A coroutine that waits while a value remains non-null.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// Waits while a reference returned by a getter remains non-null.
/// </summary>
/// <typeparam name="T">The reference type being observed.</typeparam>
/// <remarks>
/// The getter is evaluated each time the coroutine advances. The coroutine
/// completes when the getter returns <see langword="null"/>.
/// </remarks>
public sealed class WaitWhileNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    /// <summary>
    /// Gets the value yielded by the coroutine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Gets the value returned by the most recent getter evaluation.
    /// </summary>
    /// <remarks>
    /// When the wait completes normally, this property is <see langword="null"/>.
    /// </remarks>
    public T Value => _value;

    /// <summary>
    /// Initializes a wait that continues while the specified getter returns a value.
    /// </summary>
    /// <param name="getter">The function that provides the value to observe.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="getter"/> is <see langword="null"/>.
    /// </exception>
    public WaitWhileNotNull(Func<T> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    /// <summary>
    /// Reads the current value and advances the wait.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the observed value is non-null; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        _value = _getter();
        return _value != null;
    }

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
