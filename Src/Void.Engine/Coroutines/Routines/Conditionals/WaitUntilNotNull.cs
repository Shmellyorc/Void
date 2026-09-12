// ============================================================================
//  WaitUntilNotNull.cs
// ============================================================================
//  A coroutine that waits until a value becomes non-null.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// Waits until a reference returned by a getter becomes non-null.
/// </summary>
/// <typeparam name="T">The reference type being observed.</typeparam>
/// <remarks>
/// The getter is evaluated each time the coroutine advances. When the getter
/// first returns a non-null value, that value is stored in <see cref="Value"/>
/// and the coroutine completes.
/// </remarks>
public sealed class WaitUntilNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    /// <summary>
    /// Gets the value yielded by the coroutine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Gets the most recently observed value.
    /// </summary>
    /// <remarks>
    /// This remains <see langword="null"/> until the getter returns a non-null
    /// value. When the wait completes normally, this property contains that value.
    /// </remarks>
    public T Value => _value;

    /// <summary>
    /// Initializes a wait for the specified getter to return a non-null value.
    /// </summary>
    /// <param name="getter">The function that provides the value to observe.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="getter"/> is <see langword="null"/>.
    /// </exception>
    public WaitUntilNotNull(Func<T> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    /// <summary>
    /// Reads the current value and advances the wait.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the observed value is <see langword="null"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        _value = _getter();
        return _value == null;
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
