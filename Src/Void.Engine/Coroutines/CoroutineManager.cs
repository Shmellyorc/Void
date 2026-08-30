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
//  Manages coroutine execution with support for delays, nested coroutines,
//  and cancellation. Coroutines are IEnumerators that can yield float values
//  for delays or nested IEnumerators for complex sequencing.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Coroutines;

/// <summary>
/// Manages coroutine execution with support for delays, nested coroutines,
/// and cancellation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CoroutineManager"/> class provides a coroutine system where
/// coroutines are implemented as <see cref="IEnumerator"/> methods. Coroutines
/// can yield:
/// <list type="bullet">
///   <item><description><see cref="float"/> - A delay in seconds</description></item>
///   <item><description><see cref="double"/> - A delay in seconds</description></item>
///   <item><description><see cref="int"/> - A delay in seconds</description></item>
///   <item><description><see cref="IEnumerator"/> - A nested coroutine to execute</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Coroutine execution with optional delays</description></item>
///   <item><description>Nested coroutine support</description></item>
///   <item><description>Coroutine cancellation by reference or handle</description></item>
///   <item><description>Stop all coroutines</description></item>
///   <item><description>Automatic cleanup of completed or failed coroutines</description></item>
///   <item><description>Thread-safe singleton access</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Define a coroutine
/// IEnumerator MyCoroutine()
/// {
///     // Wait 1 second
///     yield return 1.0f;
///     
///     // Do something
///     Console.WriteLine("After 1 second");
///     
///     // Wait 0.5 seconds
///     yield return 0.5f;
///     
///     // Nest another coroutine
///     yield return AnotherCoroutine();
///     
///     Console.WriteLine("Done!");
/// }
/// 
/// // Start a coroutine
/// var handle = CoroutineManager.Instance.Run(MyCoroutine());
/// 
/// // Start with an initial delay
/// var handle2 = CoroutineManager.Instance.Run(0.5f, MyCoroutine());
/// 
/// // Stop a coroutine
/// CoroutineManager.Instance.Stop(handle);
/// 
/// // Stop all coroutines
/// CoroutineManager.Instance.StopAll();
/// 
/// // Check if a coroutine is running
/// bool running = CoroutineManager.Instance.IsRunning(handle);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed from
/// the main thread.
/// </para>
/// </remarks>
public sealed class CoroutineManager
{
    private static readonly Lazy<CoroutineManager> _instance =
       new(() => new CoroutineManager());
    private readonly List<IEnumerator> _running = [];
    private readonly List<float> _delays = [];

    /// <summary>
    /// Gets the singleton instance of the coroutine manager.
    /// </summary>
    public static CoroutineManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of currently running coroutines.
    /// </summary>
    public int Count => _running.Count;

    private CoroutineManager() { }

    /// <summary>
    /// Starts a coroutine with the specified initial delay.
    /// </summary>
    /// <param name="delay">The initial delay in seconds before the coroutine starts.</param>
    /// <param name="routine">The coroutine to run.</param>
    /// <returns>A handle that can be used to track and stop the coroutine.</returns>
    public CoroutineHandle Run(float delay, IEnumerator routine)
    {
        Logger.Instance.DebugWithCategory("Coroutine",
            "Starting coroutine (delay: {0}s, total running: {1})", delay, _running.Count + 1);

        _running.Add(routine);
        _delays.Add(delay);

        return new CoroutineHandle(this, routine);
    }

    /// <summary>
    /// Starts a coroutine immediately.
    /// </summary>
    /// <param name="routine">The coroutine to run.</param>
    /// <returns>A handle that can be used to track and stop the coroutine.</returns>
    public CoroutineHandle Run(IEnumerator routine) => Run(0f, routine);

    /// <summary>
    /// Stops a running coroutine.
    /// </summary>
    /// <param name="routine">The coroutine to stop.</param>
    /// <returns><see langword="true"/> if the coroutine was found and stopped; otherwise, <see langword="false"/>.</returns>
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
    /// Stops a running coroutine using its handle.
    /// </summary>
    /// <param name="routine">The coroutine handle to stop.</param>
    /// <returns><see langword="true"/> if the coroutine was found and stopped; otherwise, <see langword="false"/>.</returns>
    public bool Stop(CoroutineHandle routine)
    {
        if (!routine.IsRunning)
            return false;

        return Stop(routine.Enumerator);
    }

    /// <summary>
    /// Stops all running coroutines.
    /// </summary>
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
    /// Determines whether a coroutine is currently running.
    /// </summary>
    /// <param name="routine">The coroutine to check.</param>
    /// <returns><see langword="true"/> if the coroutine is running; otherwise, <see langword="false"/>.</returns>
    public bool IsRunning(IEnumerator routine) => _running.Contains(routine);

    /// <summary>
    /// Determines whether a coroutine is currently running using its handle.
    /// </summary>
    /// <param name="routine">The coroutine handle to check.</param>
    /// <returns><see langword="true"/> if the coroutine is running; otherwise, <see langword="false"/>.</returns>
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