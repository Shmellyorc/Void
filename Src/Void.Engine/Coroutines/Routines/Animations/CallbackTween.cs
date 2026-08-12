using Snap.Engine.Tweens;

namespace Void.Engine.Coroutines.Routines.Animations;

public sealed class CallbackTween<T> : IEnumerator
{
    private readonly Tween<T> _inner;
    private readonly Action _onComplete;

    public object Current => null;

    public CallbackTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, Action onComplete)
    {
        _inner = new Tween<T>(from, to, duration, type, lerpFunc, onUpdate);
        _onComplete = onComplete;
    }

    public bool MoveNext()
    {
        bool running = _inner.MoveNext();
        if (!running)
            _onComplete?.Invoke();
        return running;
    }

    public void Reset() => throw new NotSupportedException();
}