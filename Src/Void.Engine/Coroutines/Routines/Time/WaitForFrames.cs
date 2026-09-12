// ============================================================================
//  WaitForFrames.cs
// ============================================================================
//  A coroutine that waits for a specified number of coroutine updates.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Waits for a specified number of coroutine updates.
/// </summary>
/// <remarks>
/// The counter is reduced by one each time <see cref="MoveNext"/> is called.
/// Negative values are treated as zero. Fractional values effectively require
/// enough whole updates for the counter to reach zero or below.
/// <code>
/// yield return new WaitForFrames(30);
/// </code>
/// </remarks>
public sealed class WaitForFrames : IEnumerator
{
    private float _framesLeft;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a frame-count wait.
    /// </summary>
    /// <param name="frames">The number of coroutine updates to wait. Negative values are treated as zero.</param>
    public WaitForFrames(float frames)
    {
        _framesLeft = Math.Max(0f, frames);
    }

    /// <summary>
    /// Decrements the remaining frame count by one.
    /// </summary>
    /// <returns><see langword="true"/> while frames remain; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _framesLeft--;
        return _framesLeft > 0f;
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
