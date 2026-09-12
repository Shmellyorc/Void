// ============================================================================
//  Timeout.cs
// ============================================================================
//  A coroutine wrapper that stops after an accumulated timeout.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// Wraps a coroutine and stops advancing it after a timeout is reached.
/// </summary>
/// <remarks>
/// <para>
/// The timeout accumulates <see cref="FrameTime.DeltaTime"/> each time this
/// wrapper's <see cref="MoveNext"/> method is advanced. A negative timeout disables
/// the timeout and allows the wrapped coroutine to run until it completes.
/// </para>
/// <para>
/// <see cref="Current"/> forwards the wrapped coroutine's current value so normal
/// VOID yields, including delays and nested routines, can be processed by the
/// coroutine manager. Time spent while the manager is processing such a forwarded
/// yield does not advance this wrapper's timeout counter.
/// </para>
/// <code>
/// yield return new Timeout(new WaitUntil(() =&gt; ready), 5f);
/// </code>
/// </remarks>
public sealed class Timeout : IEnumerator, IDisposable
{
    private readonly IEnumerator _inner;
    private readonly float _timeout;
    private float _elapsed;

    /// <summary>
    /// Gets the current yielded value from the wrapped coroutine.
    /// </summary>
    public object Current => _inner?.Current!;

    /// <summary>
    /// Initializes a timeout wrapper.
    /// </summary>
    /// <param name="inner">The coroutine to wrap.</param>
    /// <param name="timeout">The timeout in scaled seconds. A negative value disables the timeout.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <see langword="null"/>.</exception>
    public Timeout(IEnumerator inner, float timeout)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeout = timeout;
    }

    /// <summary>
    /// Advances the timeout and then the wrapped coroutine when time remains.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while the wrapped coroutine is running and the timeout has not been reached;
    /// otherwise, <see langword="false"/>.
    /// </returns>
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
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the wrapped coroutine when it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
