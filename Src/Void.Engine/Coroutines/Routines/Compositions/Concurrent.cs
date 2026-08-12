namespace Void.Engine.Coroutines.Routines.Compositions;

public class Concurrent : IEnumerator
{
    private readonly List<IEnumerator> _active;

    public Concurrent(params IEnumerator[] routines)
    {
        _active = routines?.Where(r => r != null).ToList() ?? [];
    }

    public object Current => null;

    public bool MoveNext()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (!_active[i].MoveNext())
                _active.RemoveAt(i);
        }

        return _active.Count > 0;
    }

    public void Reset() => throw new NotSupportedException();
}