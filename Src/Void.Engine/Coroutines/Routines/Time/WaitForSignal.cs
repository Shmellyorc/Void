// ============================================================================
//  WaitForSignal.cs
// ============================================================================
//  A coroutine that waits until manually signaled.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Waits until <see cref="Signal"/> is called.
/// </summary>
/// <remarks>
/// A signaled instance remains signaled until <see cref="Reset"/> is called.
/// <code>
/// var signal = new WaitForSignal();
/// yield return signal;
///
/// // Elsewhere:
/// signal.Signal();
/// </code>
/// </remarks>
public sealed class WaitForSignal : IEnumerator
{
    private bool _signaled;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Marks the wait as complete.
    /// </summary>
    public void Signal() => _signaled = true;

    /// <summary>
    /// Checks whether the signal has been raised.
    /// </summary>
    /// <returns><see langword="true"/> while waiting for a signal; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => !_signaled;

    /// <summary>
    /// Clears the signal so the instance can wait again.
    /// </summary>
    public void Reset() => _signaled = false;

    /// <summary>
    /// Releases the routine. This implementation has no resources to release.
    /// </summary>
    public void Dispose() { }
}
