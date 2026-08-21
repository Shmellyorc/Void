// ============================================================================
//  WaitForSeconds.cs
// ============================================================================
//  A coroutine that waits for a specified number of seconds (scaled time).
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for a specified number of seconds using scaled delta time.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForSeconds"/> class pauses the coroutine execution for
/// the specified duration using scaled delta time. This means the wait is
/// affected by <see cref="FrameTime.TimeScale"/>, allowing for slow-motion
/// or fast-forward effects.
/// </para>
/// <para>
/// This is the most commonly used wait coroutine and is suitable for most
/// gameplay timing needs.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for 2 seconds
/// yield return new WaitForSeconds(2f);
/// 
/// // Wait for 0.5 seconds
/// yield return new WaitForSeconds(0.5f);
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new WaitForSeconds(0.5f),
///     new Tween&lt;float&gt;(100f, 200f, 1f, EaseType.QuadOut, Lerp, value => x = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Time Scale:</b>
/// The wait duration is affected by <see cref="FrameTime.TimeScale"/>.
/// If TimeScale is 0.5, a 2-second wait will take 4 real seconds.
/// Use <see cref="WaitForSecondsRealtime"/> for unscaled timing.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitForSeconds : IEnumerator
{
    private float _remaining;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitForSeconds"/> class.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait (scaled by time scale).</param>
    public WaitForSeconds(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.DeltaTime;
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