// ============================================================================
//  CoroutineHandle.cs
// ============================================================================
//  Immutable handle for tracking and controlling a running coroutine.
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
/// <para>
/// A handle keeps the manager and root <see cref="IEnumerator"/> used when the
/// coroutine was started. The handle itself is immutable and can be copied freely.
/// </para>
/// <para>
/// The root routine remains the identity of the coroutine even while nested child
/// routines are being executed by the manager.
/// </para>
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
    /// Stops the coroutine and its active child chain when it is still running.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the coroutine was running and was stopped;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Stop()
        => Runner != null && Enumerator != null && Runner.Stop(Enumerator);

    /// <summary>
    /// Creates an enumerator that waits until this coroutine is no longer running.
    /// </summary>
    /// <returns>
    /// An enumerator that yields once per coroutine update while the coroutine remains active.
    /// </returns>
    /// <remarks>
    /// A default handle, or a handle whose coroutine has already finished or been
    /// stopped, completes immediately.
    /// </remarks>
    public IEnumerator Wait()
    {
        if (Runner == null || Enumerator == null)
            yield break;

        while (Runner.IsRunning(Enumerator))
            yield return null;
    }

    /// <summary>
    /// Gets a value indicating whether the coroutine is currently running.
    /// </summary>
    public bool IsRunning
        => Runner != null && Enumerator != null && Runner.IsRunning(Enumerator);
}
