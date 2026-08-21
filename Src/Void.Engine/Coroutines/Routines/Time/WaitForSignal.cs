// ============================================================================
//  WaitForSignal.cs
// ============================================================================
//  A coroutine that waits for a manual signal to be triggered.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for a manual signal to be triggered.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForSignal"/> class pauses the coroutine execution until
/// the <see cref="Signal"/> method is called. This provides a manual way to
/// control when a coroutine should continue.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Waiting for user input</description></item>
///   <item><description>Waiting for asynchronous operations to complete</description></item>
///   <item><description>Manual control over coroutine flow</description></item>
///   <item><description>Synchronizing multiple coroutines</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a signal
/// var signal = new WaitForSignal();
/// 
/// // Start a coroutine that waits for the signal
/// CoroutineManager.Instance.Run(WaitForSignalCoroutine(signal));
/// 
/// // Later, signal it to continue
/// signal.Signal();
/// 
/// IEnumerator WaitForSignalCoroutine(WaitForSignal signal)
/// {
///     Console.WriteLine("Waiting for signal...");
///     yield return signal;
///     Console.WriteLine("Signal received!");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitForSignal : IEnumerator
{
    private bool _signaled;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Signals the coroutine to continue execution.
    /// </summary>
    public void Signal() => _signaled = true;

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext() => !_signaled;

    /// <summary>
    /// Resets the signal to its initial state.
    /// </summary>
    public void Reset() => _signaled = false;

    /// <summary>
    /// Disposes the coroutine. Does nothing.
    /// </summary>
    public void Dispose() { }
}