namespace Void.Engine.Coroutines.Routines.Utilities;

public sealed class WaitForBeaconCount : IEnumerator, IDisposable
{
    private readonly string _topic;
    private readonly int _targetCount;
    private readonly Func<BeaconHandle, bool> _predicate;
    private readonly float _timeoutSeconds;
    private readonly Action<BeaconHandle> _handler;
    private int _count;
    private bool _subscribed;
    private bool _done;
    private float _elapsed;

    public object Current => null;
    public BeaconHandle? LastBeacon { get; private set; }

    public WaitForBeaconCount(string topic, int count, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic must be non-empty.", nameof(topic));
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

        _topic = topic;
        _targetCount = count;
        _predicate = predicate;
        _timeoutSeconds = timeoutSeconds;
        _handler = OnBeacon;
    }

    public WaitForBeaconCount(Enum topic, int count, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
        : this(topic.ToEnumString(), count, predicate, timeoutSeconds) { }

    public bool MoveNext()
    {
        if (_done) return false;

        if (!_subscribed)
        {
            BeaconManager.Instance.Subscribe(_topic, _handler);
            _subscribed = true;
        }

        if (_done) return false;

        if (_timeoutSeconds >= 0f)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            if (_elapsed >= _timeoutSeconds)
            {
                Cleanup();
                _done = true;
                return false;
            }
        }

        return true;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() => Cleanup();

    private void OnBeacon(BeaconHandle h)
    {
        if (_done) return;

        if (_predicate == null || _predicate(h))
        {
            LastBeacon = h;
            _count++;
            if (_count >= _targetCount)
            {
                Cleanup();
                _done = true;
            }
        }
    }

    private void Cleanup()
    {
        if (_subscribed)
        {
            try { BeaconManager.Instance.Unsubscribe(_topic, _handler); }
            catch { /* ignore */ }
            _subscribed = false;
        }
    }
}