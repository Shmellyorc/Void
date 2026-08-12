using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Void.Engine.Coroutines.Routines.Conditionals;

/// <summary>
/// A coroutine that waits while a getter returns a non-null value.
/// Completes once the getter returns null.
/// </summary>
/// <typeparam name="T">The type of value to check. Must be a class.</typeparam>
public sealed class WaitWhileNotNull<T> : IEnumerator where T : class
{
    private readonly Func<T> _getter;
    private T _value;

    public object Current => null;
    public T Value => _value;

    public WaitWhileNotNull(Func<T> getter)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }

    public bool MoveNext()
    {
        _value = _getter();
        return _value != null;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}
