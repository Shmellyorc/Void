// ============================================================================
//  Sequence.cs
// ============================================================================
//  Coroutine composition that runs routines in order.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// Executes a collection of coroutines sequentially.
/// </summary>
/// <remarks>
/// <para>
/// Each routine runs until it completes before the next routine is advanced.
/// Null entries are skipped. The active routine's <see cref="IEnumerator.Current"/>
/// value is exposed through <see cref="Current"/>, allowing normal coroutine
/// yields to flow through the sequence.
/// </para>
/// <code>
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 0.5f, EaseType.QuadOut, Lerp, value =&gt; x = value),
///     new Tween&lt;float&gt;(100f, 0f, 0.5f, EaseType.QuadIn, Lerp, value =&gt; x = value));
///
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </remarks>
public class Sequence : IEnumerator
{
    private readonly IEnumerator[] _routines;
    private int _index;

    /// <summary>
    /// Gets the value yielded by the currently active routine.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when the sequence is empty or has completed.
    /// </remarks>
    public object Current
    {
        get
        {
            if (_routines.Length == 0 || _index >= _routines.Length)
                return null!;
            return _routines[_index]?.Current!;
        }
    }

    /// <summary>
    /// Initializes a sequence from the supplied routines.
    /// </summary>
    /// <param name="routines">
    /// The routines to execute in order. A null array produces an empty sequence,
    /// and null entries are skipped during execution.
    /// </param>
    public Sequence(params IEnumerator[] routines)
    {
        _routines = routines ?? [];
        _index = 0;
    }

    /// <summary>
    /// Advances the active routine, moving to later routines as earlier ones complete.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while a routine in the sequence remains active;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        while (_index < _routines.Length)
        {
            var r = _routines[_index];

            if (r != null && r.MoveNext())
                return true;

            _index++;
        }

        return false;
    }

    /// <summary>
    /// Resetting a sequence is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
