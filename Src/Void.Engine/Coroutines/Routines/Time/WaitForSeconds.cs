// ============================================================================
//  WaitForSeconds.cs
// ============================================================================
//  A coroutine that waits using scaled delta time.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Waits for a specified duration using scaled delta time.
/// </summary>
/// <remarks>
/// <para>
/// Each update subtracts <see cref="FrameTime.DeltaTime"/> from the remaining
/// duration, so the wait is affected by <see cref="FrameTime.TimeScale"/>.
/// Use <see cref="WaitForSecondsRealtime"/> when the wait should ignore time scale.
/// </para>
/// <code>
/// yield return new WaitForSeconds(2f);
/// </code>
/// </remarks>
public sealed class WaitForSeconds : IEnumerator
{
    private float _remaining;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a scaled-time wait.
    /// </summary>
    /// <param name="seconds">The duration in scaled seconds. Negative values are treated as zero.</param>
    public WaitForSeconds(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    /// <summary>
    /// Subtracts the current scaled delta time from the remaining duration.
    /// </summary>
    /// <returns><see langword="true"/> while time remains; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.DeltaTime;
        return _remaining > 0f;
    }

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
