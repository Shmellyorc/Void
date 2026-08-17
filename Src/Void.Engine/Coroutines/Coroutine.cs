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

using Void.Engine.Logs;

namespace Void.Engine.Coroutines;

public sealed class CoroutineManager
{
    private readonly List<IEnumerator> _running = [];
    private readonly List<float> _delays = [];

    public static CoroutineManager Instance { get; private set; }
    public int Count => _running.Count;

    internal CoroutineManager() => Instance ??= this;

    public CoroutineHandle Run(float delay, IEnumerator routine)
    {
        Logger.Instance.DebugWithCategory("Coroutine", 
            "Starting coroutine (delay: {0}s, total running: {1})", delay, _running.Count + 1);

        _running.Add(routine);
        _delays.Add(delay);

        return new CoroutineHandle(this, routine);
    }

    public CoroutineHandle Run(IEnumerator routine) => Run(0f, routine);

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

    public bool Stop(CoroutineHandle routine)
    {
        if (!routine.IsRunning)
            return false;

        return Stop(routine.Enumerator);
    }

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

    public bool IsRunning(IEnumerator routine) => _running.Contains(routine);

    public bool IsRunning(CoroutineHandle routine) => routine.IsRunning;

    internal void Update(FrameTime frameTime)
    {
        for (int i = 0; i < _running.Count; i++)
        {
            if (_delays[i] > 0f)
                _delays[i] -= frameTime.DeltaTime;
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
        }

        bool result = routine.MoveNext();

        if (routine.Current is float fValue)
            _delays[index] = fValue;
        else if (routine.Current is double dValue)
            _delays[index] = (float)dValue;
        else if (routine.Current is int iValue)
            _delays[index] = iValue;

        return result;
    }
}
