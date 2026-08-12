namespace Void.Engine.Coroutines.Routines.Animations;

public sealed class PingPongTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;
    private bool _reverse;

    public object Current => null;

    public PingPongTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
    }

    public bool MoveNext()
    {
        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            if (_reverse)
                normalized = 1f - normalized;

            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        if (!_reverse)
        {
            _reverse = true;
            _elapsed = 0f;
            return true;
        }

        _onUpdate?.Invoke(_from);
        return false;
    }

    public void Reset() => throw new NotSupportedException();
}
