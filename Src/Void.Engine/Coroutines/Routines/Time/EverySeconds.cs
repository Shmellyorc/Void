namespace Void.Engine.Coroutines.Routines.Time;

public class EverySeconds : IEnumerator
{
    private readonly float _interval;
    private readonly Action _action;
    private float _elapsed;

    public object Current => null;

    public EverySeconds(float interval, Action action)
    {
        _interval = Math.Max(0f, interval);
        _action = action;
        _elapsed = 0f;
    }

    public bool MoveNext()
    {
        if (_interval <= 0f)
        {
            _action?.Invoke();
            return true;
        }

        _elapsed += Game.Instance.FrameTime.DeltaTime;
        if (_elapsed >= _interval)
        {
            _elapsed -= _interval;
            _action?.Invoke();
        }
        return true;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
