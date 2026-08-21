// ============================================================================
//  WaitWhileNotNull.cs
// ============================================================================
//  A coroutine that waits while a value remains non-null.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits while a value remains non-null.
/// </summary>
/// <typeparam name="T">The type of value to monitor.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="WaitWhileNotNull{T}"/> class pauses the coroutine execution
/// while the specified getter function returns a non-null value. The getter
/// is called each frame.
/// </para>
/// <para>
/// This is the inverse of <see cref="WaitUntilNotNull{T}"/> and is useful for
/// waiting for an object to be destroyed, unloaded, or cleared, such as
/// waiting for an enemy to die, a resource to be unloaded, or a reference
/// to be released.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait while an enemy exists (i.e., until it dies)
/// var waitForEnemy = new WaitWhileNotNull&lt;Enemy&gt;(() => currentEnemy);
/// yield return waitForEnemy;
/// 
/// // Wait while a reference is valid
/// var waitForNull = new WaitWhileNotNull&lt;Texture&gt;(() => texture);
/// yield return waitForNull;
/// 
/// // In a sequence
/// var sequence = new Sequence(
///     new WaitWhileNotNull&lt;Projectile&gt;(() => activeProjectile),
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
public sealed class WaitWhileNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Gets the current value from the getter.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitWhileNotNull{T}"/> class.
    /// </summary>
    /// <param name="getter">A function that returns the value to monitor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="getter"/> is null.</exception>
    public WaitWhileNotNull(Func<T> getter)
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
        return _value != null;
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