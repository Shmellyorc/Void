// ============================================================================
//  WaitForAny.cs
// ============================================================================
//  A coroutine that waits for any of multiple coroutines to complete.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// A coroutine that waits for any of multiple coroutines to complete.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForAny"/> class runs multiple coroutines concurrently
/// and completes as soon as any one of them finishes. The remaining
/// coroutines are disposed when the wait completes.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Waiting for the first of several events to occur</description></item>
///   <item><description>Race conditions between multiple operations</description></item>
///   <item><description>Implementing timeouts or fallback mechanisms</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for the first to complete
/// var waitAny = new WaitForAny(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new Delay(0.5f),
///     new WaitForSeconds(2f)
/// );
/// CoroutineManager.Instance.Run(waitAny);
/// 
/// // In a sequence with timeout
/// var sequence = new Sequence(
///     new WaitForAny(
///         new WaitUntil(() => isReady),
///         new Timeout(new WaitForSeconds(5f), 5f)
///     ),
///     new DoOnce(() => 
///     {
///         if (!isReady)
///             Console.WriteLine("Timed out!");
///     })
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitForAny : IEnumerator, IDisposable
{
    private readonly IEnumerator[] _routines;
    private bool _completed;

    /// <summary>
    /// Gets the current value from the first non-null running coroutine.
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
    /// Initializes a new instance of the <see cref="WaitForAny"/> class.
    /// </summary>
    /// <param name="routines">The coroutines to run concurrently.</param>
    public WaitForAny(params IEnumerator[] routines)
        => _routines = routines?.Where(r => r != null).ToArray() ?? [];

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes all wrapped coroutines that are disposable.
    /// </summary>
    public void Dispose()
    {
        foreach (var r in _routines)
            (r as IDisposable)?.Dispose();
    }
}