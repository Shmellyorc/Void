// ============================================================================
//  PulseTween.cs
// ============================================================================
//  Repeating two-phase tween that moves between two values and back.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Repeats a two-phase interpolation from one value to another and back.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// <para>
/// Each cycle spends half of its duration interpolating from <c>a</c> to
/// <c>b</c>, then half interpolating from <c>b</c> back to <c>a</c>. A cycle
/// count of <c>-1</c> repeats indefinitely.
/// </para>
/// <para>
/// For a finite cycle count, the implementation applies <c>b</c> as its final
/// update before the enumerator completes.
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
    /// Gets the value yielded by the enumerator. Pulse tweens do not yield a
    /// value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a pulsing tween.
    /// </summary>
    /// <param name="a">The first value and start of each cycle.</param>
    /// <param name="b">The second value reached halfway through each cycle.</param>
    /// <param name="durationPerCycle">The duration of one complete forward-and-reverse cycle in seconds.</param>
    /// <param name="type">The easing function applied independently to each half of the cycle.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    /// <param name="cycles">The number of cycles, or <c>-1</c> to repeat indefinitely.</param>
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
    /// Advances the current pulse cycle using the current frame delta time.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while additional pulse work remains;
    /// otherwise, <see langword="false"/>.
    /// </returns>
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
    /// Resetting a pulse tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
