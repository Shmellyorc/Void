// ============================================================================
//  WaitForFrames.cs
// ============================================================================
//  A coroutine that waits for a specified number of frames.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for a specified number of frames.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForFrames"/> class pauses the coroutine execution for a
/// specified number of frames. Unlike time-based waits, this is frame-count
/// based and will complete faster at higher frame rates.
/// </para>
/// <para>
/// This is useful for frame-dependent operations such as:
/// <list type="bullet">
///   <item><description>Waiting for a specific number of rendering frames</description></item>
///   <item><description>Synchronizing with frame-based animations</description></item>
///   <item><description>Delaying actions that should be measured in frames</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for 30 frames (approximately 0.5 seconds at 60 FPS)
/// yield return new WaitForFrames(30);
/// 
/// // Wait for 60 frames (approximately 1 second at 60 FPS)
/// yield return new WaitForFrames(60);
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value),
///     new WaitForFrames(15),
///     new Tween&lt;float&gt;(100f, 200f, 1f, EaseType.QuadOut, Lerp, value => x = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class WaitForFrames : IEnumerator
{
    private float _framesLeft;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitForFrames"/> class.
    /// </summary>
    /// <param name="frames">The number of frames to wait.</param>
    public WaitForFrames(float frames)
    {
        _framesLeft = Math.Max(0f, frames);
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _framesLeft--;
        return _framesLeft > 0f;
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