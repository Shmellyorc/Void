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
//  Handle for tracking and controlling a coroutine managed by CoroutineManager.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections;

namespace Void.Engine.Coroutines;

/// <summary>
/// Identifies a coroutine managed by a <see cref="CoroutineManager"/>.
/// </summary>
/// <remarks>
/// A handle keeps the manager and root enumerator used when the coroutine was
/// started. It can be used to query, stop, or wait for that coroutine without
/// retaining those values separately.
/// </remarks>
public readonly struct CoroutineHandle
{
    /// <summary>
    /// Gets the manager that owns the coroutine.
    /// </summary>
    public CoroutineManager Runner { get; }

    /// <summary>
    /// Gets the root enumerator registered with the manager.
    /// </summary>
    public IEnumerator Enumerator { get; }

    internal CoroutineHandle(CoroutineManager runner, IEnumerator enumerator)
    {
        Runner = runner;
        Enumerator = enumerator;
    }

    /// <summary>
    /// Stops the coroutine when it is still running.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the coroutine was running and was stopped;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Stop() => IsRunning && Runner.Stop(Enumerator);

    /// <summary>
    /// Creates an enumerator that waits until this coroutine is no longer running.
    /// </summary>
    /// <returns>
    /// An enumerator that yields once per update while the coroutine remains active.
    /// </returns>
    /// <remarks>
    /// A default handle, or a handle whose coroutine has already finished or been
    /// stopped, completes immediately.
    /// </remarks>
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
