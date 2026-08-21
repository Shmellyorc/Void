// ============================================================================
//  Sequence.cs
// ============================================================================
//  A coroutine that executes multiple coroutines in sequential order.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// A coroutine that executes multiple coroutines in sequential order.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Sequence"/> class allows multiple coroutines to be chained
/// together, executing one after another. Each coroutine must complete before
/// the next one begins.
/// </para>
/// <para>
/// This is useful for creating complex sequences of animations, effects, or
/// operations that need to happen in a specific order.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a sequence of tweens
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new Tween&lt;float&gt;(100f, 200f, 0.5f, EaseType.Linear, Lerp, value => x = value),
///     new Tween&lt;float&gt;(200f, 0f, 0.75f, EaseType.QuadIn, Lerp, value => x = value),
///     new Delay(0.5f),
///     new Callback(() => Console.WriteLine("Sequence complete!"))
/// );
/// 
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public class Sequence : IEnumerator
{
    private readonly IEnumerator[] _routines;
    private int _index;

    /// <summary>
    /// Gets the current value from the currently running coroutine.
    /// </summary>
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
    /// Initializes a new instance of the <see cref="Sequence"/> class.
    /// </summary>
    /// <param name="routines">The coroutines to execute in sequence.</param>
    public Sequence(params IEnumerator[] routines)
    {
        _routines = routines ?? [];
        _index = 0;
    }

    /// <summary>
    /// Advances the sequence by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the sequence is still running; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the sequence to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}