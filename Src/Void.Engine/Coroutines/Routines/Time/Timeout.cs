using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Void.Engine.Coroutines.Routines.Time;

/// <summary>
/// A coroutine that wraps another routine and completes after a specified timeout.
/// </summary>
public sealed class Timeout : IEnumerator, IDisposable
{
    private readonly IEnumerator _inner;
    private readonly float _timeout;
    private float _elapsed;

    public object Current => _inner?.Current;
    public Timeout(IEnumerator inner, float timeout)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeout = timeout;
    }

    public bool MoveNext()
    {
        if (_timeout >= 0f)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            if (_elapsed >= _timeout)
                return false;
        }

        return _inner.MoveNext();
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() => (_inner as IDisposable)?.Dispose();
}
