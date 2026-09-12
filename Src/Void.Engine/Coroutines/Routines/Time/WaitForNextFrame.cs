// ============================================================================
//  WaitForNextFrame.cs
// ============================================================================
//  A coroutine that remains active for one coroutine update.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Keeps a coroutine waiting for one update before completing.
/// </summary>
/// <remarks>
/// The first call to <see cref="MoveNext"/> returns <see langword="true"/> and
/// the second returns <see langword="false"/>.
/// <code>
/// yield return new WaitForNextFrame();
/// </code>
/// </remarks>
public class WaitForNextFrame : IEnumerator
{
    private bool _first = true;

    /// <summary>
    /// Gets the current yielded value. This routine always yields <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Advances the wait state.
    /// </summary>
    /// <returns><see langword="true"/> on the first call; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (_first)
        {
            _first = false;
            return true;
        }
        return false;
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
