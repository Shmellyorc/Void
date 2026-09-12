// ============================================================================
//  WaitForSecondsRealtime.cs
// ============================================================================
//  A coroutine that waits using unscaled delta time.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Waits for a specified duration using unscaled delta time.
/// </summary>
/// <remarks>
/// <para>
/// Each update subtracts <see cref="FrameTime.UnscaledDeltaTime"/> from the
/// remaining duration, so the wait is not affected by <see cref="FrameTime.TimeScale"/>.
/// In fixed-timestep mode, the unscaled delta reflects the fixed update interval
/// supplied by <see cref="FrameTime"/>, rather than a separate wall-clock timer.
/// </para>
/// <code>
/// yield return new WaitForSecondsRealtime(2f);
/// </code>
/// </remarks>
public sealed class WaitForSecondsRealtime : IEnumerator
{
    private float _remaining;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes an unscaled-time wait.
    /// </summary>
    /// <param name="seconds">The duration in unscaled seconds. Negative values are treated as zero.</param>
    public WaitForSecondsRealtime(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    /// <summary>
    /// Subtracts the current unscaled delta time from the remaining duration.
    /// </summary>
    /// <returns><see langword="true"/> while time remains; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.UnscaledDeltaTime;
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
