namespace Void.Engine.Coroutines.Routines.Utilities;

public sealed class WaitForAny : IEnumerator, IDisposable
{
    private readonly IEnumerator[] _routines;
    private bool _completed;

    public object Current
    {
        get
        {
            foreach (var r in _routines)
            {
                if (r != null)
                    return r.Current;
            }
            return null;
        }
    }

    public WaitForAny(params IEnumerator[] routines)
        => _routines = routines?.Where(r => r != null).ToArray() ?? [];

    public bool MoveNext()
    {
        if (_completed) return false;

        foreach (var r in _routines)
        {
            if (r != null && !r.MoveNext())
            {
                _completed = true;
                return false;
            }
        }

        return true;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose()
    {
        foreach (var r in _routines)
            (r as IDisposable)?.Dispose();
    }
}
