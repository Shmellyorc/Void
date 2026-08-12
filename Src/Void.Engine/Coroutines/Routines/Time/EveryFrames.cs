namespace Void.Engine.Coroutines.Routines.Time;

public sealed class EveryFrames : IEnumerator
{
    private readonly int _interval;
    private readonly Action _action;
    private int _elapsed;

    public object Current => null;

    public EveryFrames(int interval, Action action)
    {
        _interval = Math.Max(1, interval);
        _action = action;
        _elapsed = 0;
    }

    public bool MoveNext()
    {
        _elapsed++;
        if (_elapsed >= _interval)
        {
            _elapsed = 0;
            _action?.Invoke();
        }
        return true;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
