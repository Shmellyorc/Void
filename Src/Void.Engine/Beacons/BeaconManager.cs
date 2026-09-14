// ============================================================================
//  BeaconManager.cs
// ============================================================================
//  Topic-based publish/subscribe messaging for decoupled engine and game code.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Concurrent;

namespace Void.Engine.Beacons;

/// <summary>
/// Provides topic-based publish/subscribe messaging through a shared manager.
/// </summary>
/// <remarks>
/// <para>
/// Subscribers register an <see cref="Action{T}"/> for a string or enum topic.
/// Publishing that topic invokes its current subscribers synchronously on the
/// thread that calls <see cref="Publish(string, object[])"/>.
/// </para>
/// <para>
/// Multiple callbacks can subscribe to the same topic. Subscribers are stored as
/// a multicast <see cref="Action{T}"/>, allowing publishing to invoke the complete
/// subscriber chain directly without manually iterating subscribers.
/// </para>
/// <para>
/// Subscription changes are synchronized with each other. Publishing remains a
/// lock-free lookup and invokes the subscriber snapshot returned by that lookup.
/// </para>
/// <code>
/// void OnPlayerMoved(BeaconHandle beacon)
/// {
///     if (beacon.TryGet&lt;Vect2&gt;(0, out var position))
///         Console.WriteLine(position);
/// }
///
/// BeaconManager.Instance.Subscribe("PlayerMoved", OnPlayerMoved);
/// BeaconManager.Instance.Publish("PlayerMoved", new Vect2(32, 24));
/// BeaconManager.Instance.Unsubscribe("PlayerMoved", OnPlayerMoved);
/// </code>
/// </remarks>
public sealed class BeaconManager
{
    private static readonly Lazy<BeaconManager> _instance =
        new Lazy<BeaconManager>(() => new BeaconManager());

    private readonly ConcurrentDictionary<ulong, Action<BeaconHandle>> _topics = [];
    private readonly object _subscriptionLock = new();

    /// <summary>
    /// Gets the shared beacon manager.
    /// </summary>
    public static BeaconManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of topics that currently have subscribers.
    /// </summary>
    public int Count => _topics.Count;

    private BeaconManager() { }

    /// <summary>
    /// Subscribes a callback to a string topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke when the topic is published.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="topic"/> is null or empty, or <paramref name="handle"/> is null.
    /// </exception>
    public void Subscribe(string topic, Action<BeaconHandle> handle)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");
        if (handle == null)
            throw new ArgumentNullException(nameof(handle), "handle is null");

        ulong hash = HashHelper.Cache64(topic);

        lock (_subscriptionLock)
        {
            if (_topics.TryGetValue(hash, out var existing))
                _topics[hash] = existing + handle;
            else
                _topics[hash] = handle;
        }
    }

    /// <summary>
    /// Subscribes a callback to an enum topic.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke when the topic is published.</param>
    public void Subscribe(Enum topic, Action<BeaconHandle> handle)
        => Subscribe(topic.ToEnumString(), handle);

    /// <summary>
    /// Removes a callback from a string topic.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from.</param>
    /// <param name="handle">The callback to remove.</param>
    /// <returns>
    /// <see langword="true"/> when the topic existed and the subscription was
    /// updated or removed; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="topic"/> is null or empty, or <paramref name="handle"/> is null.
    /// </exception>
    public bool Unsubscribe(string topic, Action<BeaconHandle> handle)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");
        if (handle == null)
            throw new ArgumentNullException(nameof(handle), "handle is null");

        ulong hash = HashHelper.Cache64(topic);

        lock (_subscriptionLock)
        {
            if (!_topics.TryGetValue(hash, out var handles))
                return false;

            var remaining = handles - handle;

            if (remaining == null)
                return _topics.TryRemove(hash, out _);

            _topics[hash] = remaining;
            return true;
        }
    }

    /// <summary>
    /// Removes a callback from an enum topic.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to remove.</param>
    public void Unsubscribe(Enum topic, Action<BeaconHandle> handle)
        => Unsubscribe(topic.ToEnumString(), handle);

    /// <summary>
    /// Publishes a string topic with an optional payload.
    /// </summary>
    /// <param name="topic">The topic to publish.</param>
    /// <param name="data">The payload items delivered to subscribers.</param>
    /// <remarks>
    /// If the topic has no subscribers, the call returns without creating a
    /// <see cref="BeaconHandle"/>. Subscriber callbacks are invoked synchronously
    /// through the topic's multicast <see cref="Action{T}"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="topic"/> is null or empty.
    /// </exception>
    public void Publish(string topic, params object[] data)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");

        ulong hash = HashHelper.Cache64(topic);

        if (!_topics.TryGetValue(hash, out var handles))
            return;

        var handle = new BeaconHandle(
            topic,
            data.IsEmpty() ? Array.Empty<object>() : data);

        handles.Invoke(handle);
    }

    /// <summary>
    /// Publishes an enum topic with an optional payload.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="data">The payload items delivered to subscribers.</param>
    public void Publish(Enum topic, params object[] data)
        => Publish(topic.ToEnumString(), data);

    /// <summary>
    /// Removes all beacon subscriptions.
    /// </summary>
    public void Clear()
    {
        lock (_subscriptionLock)
            _topics.Clear();
    }
}
