/*
    MIT License

    Copyright (c) 2017 Chevy Ray Johnston

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

// ============================================================================
//  CoroutineManager.cs
// ============================================================================
//  Frame-driven coroutine runner with delays, nesting, and cancellation.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Coroutines;

/// <summary>
/// Runs frame-driven <see cref="IEnumerator"/> coroutines.
/// </summary>
/// <remarks>
/// <para>
/// Coroutines are advanced by VOID's game loop. Yielding a <see cref="float"/>,
/// <see cref="double"/>, or <see cref="int"/> pauses the coroutine for that many
/// seconds. Yielding another <see cref="IEnumerator"/> runs it as a nested
/// coroutine before the parent continues. Other yielded values, including
/// <see langword="null"/>, resume on a later manager update without adding a
/// timed delay.
/// </para>
/// <para>
/// The manager is intended to be used from the game thread. Its internal lists
/// are not synchronized for concurrent access.
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
    private static readonly Lazy<CoroutineManager> _instance =
       new(() => new CoroutineManager());
    private readonly List<IEnumerator> _running = [];
    private readonly List<float> _delays = [];

    /// <summary>
    /// Gets the shared coroutine manager used by the engine.
    /// </summary>
    public static CoroutineManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of coroutine entries currently tracked by the manager.
    /// </summary>
    /// <remarks>
    /// A coroutine stopped individually remains as an empty entry until the next
    /// manager update, so this value can temporarily include a coroutine for which
    /// <see cref="IsRunning(CoroutineHandle)"/> returns <see langword="false"/>.
    /// </remarks>
    public int Count => _running.Count;

    private CoroutineManager() { }

    /// <summary>
    /// Schedules a coroutine with an initial delay.
    /// </summary>
    /// <param name="delay">
    /// The initial delay in seconds. Values less than or equal to zero make the
    /// coroutine eligible to advance on the next manager update.
    /// </param>
    /// <param name="routine">The coroutine enumerator to run.</param>
    /// <returns>A handle for querying, waiting for, or stopping the coroutine.</returns>
    public CoroutineHandle Run(float delay, IEnumerator routine)
    {
        Logger.Instance.DebugWithCategory("Coroutine",
            "Starting coroutine (delay: {0}s, total running: {1})", delay, _running.Count + 1);

        _running.Add(routine);
        _delays.Add(delay);

        return new CoroutineHandle(this, routine);
    }

    /// <summary>
    /// Schedules a coroutine without an initial timed delay.
    /// </summary>
    /// <param name="routine">The coroutine enumerator to run.</param>
    /// <returns>A handle for querying, waiting for, or stopping the coroutine.</returns>
    public CoroutineHandle Run(IEnumerator routine) => Run(0f, routine);

    /// <summary>
    /// Stops a coroutine registered with this manager.
    /// </summary>
    /// <param name="routine">The root coroutine enumerator to stop.</param>
    /// <returns>
    /// <see langword="true"/> when the coroutine was found and stopped;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If the root enumerator implements <see cref="IDisposable"/>, it is disposed
    /// when stopped.
    /// </remarks>
    public bool Stop(IEnumerator routine)
    {
        int i = _running.IndexOf(routine);

        if (i < 0)
            return false;

        if (_running[i] is IDisposable disposable)
            disposable.Dispose();

        _running[i] = null;
        _delays[i] = 0f;

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
    {
        if (!routine.IsRunning)
            return false;

        return Stop(routine.Enumerator);
    }

    /// <summary>
    /// Stops and removes all coroutines currently tracked by the manager.
    /// </summary>
    /// <remarks>
    /// Root enumerators that implement <see cref="IDisposable"/> are disposed before
    /// the manager clears its coroutine state.
    /// </remarks>
    public void StopAll()
    {
        Logger.Instance.InfoWithCategory("Coroutine",
            "Stopping all coroutines ({0} running)", _running.Count);

        foreach (var routine in _running)
        {
            if (routine is IDisposable disposable)
                disposable.Dispose();
        }

        _running.Clear();
        _delays.Clear();
    }

    /// <summary>
    /// Determines whether a root coroutine enumerator is currently running.
    /// </summary>
    /// <param name="routine">The coroutine enumerator to check.</param>
    /// <returns>
    /// <see langword="true"/> when the enumerator is registered as running;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsRunning(IEnumerator routine) => _running.Contains(routine);

    /// <summary>
    /// Determines whether a coroutine handle currently refers to a running coroutine.
    /// </summary>
    /// <param name="routine">The coroutine handle to check.</param>
    /// <returns>
    /// <see langword="true"/> when the coroutine is running; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool IsRunning(CoroutineHandle routine) => routine.IsRunning;

    internal void Update(float frameTime)
    {
        for (int i = 0; i < _running.Count; i++)
        {
            if (_delays[i] > 0f)
                _delays[i] -= frameTime;
            else
            {
                try
                {
                    if (_running[i] == null || !MoveNext(_running[i], i))
                    {
                        if (_running[i] is IDisposable disposable)
                            disposable.Dispose();
                        _running.RemoveAt(i);
                        _delays.RemoveAt(i--);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.ErrorWithCategory("Coroutine", ex, "Coroutine failed");

                    if (_running[i] is IDisposable disposable)
                        disposable.Dispose();
                    _running.RemoveAt(i);
                    _delays.RemoveAt(i--);
                }
            }
        }
    }

    private bool MoveNext(IEnumerator routine, int index)
    {
        if (routine.Current is IEnumerator enumerator)
        {
            if (MoveNext(enumerator, index))
                return true;

            _delays[index] = 0f;
            if (!routine.MoveNext())
                return false;

            SetDelays(routine, index);
            return true;
        }

        if (!routine.MoveNext())
            return false;

        SetDelays(routine, index);
        return true;
    }

    private void SetDelays(IEnumerator routine, int index)
    {
        if (routine.Current is float f)
            _delays[index] = f;
        else if (routine.Current is double d)
            _delays[index] = (float)d;
        else if (routine.Current is int i)
            _delays[index] = i;
        else
            _delays[index] = 0;
    }
}
