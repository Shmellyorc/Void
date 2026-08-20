namespace Void.Engine.Coroutines.Routines.Time;

public sealed class WaitForSignal : IEnumerator
{
    private bool _signaled;

    public object Current => null;

    public void Signal() => _signaled = true;

    public bool MoveNext() => !_signaled;

    public void Reset() => _signaled = false;

    public void Dispose() { }
}
