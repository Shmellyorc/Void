namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that waits until it is manually signaled.
/// </summary>
public sealed class WaitForSignal : IEnumerator
{
    private bool _signaled;

    public object Current => null;

    public void Signal() => _signaled = true;

    public bool MoveNext() => !_signaled;

    public void Reset() => _signaled = false;

    public void Dispose() { }
}
