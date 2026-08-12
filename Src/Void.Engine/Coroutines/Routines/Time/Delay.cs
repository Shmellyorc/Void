namespace Void.Engine.Coroutines.Routines.Time;

public sealed class Delay : IEnumerator
{
    private readonly WaitForSeconds _wait;

    public object Current => null;

    public Delay(float seconds)
    {
        _wait = new WaitForSeconds(seconds);
    }

    public bool MoveNext() => _wait.MoveNext();
    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}