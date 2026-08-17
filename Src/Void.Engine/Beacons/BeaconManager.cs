namespace Void.Engine.Beacons;

public sealed class BeaconManager
{
    private readonly ConcurrentDictionary<ulong, Action<BeaconHandle>> _topics = [];

    public static BeaconManager Instance { get; private set; }
    public int Count => _topics.Count;

    internal BeaconManager() => Instance ??= this;

    public void Subscribe(string topic, Action<BeaconHandle> handle)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");
        if (handle == null)
            throw new ArgumentNullException(nameof(handle), "handle is null");

        var hash = HashHelper.Cache64(topic);
        _topics.AddOrUpdate(
            hash,
            handle,
            (k, existing) => (Action<BeaconHandle>)Delegate.Combine(existing, handle)
        );
    }
    public void Subscribe(Enum topic, Action<BeaconHandle> handle)
        => Subscribe(topic.ToEnumString(), handle);

    public bool Unsubscribe(string topic, Action<BeaconHandle> handle)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");
        if (handle == null)
            throw new ArgumentNullException(nameof(handle), "handle is null");

        var hash = HashHelper.Cache64(topic);
        if (!_topics.TryGetValue(hash, out var handles))
            return false;

        var newHandler = (Action<BeaconHandle>)Delegate.Remove(handles, handle);

        if (newHandler == null)
            return _topics.TryRemove(hash, out _);

        _topics[hash] = newHandler;
        return true;
    }
    public void Unsubscribe(Enum topic, Action<BeaconHandle> handle)
        => Unsubscribe(topic.ToEnumString(), handle);

    public void Publish(string topic, params object[] data)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");

        var hash = HashHelper.Cache64(topic);

        if (!_topics.TryGetValue(hash, out var handles))
            return;

        BeaconHandle handle;

        if (data.IsEmpty())
            handle = new BeaconHandle(topic, Array.Empty<object>());
        else
            handle = new BeaconHandle(topic, data);

        handles.Invoke(handle);
    }
    public void Publish(Enum topic, params object[] data)
        => Publish(topic.ToEnumString(), data);

    public void Clear() => _topics.Clear();
}
