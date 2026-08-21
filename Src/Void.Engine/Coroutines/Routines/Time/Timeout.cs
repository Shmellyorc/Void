// ============================================================================
//  Timeout.cs
// ============================================================================
//  A coroutine wrapper that limits execution time with a timeout.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine wrapper that limits execution time with a timeout.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Timeout"/> class wraps another coroutine and ensures it
/// does not run longer than the specified timeout duration. If the timeout
/// is reached, the wrapper stops and returns <see langword="false"/>.
/// </para>
/// <para>
/// This is useful for preventing coroutines from running indefinitely,
/// such as waiting for a condition that might never become true, or
/// protecting against infinite loops in user code.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wrap a coroutine with a 5-second timeout
/// var timed = new Timeout(
///     new WaitUntil(() => isReady),
///     5f
/// );
/// 
/// // In a sequence with fallback
/// var sequence = new Sequence(
///     new Timeout(new WaitUntil(() => hasLoaded), 10f),
///     new Callback(() => 
///     {
///         if (!hasLoaded)
///             LoadFallbackContent();
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
public sealed class Timeout : IEnumerator, IDisposable
{
    private readonly IEnumerator _inner;
    private readonly float _timeout;
    private float _elapsed;

    /// <summary>
    /// Gets the current value from the wrapped coroutine.
    /// </summary>
    public object Current => _inner?.Current!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Timeout"/> class.
    /// </summary>
    /// <param name="inner">The coroutine to wrap with a timeout.</param>
    /// <param name="timeout">The maximum time in seconds before the timeout triggers.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is null.</exception>
    public Timeout(IEnumerator inner, float timeout)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeout = timeout;
    }

    /// <summary>
    /// Advances the wrapped coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the wrapped coroutine is still running and the timeout hasn't been reached; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (_timeout >= 0f)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            if (_elapsed >= _timeout)
                return false;
        }

        return _inner.MoveNext();
    }

    /// <summary>
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the wrapped coroutine if it is disposable.
    /// </summary>
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}