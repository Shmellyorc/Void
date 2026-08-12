namespace Void.Engine.Coroutines.Routines.Time;

public sealed class WaitForSeconds : IEnumerator
{
    private float _remaining;

    public object Current => null;

    public WaitForSeconds(float seconds)
    {
        _remaining = Math.Max(0f, seconds);
    }

    public bool MoveNext()
    {
        _remaining -= Game.Instance.FrameTime.DeltaTime;
        return _remaining > 0f;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}