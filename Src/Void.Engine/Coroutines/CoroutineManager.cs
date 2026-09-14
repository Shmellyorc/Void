// ============================================================================
//  CoroutineManager.cs
// ============================================================================
//  VOID-native frame-driven coroutine runner with delays, nesting, and control.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections;

namespace Void.Engine.Coroutines;

/// <summary>
/// Runs frame-driven <see cref="IEnumerator"/> coroutines.
/// </summary>
/// <remarks>
/// <para>
/// Coroutines are advanced by VOID's game loop and always remain normal
/// <see cref="IEnumerator"/> routines. No custom coroutine base type or interface
/// is required.
/// </para>
/// <para>
/// Yielding a <see cref="float"/>, <see cref="double"/>, or <see cref="int"/>
/// pauses the active coroutine chain for that many scaled seconds. Yielding another
/// <see cref="IEnumerator"/> runs that child to completion before its parent resumes.
/// Child routines may yield their own children to any practical nesting depth.
/// Other yielded values, including <see langword="null"/>, resume on a later manager
/// update without adding a timed delay.
/// </para>
/// <para>
/// The manager is intended for the game thread. Starting or stopping coroutines
/// while the manager is updating is supported, but the manager itself is not
/// designed for concurrent multi-threaded access.
/// </para>
/// <code>
/// IEnumerator FlashMessage()
/// {
///     ShowMessage();
///     yield return 1.5f;
///     HideMessage();
/// }
///
/// CoroutineHandle handle = CoroutineManager.Instance.Run(FlashMessage());
///
/// if (handle.IsRunning)
///     handle.Stop();
/// </code>
/// </remarks>
public sealed class CoroutineManager
{
    private sealed class CoroutineState
    {
        public IEnumerator Root { get; }
        public List<IEnumerator> Stack { get; } = [];
        public float Delay { get; set; }
        public bool IsStopped { get; set; }

        public CoroutineState(IEnumerator root, float delay)
        {
            Root = root;
            Delay = delay;
            Stack.Add(root);
        }
    }

    private static readonly Lazy<CoroutineManager> _instance =
        new(() => new CoroutineManager());

    private readonly List<CoroutineState> _running = [];
    private bool _isUpdating;

    /// <summary>
    /// Gets the shared coroutine manager used by the engine.
    /// </summary>
    public static CoroutineManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of root coroutines currently running.
    /// </summary>
    /// <remarks>
    /// Nested child routines belong to their root coroutine and are not counted as
    /// separate manager entries.
    /// </remarks>
    public int Count
    {
        get
        {
            int count = 0;

            for (int i = 0; i < _running.Count; i++)
            {
                if (!_running[i].IsStopped)
                    count++;
            }

            return count;
        }
    }

    private CoroutineManager() { }

    /// <summary>
    /// Schedules a coroutine with an initial delay.
    /// </summary>
    /// <param name="delay">
    /// The initial delay in scaled seconds. Values less than or equal to zero make
    /// the coroutine eligible to advance on the next manager update.
    /// </param>
    /// <param name="routine">The root coroutine enumerator to run.</param>
    /// <returns>A handle for querying, waiting for, or stopping the coroutine.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="routine"/> is <see langword="null"/>.
    /// </exception>
    public CoroutineHandle Run(float delay, IEnumerator routine)
    {
        ArgumentNullException.ThrowIfNull(routine);

        var state = new CoroutineState(routine, delay);
        _running.Add(state);

        Logger.Instance.DebugWithCategory(
            "Coroutine",
            "Starting coroutine (delay: {0}s, total running: {1})",
            delay,
            Count);

        return new CoroutineHandle(this, routine);
    }

    /// <summary>
    /// Schedules a coroutine without an initial timed delay.
    /// </summary>
    /// <param name="routine">The root coroutine enumerator to run.</param>
    /// <returns>A handle for querying, waiting for, or stopping the coroutine.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="routine"/> is <see langword="null"/>.
    /// </exception>
    public CoroutineHandle Run(IEnumerator routine)
        => Run(0f, routine);

    /// <summary>
    /// Stops a root coroutine and its currently active child chain.
    /// </summary>
    /// <param name="routine">The root coroutine enumerator to stop.</param>
    /// <returns>
    /// <see langword="true"/> when the coroutine was found and stopped;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Active nested routines that implement <see cref="IDisposable"/> are disposed
    /// from the deepest child back to the root.
    /// </remarks>
    public bool Stop(IEnumerator routine)
    {
        if (routine == null)
            return false;

        CoroutineState state = FindState(routine);

        if (state == null || state.IsStopped)
            return false;

        StopState(state);

        if (!_isUpdating)
            RemoveStopped();

        return true;
    }

    /// <summary>
    /// Stops the coroutine represented by a handle.
    /// </summary>
    /// <param name="routine">The coroutine handle to stop.</param>
    /// <returns>
    /// <see langword="true"/> when the handle referred to a running coroutine and
    /// it was stopped; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Stop(CoroutineHandle routine)
        => routine.Enumerator != null && Stop(routine.Enumerator);

    /// <summary>
    /// Stops and removes all coroutines currently tracked by the manager.
    /// </summary>
    /// <remarks>
    /// Every active child chain is disposed from the deepest child back to its root.
    /// </remarks>
    public void StopAll()
    {
        int count = Count;

        Logger.Instance.InfoWithCategory(
            "Coroutine",
            "Stopping all coroutines ({0} running)",
            count);

        for (int i = 0; i < _running.Count; i++)
        {
            if (!_running[i].IsStopped)
                StopState(_running[i]);
        }

        if (!_isUpdating)
            RemoveStopped();
    }

    /// <summary>
    /// Determines whether a root coroutine enumerator is currently running.
    /// </summary>
    /// <param name="routine">The root coroutine enumerator to check.</param>
    /// <returns>
    /// <see langword="true"/> when the enumerator is registered and active;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsRunning(IEnumerator routine)
    {
        if (routine == null)
            return false;

        CoroutineState state = FindState(routine);
        return state != null && !state.IsStopped;
    }

    /// <summary>
    /// Determines whether a coroutine handle currently refers to a running coroutine.
    /// </summary>
    /// <param name="routine">The coroutine handle to check.</param>
    /// <returns>
    /// <see langword="true"/> when the coroutine is running; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool IsRunning(CoroutineHandle routine)
        => routine.Enumerator != null && IsRunning(routine.Enumerator);

    /// <summary>
    /// Advances all active coroutines using the current VOID timing state.
    /// </summary>
    /// <param name="frameTime">Timing information for the current update.</param>
    internal void Update(FrameTime frameTime)
    {
        ArgumentNullException.ThrowIfNull(frameTime);

        int updateCount = _running.Count;
        _isUpdating = true;

        try
        {
            for (int i = 0; i < updateCount; i++)
            {
                CoroutineState state = _running[i];

                if (state.IsStopped)
                    continue;

                if (state.Delay > 0f)
                {
                    state.Delay -= frameTime.DeltaTime;
                    continue;
                }

                try
                {
                    if (!Advance(state))
                        state.IsStopped = true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.ErrorWithCategory(
                        "Coroutine",
                        ex,
                        "Coroutine failed");

                    StopState(state);
                }
            }
        }
        finally
        {
            _isUpdating = false;
            RemoveStopped();
        }
    }

    private CoroutineState FindState(IEnumerator routine)
    {
        for (int i = 0; i < _running.Count; i++)
        {
            if (ReferenceEquals(_running[i].Root, routine))
                return _running[i];
        }

        return null;
    }

    private static bool Advance(CoroutineState state)
    {
        while (state.Stack.Count > 0)
        {
            if (state.IsStopped)
                return false;

            int index = state.Stack.Count - 1;
            IEnumerator routine = state.Stack[index];

            if (!routine.MoveNext())
            {
                DisposeRoutine(routine);
                state.Stack.RemoveAt(index);

                if (state.Stack.Count == 0)
                    return false;

                // A child completed. Resume its parent during this same manager
                // update, matching normal nested-coroutine chaining semantics.
                continue;
            }

            if (state.IsStopped)
                return false;

            object current = routine.Current;

            if (current is IEnumerator child)
            {
                if (ContainsReference(state.Stack, child))
                {
                    throw new InvalidOperationException(
                        "A coroutine cannot yield itself or another active coroutine in its own child chain.");
                }

                state.Stack.Add(child);

                // The child becomes active now but does not advance until the next
                // manager update. This preserves normal yield-to-child behavior.
                return true;
            }

            state.Delay = current switch
            {
                float value => value,
                double value => (float)value,
                int value => value,
                _ => 0f
            };

            return true;
        }

        return false;
    }

    private static bool ContainsReference(List<IEnumerator> stack, IEnumerator routine)
    {
        for (int i = 0; i < stack.Count; i++)
        {
            if (ReferenceEquals(stack[i], routine))
                return true;
        }

        return false;
    }

    private static void StopState(CoroutineState state)
    {
        if (state.IsStopped)
            return;

        state.IsStopped = true;
        state.Delay = 0f;

        for (int i = state.Stack.Count - 1; i >= 0; i--)
            DisposeRoutine(state.Stack[i]);

        state.Stack.Clear();
    }

    private static void DisposeRoutine(IEnumerator routine)
    {
        if (routine is not IDisposable disposable)
            return;

        try
        {
            disposable.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Instance.ErrorWithCategory(
                "Coroutine",
                ex,
                "Coroutine disposal failed");
        }
    }

    private void RemoveStopped()
    {
        for (int i = _running.Count - 1; i >= 0; i--)
        {
            if (_running[i].IsStopped)
                _running.RemoveAt(i);
        }
    }
}
