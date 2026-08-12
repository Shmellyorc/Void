global using Snap.Engine.Tweens;

namespace Void.Engine.Coroutines.Routines.Animations;

public sealed class SpeedTween<T> : IEnumerator
{
    private readonly T _from, _to;
    private readonly float _duration;
    private readonly float _speed;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;

    public object Current => null;

    public SpeedTween(T from, T to, float duration, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, float speed = 1f)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _speed = speed;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
    }

    public bool MoveNext()
    {
        if (_elapsed < _duration)
        {
            float normalized = _elapsed / _duration;
            float eased = Easing.Ease(_type, normalized);
            T value = _lerp(_from, _to, eased);
            _onUpdate?.Invoke(value);
            _elapsed += Game.Instance.FrameTime.DeltaTime * _speed;
            return true;
        }

        _onUpdate?.Invoke(_to);
        return false;
    }

    public void Reset() => throw new NotSupportedException();
}
