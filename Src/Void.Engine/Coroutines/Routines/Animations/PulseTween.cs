// ============================================================================
//  PulseTween.cs
// ============================================================================
//  A tween that pulses between two values like a heartbeat or breathing effect.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// A tween that pulses between two values like a heartbeat or breathing effect.
/// </summary>
/// <typeparam name="T">The type of value being tweened.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="PulseTween{T}"/> class creates a pulse effect by animating
/// from one value to another and back again in a single cycle. Each cycle
/// consists of a forward tween followed by a reverse tween.
/// </para>
/// <para>
/// This class implements <see cref="IEnumerator"/> and can be used directly
/// with the <see cref="CoroutineManager"/> or within other coroutines.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a pulse tween (scale up and down)
/// var tween = new PulseTween&lt;float&gt;(
///     a: 1f,
///     b: 1.5f,
///     durationPerCycle: 1f,
///     type: EaseType.QuadOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => transform.Scale = value,
///     cycles: -1  // Infinite
/// );
/// 
/// // Create a pulse tween (3 cycles)
/// var tween2 = new PulseTween&lt;float&gt;(
///     a: 0f,
///     b: 100f,
///     durationPerCycle: 2f,
///     type: EaseType.SineInOut,
///     lerpFunc: (a, b, t) => MathHelper.Lerp(a, b, t),
///     onUpdate: value => position.X = value,
///     cycles: 3
/// );
/// 
/// // Run the tween
/// CoroutineManager.Instance.Run(tween);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
/// </remarks>
public sealed class PulseTween<T> : IEnumerator
{
    private readonly T _a, _b;
    private readonly float _durationPerCycle;
    private readonly int _cycles;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;
    private int _completedCycles;

    /// <summary>
    /// Gets the current value of the tween. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="PulseTween{T}"/> class.
    /// </summary>
    /// <param name="a">The first value (start of pulse).</param>
    /// <param name="b">The second value (peak of pulse).</param>
    /// <param name="durationPerCycle">The duration of one complete pulse cycle in seconds.</param>
    /// <param name="type">The easing type to use.</param>
    /// <param name="lerpFunc">The interpolation function for the type T.</param>
    /// <param name="onUpdate">The action to invoke with the current tween value.</param>
    /// <param name="cycles">The number of pulse cycles, or -1 for infinite pulsing.</param>
    public PulseTween(T a, T b, float durationPerCycle, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, int cycles = -1)
    {
        _a = a;
        _b = b;
        _durationPerCycle = durationPerCycle;
        _cycles = cycles;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
        _elapsed = 0f;
        _completedCycles = 0;
    }

    /// <summary>
    /// Advances the tween by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if the tween is still running; otherwise, <see langword="false"/>.</returns>
    public bool MoveNext()
    {
        float deltaTime = Game.Instance.FrameTime.DeltaTime;

        if (_cycles != -1 && _completedCycles >= _cycles)
        {
            _onUpdate?.Invoke(_b);
            return false;
        }

        float halfDuration = _durationPerCycle / 2f;

        if (_elapsed < _durationPerCycle)
        {
            float normalized;
            T currentValue;

            if (_elapsed < halfDuration)
            {
                normalized = _elapsed / halfDuration;
                float eased = Easing.Ease(_type, normalized);
                currentValue = _lerp(_a, _b, eased);
            }
            else
            {
                normalized = (_elapsed - halfDuration) / halfDuration;
                float eased = Easing.Ease(_type, normalized);
                currentValue = _lerp(_b, _a, eased);
            }

            _onUpdate?.Invoke(currentValue);
            _elapsed += deltaTime;
            return true;
        }

        _completedCycles++;
        _elapsed = 0f;

        return true;
    }

    /// <summary>
    /// Resets the tween to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();
}