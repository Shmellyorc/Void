// ============================================================================
//  Delay.cs
// ============================================================================
//  A coroutine that waits for a specified number of scaled seconds.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Waits for a specified number of scaled seconds.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Delay"/> is a convenience wrapper around <see cref="WaitForSeconds"/>.
/// Its duration therefore uses <see cref="FrameTime.DeltaTime"/> and is affected by
/// <see cref="FrameTime.TimeScale"/>.
/// </para>
/// <code>
/// yield return new Delay(0.5f);
/// </code>
/// </remarks>
public sealed class Delay : IEnumerator
{
    private readonly WaitForSeconds _wait;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a delay.
    /// </summary>
    /// <param name="seconds">The number of scaled seconds to wait. Negative values are treated as zero.</param>
    public Delay(float seconds)
    {
        _wait = new WaitForSeconds(seconds);
    }

    /// <summary>
    /// Advances the delay by one coroutine update.
    /// </summary>
    /// <returns><see langword="true"/> while time remains; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => _wait.MoveNext();

    /// <summary>
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Releases the routine. This implementation has no resources to release.
    /// </summary>
    public void Dispose() { }
}
