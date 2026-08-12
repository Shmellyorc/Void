namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits until a given condition becomes true.
/// </summary>
public sealed class WaitUntil : IEnumerator
{
    private readonly Func<bool> _predicate;

    public object Current => null;

    public WaitUntil(Func<bool> predicate) => _predicate = predicate;

    public bool MoveNext() => !_predicate();
    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
