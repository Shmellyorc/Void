namespace Void.Engine.Coroutines.Routines.Utilities;

public sealed class DoOnce : IEnumerator
{
    private readonly Action _action;
    private bool _done;

    public object Current => null;

    public DoOnce(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public bool MoveNext()
    {
        if (!_done)
        {
            _action();
            _done = true;
        }
        return false;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}