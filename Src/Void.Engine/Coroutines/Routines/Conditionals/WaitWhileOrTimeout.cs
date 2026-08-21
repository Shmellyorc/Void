// ============================================================================
//  WaitWhileOrTimeout.cs
// ============================================================================
//  A coroutine that waits while a condition remains true or a timeout occurs.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits while a condition remains true or a timeout occurs.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitWhileOrTimeout"/> class pauses the coroutine execution
/// until either the specified condition returns <see langword="false"/> or the
/// timeout duration elapses, whichever happens first.
/// </para>
/// <para>
/// This is the inverse of <see cref="WaitUntilOrTimeout"/> and is useful for
/// scenarios where you want to wait for a condition to become false but don't
/// want to wait forever, such as waiting for a state to change, an animation
/// to finish, or a process to complete with a fallback.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait while an animation is playing with a 5-second timeout
/// yield return new WaitWhileOrTimeout(() => isAnimating, 5f);
/// 
/// // Wait while health is above zero with timeout
/// yield return new WaitWhileOrTimeout(() => health > 0, 10f);
/// 
/// // In a sequence with fallback
/// var sequence = new Sequence(
///     new WaitWhileOrTimeout(() => isProcessing, 3f),
///     new Callback(() => 
///     {
///         if (isProcessing)
///             HandleTimeout();
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
public sealed class WaitWhileOrTimeout : IEnumerator
{
    private readonly Func<bool> _condition;
    private readonly float _timeout;
    private float _elapsed;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitWhileOrTimeout"/> class.
    /// </summary>
    /// <param name="condition">The condition to wait for. Returns <see langword="true"/> while waiting should continue.</param>
    /// <param name="timeoutSeconds">The maximum time to wait in seconds.</param>
    public WaitWhileOrTimeout(Func<bool> condition, float timeoutSeconds)
    {
        _condition = condition;
        _timeout = timeoutSeconds;
        _elapsed = 0f;
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        if (!_condition())
            return false;

        _elapsed += Game.Instance.FrameTime.DeltaTime;
        return _elapsed < _timeout;
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