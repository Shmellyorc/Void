namespace Void.Engine.Coroutines.Routines.Time;

public sealed class WaitForSecondsRealtime : IEnumerator
{
    private float _remaining;

    public object Current => null;

    public WaitForSecondsRealtime(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.UnscaledDeltaTime;
        return _remaining > 0f;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}