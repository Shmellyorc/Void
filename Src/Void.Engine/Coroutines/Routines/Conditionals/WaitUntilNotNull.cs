namespace Void.Engine.Coroutines.Routines.Conditionals;

public sealed class WaitUntilNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    public object Current => null;
    public T Value => _value;

    public WaitUntilNotNull(Func<T> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    public bool MoveNext()
    {
        _value = _getter();
        return _value == null;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}