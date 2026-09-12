// ============================================================================
//  Concurrent.cs
// ============================================================================
//  Coroutine composition that advances multiple routines together.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;

namespace Void.Engine.Coroutines.Routines.Compositions;

/// <summary>
/// Advances multiple coroutines together until all of them have completed.
/// </summary>
/// <remarks>
/// <para>
/// Each child routine is advanced independently. Numeric values yielded by a
/// child are treated as delays in seconds, and nested <see cref="IEnumerator"/>
/// values are executed before that child continues. This matches the yield
/// behavior provided by <see cref="CoroutineManager"/> for top-level routines.
/// </para>
/// <para>
/// Completed routines are removed automatically, and null routines supplied to
/// the constructor are ignored. Child yields are consumed internally, so
/// <see cref="Current"/> remains <see langword="null"/>.
/// </para>
/// <code>
/// IEnumerator MoveLater()
/// {
///     yield return 0.5f;
///     position.X = 100f;
/// }
///
/// IEnumerator Flash()
/// {
///     yield return FlashRoutine();
/// }
///
/// CoroutineManager.Instance.Run(new Concurrent(MoveLater(), Flash()));
/// </code>
/// </remarks>
public class Concurrent : IEnumerator
{
    private struct RoutineState
    {
        public IEnumerator Routine;
        public float Delay;

        public RoutineState(IEnumerator routine)
        {
            Routine = routine;
            Delay = 0f;
        }
    }

    private readonly List<RoutineState> _active;

    /// <summary>
    /// Gets the value yielded by this composition, which is always <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Child yield values are interpreted internally so each concurrent routine
    /// can maintain its own delay and nested-coroutine state.
    /// </remarks>
    public object Current => null!;

    /// <summary>
    /// Initializes a concurrent composition from the supplied routines.
    /// </summary>
    /// <param name="routines">
    /// The routines to advance together. Null entries are ignored.
    /// </param>
    public Concurrent(params IEnumerator[] routines)
    {
        _active = new List<RoutineState>(routines?.Length ?? 0);

        if (routines == null)
            return;

        for (int i = 0; i < routines.Length; i++)
        {
            if (routines[i] != null)
                _active.Add(new RoutineState(routines[i]));
        }
    }

    /// <summary>
    /// Advances each active routine while honoring its delays and nested coroutines.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while at least one routine remains active; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool MoveNext()
    {
        float frameTime = Game.Instance.FrameTime.DeltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var state = _active[i];

            if (state.Delay > 0f)
            {
                state.Delay -= frameTime;
                _active[i] = state;
                continue;
            }

            if (!Advance(state.Routine, ref state))
            {
                _active.RemoveAt(i);
                continue;
            }

            _active[i] = state;
        }

        return _active.Count > 0;
    }

    /// <summary>
    /// Resetting a concurrent composition is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    private static bool Advance(IEnumerator routine, ref RoutineState state)
    {
        if (routine.Current is IEnumerator nested)
        {
            if (Advance(nested, ref state))
                return true;

            state.Delay = 0f;

            if (!routine.MoveNext())
                return false;

            SetDelay(routine.Current, ref state);
            return true;
        }

        if (!routine.MoveNext())
            return false;

        SetDelay(routine.Current, ref state);
        return true;
    }

    private static void SetDelay(object current, ref RoutineState state)
    {
        state.Delay = current switch
        {
            float value => value,
            double value => (float)value,
            int value => value,
            _ => 0f
        };
    }
}
