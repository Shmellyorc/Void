namespace Void.Engine.Coroutines.Routines.Animations;

public sealed class LoopTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private readonly int _maxLoops;  // -1 = infinite
    private float _elapsed;
    private int _currentLoop;

    public object Current => null;

    public LoopTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, int loops = -1)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
        _maxLoops = loops;
    }

    public bool MoveNext()
    {
        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        _currentLoop++;

        if (_maxLoops == -1 || _currentLoop < _maxLoops)
        {
            _elapsed = 0f;
            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    public void Reset() => throw new NotSupportedException();
}
