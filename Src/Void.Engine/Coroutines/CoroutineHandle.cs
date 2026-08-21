/*
    MIT License

    Copyright (c) 2017 Chevy Ray Johnston

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

// ============================================================================
//  CoroutineHandle.cs
// ============================================================================
//  A handle for tracking and controlling a running coroutine.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections;

namespace Void.Engine.Coroutines;

/// <summary>
/// A handle for tracking and controlling a running coroutine.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CoroutineHandle"/> structure provides a lightweight way to
/// reference and control a coroutine that was started through the
/// <see cref="CoroutineManager"/>. It can be used to stop the coroutine,
/// check its status, or wait for its completion.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Start a coroutine and get a handle
/// var handle = CoroutineManager.Instance.Run(MyCoroutine());
/// 
/// // Check if it's still running
/// if (handle.IsRunning)
/// {
///     // Do something while it runs
/// }
/// 
/// // Stop the coroutine
/// handle.Stop();
/// 
/// // Wait for the coroutine to complete from another coroutine
/// IEnumerator WaitForCoroutine()
/// {
///     yield return handle.Wait();
///     Console.WriteLine("Coroutine finished!");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe. However, the underlying
/// coroutine operations are not thread-safe and should only be performed
/// from the main thread.
/// </para>
/// </remarks>
public readonly struct CoroutineHandle
{
    /// <summary>
    /// Gets the coroutine manager that is running this coroutine.
    /// </summary>
    public CoroutineManager Runner { get; }

    /// <summary>
    /// Gets the enumerator representing the coroutine.
    /// </summary>
    public IEnumerator Enumerator { get; }

    internal CoroutineHandle(CoroutineManager runner, IEnumerator enumerator)
    {
        Runner = runner;
        Enumerator = enumerator;
    }

    /// <summary>
    /// Stops the coroutine if it is currently running.
    /// </summary>
    /// <returns><see langword="true"/> if the coroutine was stopped; otherwise, <see langword="false"/>.</returns>
    public bool Stop() => IsRunning && Runner.Stop(Enumerator);

    /// <summary>
    /// Returns a coroutine that waits for this coroutine to complete.
    /// </summary>
    /// <returns>An enumerator that yields until the coroutine completes.</returns>
    public IEnumerator Wait()
    {
        if (Enumerator != null)
            while (Runner.IsRunning(Enumerator))
                yield return null;
    }

    /// <summary>
    /// Gets a value indicating whether the coroutine is currently running.
    /// </summary>
    public bool IsRunning => Enumerator != null && Runner.IsRunning(Enumerator);
}