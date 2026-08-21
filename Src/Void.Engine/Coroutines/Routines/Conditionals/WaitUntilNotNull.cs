// ============================================================================
//  WaitUntilNotNull.cs
// ============================================================================
//  A coroutine that waits until a value becomes non-null.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits until a value becomes non-null.
/// </summary>
/// <typeparam name="T">The type of value to wait for.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="WaitUntilNotNull{T}"/> class pauses the coroutine execution
/// until the specified getter function returns a non-null value. The getter
/// is called each frame.
/// </para>
/// <para>
/// This is useful for waiting for an object to be created, loaded, or
/// initialized, such as waiting for a resource to load or an entity to spawn.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for a player object to be created
/// var waitForPlayer = new WaitUntilNotNull&lt;Player&gt;(() => playerInstance);
/// yield return waitForPlayer;
/// 
/// // Access the value after the wait
/// var player = waitForPlayer.Value;
/// player.Move();
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new WaitUntilNotNull&lt;Texture&gt;(() => AssetManager.Instance.Get&lt;Texture&gt;("player")),
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
public sealed class WaitUntilNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Gets the value once it becomes non-null.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitUntilNotNull{T}"/> class.
    /// </summary>
    /// <param name="getter">A function that returns the value to wait for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="getter"/> is null.</exception>
    public WaitUntilNotNull(Func<T> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        _value = _getter();
        return _value == null;
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