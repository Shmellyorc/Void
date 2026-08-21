// ============================================================================
//  Concurrent.cs
// ============================================================================
//  A coroutine that executes multiple coroutines concurrently in parallel.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// A coroutine that executes multiple coroutines concurrently in parallel.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Concurrent"/> class allows multiple coroutines to run
/// simultaneously within a single coroutine. It continues until all
/// contained coroutines have completed.
/// </para>
/// <para>
/// This is useful for running multiple animations or operations at the same
/// time, such as moving a character while playing a sound effect.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Run multiple tweens concurrently
/// var concurrent = new Concurrent(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new Tween&lt;float&gt;(0f, 50f, 1.5f, EaseType.SineInOut, Lerp, value => y = value),
///     new Delay(0.5f)
/// );
/// 
/// CoroutineManager.Instance.Run(concurrent);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public class Concurrent : IEnumerator
{
    private readonly List<IEnumerator> _active;

    /// <summary>
    /// Gets the current value of the concurrent coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Concurrent"/> class.
    /// </summary>
    /// <param name="routines">The coroutines to run concurrently.</param>
    public Concurrent(params IEnumerator[] routines)
    {
        _active = routines?.Where(r => r != null).ToList() ?? [];
    }

    /// <summary>
    /// Advances all active coroutines by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if any coroutines are still running; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].MoveNext())
                _active.RemoveAt(i);
        }

        return _active.Count > 0;
    }

    /// <summary>
    /// Resets the concurrent coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}