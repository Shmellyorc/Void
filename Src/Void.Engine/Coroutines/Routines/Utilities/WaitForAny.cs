// ============================================================================
//  WaitForAny.cs
// ============================================================================
//  Coroutine utility that completes when any wrapped routine completes.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// Advances multiple routines and completes when the first one finishes.
/// </summary>
/// <remarks>
/// Null entries are ignored. Each call to <see cref="MoveNext"/> advances the
/// remaining routines in their original order until one returns
/// <see langword="false"/>. An empty routine set never completes on its own.
/// </remarks>
public sealed class WaitForAny : IEnumerator, IDisposable
{
    private readonly IEnumerator[] _routines;
    private bool _completed;

    /// <summary>
    /// Gets the current value exposed by the first stored routine, or
    /// <see langword="null"/> when no routine is available.
    /// </summary>
    public object Current
    {
        get
        {
            foreach (var r in _routines)
            {
                if (r != null)
                    return r.Current!;
            }
            return null!;
        }
    }

    /// <summary>
    /// Initializes a wait that completes when any supplied routine finishes.
    /// </summary>
    /// <param name="routines">
    /// The routines to advance. Null entries are discarded.
    /// </param>
    public WaitForAny(params IEnumerator[] routines)
        => _routines = routines?.Where(r => r != null).ToArray() ?? [];

    /// <summary>
    /// Advances each stored routine until one completes.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while all advanced routines continue running;
    /// otherwise, <see langword="false"/> once any routine completes.
    /// </returns>
    public bool MoveNext()
    {
        if (_completed) return false;

        foreach (var r in _routines)
        {
            if (r != null && !r.MoveNext())
            {
                _completed = true;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes each stored routine that implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        foreach (var r in _routines)
            (r as IDisposable)?.Dispose();
    }
}
