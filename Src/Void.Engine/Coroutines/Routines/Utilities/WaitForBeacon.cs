namespace Void.Engine.Coroutines.Routines.Utilities;

public sealed class WaitForBeacon : IEnumerator, IDisposable
{
    private readonly string _topic;
    private readonly Func<BeaconHandle, bool> _predicate;
    private readonly float _timeoutSeconds;
    private readonly Action<BeaconHandle> _handler;
    private bool _subscribed;
    private bool _done;
    private float _elapsed;

    public BeaconHandle? Result { get; private set; }

    public object Current => null;

    public WaitForBeacon(string topic, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic must be non-empty.", nameof(topic));

        _topic = topic;
        _predicate = predicate;
        _timeoutSeconds = timeoutSeconds;
        _handler = OnBeacon;
    }

    public WaitForBeacon(Enum topic, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
        : this(topic.ToEnumString(), predicate, timeoutSeconds) { }

    public bool MoveNext()
    {
        if (_done)
            return false;

        if (!_subscribed)
        {
            BeaconManager.Instance.Subscribe(_topic, _handler);
            _subscribed = true;
        }

        if (_done)
            return false;

        if (_timeoutSeconds >= 0f)
        {
            _elapsed += Game.Instance.FrameTime.DeltaTime;
            if (_elapsed >= _timeoutSeconds)
            {
                Cleanup();
                Result = null; // timed out
                _done = true;
                return false;
            }
        }

        // Still waiting
        return true;
    }

    public void Reset() => throw new NotSupportedException();

    public void Dispose() => Cleanup();

    private void OnBeacon(BeaconHandle handle)
    {
        if (_done) return;

        if (_predicate == null || _predicate(handle))
        {
            Result = handle;
            Cleanup();
            _done = true;
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
