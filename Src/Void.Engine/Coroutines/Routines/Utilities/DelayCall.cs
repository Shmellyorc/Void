namespace Void.Engine.Coroutines.Routines.Utilities;

public class DelayCall : IEnumerator
{
    private readonly float _delay;
    private readonly Action _callback;
    private float _elapsed;

    public object Current => null;

    public DelayCall(float delay, Action callback)
    {
        _delay = delay;
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _elapsed = 0f;
    }

    public bool MoveNext()
    {
        if (_elapsed < _delay)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        _callback();
        return false;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
