// ============================================================================
//  DoOnce.cs
// ============================================================================
//  A coroutine that executes an action once and immediately completes.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// A coroutine that executes an action once and immediately completes.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DoOnce"/> class executes the provided action immediately
/// when the coroutine starts and then completes. This is useful for
/// embedding side effects into sequences or concurrent coroutines.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Executing callbacks within a sequence</description></item>
///   <item><description>Initializing state in a coroutine flow</description></item>
///   <item><description>Triggering side effects in a concurrent group</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Execute an action once
/// CoroutineManager.Instance.Run(new DoOnce(() => Console.WriteLine("Done!")));
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new DoOnce(() => Console.WriteLine("Halfway!")),
///     new Tween&lt;float&gt;(100f, 200f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new DoOnce(() => Console.WriteLine("Complete!"))
/// );
/// CoroutineManager.Instance.Run(sequence);
/// 
/// // In a concurrent group
/// var concurrent = new Concurrent(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new DoOnce(() => Console.WriteLine("Started!"))
/// );
/// CoroutineManager.Instance.Run(concurrent);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class DoOnce : IEnumerator
{
    private readonly Action _action;
    private bool _done;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DoOnce"/> class.
    /// </summary>
    /// <param name="action">The action to execute once.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    public DoOnce(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns>Always returns <see langword="false"/> (completes immediately).</returns>
    public bool MoveNext()
    {
        if (!_done)
        {
            _action();
            _done = true;
        }
        return false;
    }

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}