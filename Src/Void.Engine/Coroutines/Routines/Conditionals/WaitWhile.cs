namespace Void.Engine.Coroutines.Routines.Conditionals;

public sealed class WaitWhile : IEnumerator
{
    private readonly Func<bool> _predicate;

    public object Current => null;

    public WaitWhile(Func<bool> predicate) => _predicate = predicate;

    public bool MoveNext() => _predicate();
    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
