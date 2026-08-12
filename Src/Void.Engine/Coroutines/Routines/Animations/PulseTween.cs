namespace Void.Engine.Coroutines.Routines.Animations;

public sealed class PulseTween<T> : IEnumerator
{
    private readonly T _a, _b;
    private readonly float _durationPerCycle;
    private readonly int _cycles;
    private readonly EaseType _type;
    private readonly Func<T, T, float, T> _lerp;
    private readonly Action<T> _onUpdate;
    private float _elapsed;
    private int _completedCycles;

    public object Current => null;

    public PulseTween(T a, T b, float durationPerCycle, EaseType type, Func<T, T, float, T> lerpFunc, Action<T> onUpdate, int cycles = -1)
    {
        _a = a;
        _b = b;
        _durationPerCycle = durationPerCycle;
        _cycles = cycles;
        _type = type;
        _lerp = lerpFunc;
        _onUpdate = onUpdate;
        _elapsed = 0f;
        _completedCycles = 0;
    }

    public bool MoveNext()
    {
        if (_cycles != -1 && _completedCycles >= _cycles)
        {
            _onUpdate?.Invoke(_b);
            return false;
        }

        float halfDuration = _durationPerCycle / 2f;

        if (_elapsed < _durationPerCycle)
        {
            float normalized;
            T currentValue;

            if (_elapsed < halfDuration)
            {
                normalized = _elapsed / halfDuration;  // 0→1
                float eased = Easing.Ease(_type, normalized);
                currentValue = _lerp(_a, _b, eased);
            }
            else
            {
                normalized = (_elapsed - halfDuration) / halfDuration;  // 0→1
                float eased = Easing.Ease(_type, normalized);
                currentValue = _lerp(_b, _a, eased);
            }

            _onUpdate?.Invoke(currentValue);
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            return true;
        }

        _completedCycles++;
        _elapsed = 0f;

        return true;
    }

    public void Reset() => throw new NotSupportedException();
}