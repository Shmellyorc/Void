// ============================================================================
//  DoOnce.cs
// ============================================================================
//  Coroutine utility that invokes an action once and then completes.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// Invokes an action the first time the routine is advanced, then completes.
/// </summary>
/// <remarks>
/// <para>
/// The action is not run by the constructor. It runs on the first call to
/// <see cref="MoveNext"/>, which makes this type useful for inserting a side
/// effect into a composed coroutine flow.
/// </para>
/// <code>
/// var sequence = new Sequence(
///     new DoOnce(() => OpenDoor()),
///     new Delay(0.5f),
///     new DoOnce(() => CloseDoor())
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </remarks>
public sealed class DoOnce : IEnumerator
{
    private readonly Action _action;
    private bool _done;

    /// <summary>
    /// Gets the value yielded by this routine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a one-shot coroutine action.
    /// </summary>
    /// <param name="action">The action to invoke once.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="action"/> is <see langword="null"/>.
    /// </exception>
    public DoOnce(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Invokes the action once and completes the routine.
    /// </summary>
    /// <returns>Always <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (!_done)
        {
            _action();
            _done = true;
        }
        return false;
    }

    /// <summary>
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases this routine. This implementation performs no work.
    /// </summary>
    public void Dispose() { }
}
