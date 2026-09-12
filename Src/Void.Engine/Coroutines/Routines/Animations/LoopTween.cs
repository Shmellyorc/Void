// ============================================================================
//  LoopTween.cs
// ============================================================================
//  Repeating tween with finite or indefinite looping.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Animations;

/// <summary>
/// Repeats interpolation from a starting value to an ending value.
/// </summary>
/// <typeparam name="T">The value type being interpolated.</typeparam>
/// <remarks>
/// Each loop restarts from the beginning after the previous traversal reaches
/// its duration. A loop count of <c>-1</c> repeats indefinitely. For finite
/// counts, the count is checked after each completed traversal.
/// </remarks>
public sealed class LoopTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private readonly int _maxLoops;
    private float _elapsed;
    private int _currentLoop;

    /// <summary>
    /// Gets the value yielded by the enumerator. Loop tweens do not yield a
    /// value, so this property returns <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a repeating tween.
    /// </summary>
    /// <param name="from">The starting value for each traversal.</param>
    /// <param name="to">The ending value for each traversal.</param>
    /// <param name="duration">The duration of each traversal in seconds.</param>
    /// <param name="type">The easing function to apply to normalized progress.</param>
    /// <param name="lerpFunc">The interpolation function used to produce values of type <typeparamref name="T"/>.</param>
    /// <param name="onUpdate">The callback that receives each interpolated value.</param>
    /// <param name="loops">The loop count, or <c>-1</c> to repeat indefinitely.</param>
    public LoopTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, int loops = -1)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
        _maxLoops = loops;
    }

    /// <summary>
    /// Advances the current traversal using the current frame delta time.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while another traversal remains;
    /// otherwise, <see langword="false"/> after applying the ending value.
    /// </returns>
    public bool MoveNext()
    {
        float deltaTime = Game.Instance.FrameTime.DeltaTime;

        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += deltaTime;
            return true;
        }

        _currentLoop++;

        if (_maxLoops == -1 || _currentLoop < _maxLoops)
        {
            _elapsed = 0f;
            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    /// <summary>
    /// Resetting a loop tween is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();
}
