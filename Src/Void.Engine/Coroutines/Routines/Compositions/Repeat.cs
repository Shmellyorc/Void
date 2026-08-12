namespace Void.Engine.Coroutines.Routines.Compositions;

public sealed class Repeat : IEnumerator
{
    private readonly Func<IEnumerator> _factory;
    private IEnumerator _current;
    private int _count;

    public object Current => _current?.Current;

    public Repeat(Func<IEnumerator> factory, int count = -1)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _count = count;
        _current = _factory();
    }

    public bool MoveNext()
    {
        if (_current == null)
            return false;

        if (_current.MoveNext())
            return true;

        // Current routine finished
        if (_count > 0)
        {
            _count--;
            if (_count == 0)
                return false;
        }

        _current = _factory();
        return _current.MoveNext();
    }

    public void Reset() => throw new NotSupportedException();
}