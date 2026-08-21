// ============================================================================
//  WaitForSecondsRealtime.cs
// ============================================================================
//  A coroutine that waits for a specified number of seconds (unscaled time).
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for a specified number of seconds using unscaled delta time.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForSecondsRealtime"/> class pauses the coroutine execution for
/// the specified duration using unscaled delta time. This means the wait is
/// NOT affected by <see cref="FrameTime.TimeScale"/>, making it suitable for
/// real-time timing that should not be slowed down or sped up.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>UI animations that should always run at normal speed</description></item>
///   <item><description>Audio and visual effects that shouldn't be affected by time scale</description></item>
///   <item><description>Network timeouts and synchronization</description></item>
///   <item><description>Debug and profiling timers</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for 2 real seconds (ignoring time scale)
/// yield return new WaitForSecondsRealtime(2f);
/// 
/// // In a UI sequence that shouldn't be affected by pause/slow-mo
/// var sequence = new Sequence(
///     new WaitForSecondsRealtime(0.5f),
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => uiX = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Time Scale:</b>
/// Unlike <see cref="WaitForSeconds"/>, this wait is NOT affected by
/// <see cref="FrameTime.TimeScale"/>. A 2-second wait will always take
/// exactly 2 real seconds regardless of time scale settings.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitForSecondsRealtime : IEnumerator
{
    private float _remaining;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitForSecondsRealtime"/> class.
    /// </summary>
    /// <param name="seconds">The number of real seconds to wait.</param>
    public WaitForSecondsRealtime(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    /// <summary>
    /// Advances the coroutine by one frame using unscaled delta time.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.UnscaledDeltaTime;
        return _remaining > 0f;
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