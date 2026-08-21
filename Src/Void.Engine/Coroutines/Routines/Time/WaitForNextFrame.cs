// ============================================================================
//  WaitForNextFrame.cs
// ============================================================================
//  A coroutine that waits for the next frame.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits for the next frame.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForNextFrame"/> class pauses the coroutine execution
/// until the next frame. This is the smallest possible delay in a coroutine.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Deferring execution by one frame</description></item>
///   <item><description>Breaking up heavy operations across frames</description></item>
///   <item><description>Ensuring other systems have a chance to update</description></item>
///   <item><description>Implementing frame-perfect timing</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for the next frame
/// yield return new WaitForNextFrame();
/// 
/// // Wait for 5 frames (repeats the wait)
/// for (int i = 0; i < 5; i++)
///     yield return new WaitForNextFrame();
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new WaitForNextFrame(),
///     new Tween&lt;float&gt;(0f, 100f, 1f, EaseType.QuadOut, Lerp, value => x = value)
/// );
/// CoroutineManager.Instance.Run(sequence);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public class WaitForNextFrame : IEnumerator
{
    private bool _first = true;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> on the first frame; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (_first)
        {
            _first = false;
            return true;
        }
        return false;
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