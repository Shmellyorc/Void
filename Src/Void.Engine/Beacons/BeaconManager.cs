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
using System.Collections.Generic;

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
/// Timed subscriptions use lazy expiration and do not add a per-frame update.
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
    private readonly struct Subscription
    {
        public Action<BeaconHandle> Handler { get; }
        public Action<BeaconHandle> WrappedHandler { get; }
        public TimeSpan ExpiresAt { get; }

        public Subscription(Action<BeaconHandle> handler, Action<BeaconHandle> wrappedHandler, TimeSpan expiresAt)
        {
            Handler = handler;
            WrappedHandler = wrappedHandler;
            ExpiresAt = expiresAt;
        }
    }

    private static readonly Lazy<BeaconManager> _instance =
        new Lazy<BeaconManager>(() => new BeaconManager());

    private readonly ConcurrentDictionary<ulong, Action<BeaconHandle>> _topics = [];
    private readonly Dictionary<ulong, List<Subscription>> _timedSubscriptions = [];
    private readonly object _subscriptionLock = new();

    /// <summary>
    /// Gets the shared beacon manager.
    /// </summary>
    public static BeaconManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of topics currently tracked as having subscribers.
    /// </summary>
    /// <remarks>
    /// Expired timed subscriptions are removed lazily when their topic is next
    /// published, subscribed to, or unsubscribed from.
    /// </remarks>
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
            CleanupExpiredLocked(hash);

            if (_topics.TryGetValue(hash, out var existing))
                _topics[hash] = existing + handle;
            else
                _topics[hash] = handle;
        }
    }

    /// <summary>
    /// Subscribes a callback to a string topic for a limited lifetime.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke while the subscription is active.</param>
    /// <param name="lifetime">The subscription lifetime in seconds.</param>
    /// <remarks>
    /// The lifetime is measured against <see cref="Void.Engine.Game.FrameTime"/>'s
    /// <see cref="Void.Engine.Systems.FrameTime.TotalTime"/> and expires lazily when the topic
    /// is next touched. Publishing after expiration does not invoke the callback.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="topic"/> is null or empty, or <paramref name="handle"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="lifetime"/> is not finite or is less than or equal to zero.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No game instance is available to provide frame timing.
    /// </exception>
    public void Subscribe(string topic, Action<BeaconHandle> handle, float lifetime)
    {
        if (float.IsNaN(lifetime) || float.IsInfinity(lifetime) || lifetime <= 0f)
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "lifetime must be a finite value greater than zero");

        SubscribeTimed(topic, handle, TimeSpan.FromSeconds(lifetime));
    }

    /// <summary>
    /// Subscribes a callback to an enum topic.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke when the topic is published.</param>
    public void Subscribe(Enum topic, Action<BeaconHandle> handle) 
        => Subscribe(topic.ToEnumString(), handle);

    /// <summary>
    /// Subscribes a callback to an enum topic for a limited lifetime.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke while the subscription is active.</param>
    /// <param name="lifetime">The subscription lifetime in seconds.</param>
    public void Subscribe(Enum topic, Action<BeaconHandle> handle, float lifetime) 
        => Subscribe(topic.ToEnumString(), handle, lifetime);

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
            CleanupExpiredLocked(hash);

            if (!_topics.TryGetValue(hash, out var handles))
                return false;

            var remaining = handles - handle;

            if (!ReferenceEquals(remaining, handles))
            {
                if (remaining == null)
                    return _topics.TryRemove(hash, out _);

                _topics[hash] = remaining;
                return true;
            }

            if (TryRemoveTimedSubscriptionLocked(hash, handle, out var wrappedHandler))
            {
                remaining = handles - wrappedHandler;

                if (remaining == null)
                    return _topics.TryRemove(hash, out _);

                _topics[hash] = remaining;
            }

            return true;
        }
    }

    /// <summary>
    /// Removes a callback from an enum topic.
    /// </summary>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to remove.</param>
    public void Unsubscribe(Enum topic, Action<BeaconHandle> handle) => Unsubscribe(topic.ToEnumString(), handle);

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
    public void Publish(Enum topic, params object[] data) => Publish(topic.ToEnumString(), data);

    /// <summary>
    /// Removes all beacon subscriptions.
    /// </summary>
    public void Clear()
    {
        lock (_subscriptionLock)
        {
            _topics.Clear();
            _timedSubscriptions.Clear();
        }
    }

    internal void SubscribeTimed(string topic, Action<BeaconHandle> handle, TimeSpan lifetime)
    {
        if (topic.IsEmpty())
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");
        if (handle == null)
            throw new ArgumentNullException(nameof(handle), "handle is null");
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "lifetime must be greater than zero");

        TimeSpan now = GetCurrentTime();
        TimeSpan expiresAt = AddLifetime(now, lifetime);
        ulong hash = HashHelper.Cache64(topic);

        Action<BeaconHandle> wrappedHandler = beacon =>
        {
            TimeSpan current = GetCurrentTime();

            if (current >= expiresAt)
            {
                CleanupExpired(hash, current);
                return;
            }

            handle(beacon);
        };

        lock (_subscriptionLock)
        {
            CleanupExpiredLocked(hash, now);

            if (_topics.TryGetValue(hash, out var existing))
                _topics[hash] = existing + wrappedHandler;
            else
                _topics[hash] = wrappedHandler;

            if (!_timedSubscriptions.TryGetValue(hash, out var subscriptions))
            {
                subscriptions = [];
                _timedSubscriptions[hash] = subscriptions;
            }

            subscriptions.Add(new Subscription(
                handle,
                wrappedHandler,
                expiresAt));
        }
    }

    private static TimeSpan GetCurrentTime()
    {
        if (Game.Instance == null)
            throw new InvalidOperationException(
                "Timed beacon subscriptions require an active Game instance");

        return Game.Instance.FrameTime.TotalTime;
    }

    private static TimeSpan AddLifetime(TimeSpan current, TimeSpan lifetime)
    {
        long remainingTicks = TimeSpan.MaxValue.Ticks - current.Ticks;

        if (lifetime.Ticks >= remainingTicks)
            return TimeSpan.MaxValue;

        return current + lifetime;
    }

    private void CleanupExpired(ulong hash, TimeSpan current)
    {
        lock (_subscriptionLock)
            CleanupExpiredLocked(hash, current);
    }

    private void CleanupExpiredLocked(ulong hash)
    {
        if (!_timedSubscriptions.ContainsKey(hash))
            return;

        CleanupExpiredLocked(hash, GetCurrentTime());
    }

    private void CleanupExpiredLocked(ulong hash, TimeSpan current)
    {
        if (!_timedSubscriptions.TryGetValue(hash, out var subscriptions))
            return;

        if (!_topics.TryGetValue(hash, out var handles))
        {
            _timedSubscriptions.Remove(hash);
            return;
        }

        bool changed = false;

        for (int i = subscriptions.Count - 1; i >= 0; i--)
        {
            var subscription = subscriptions[i];

            if (current < subscription.ExpiresAt)
                continue;

            handles -= subscription.WrappedHandler;
            subscriptions.RemoveAt(i);
            changed = true;
        }

        if (!changed)
            return;

        if (handles == null)
            _topics.TryRemove(hash, out _);
        else
            _topics[hash] = handles;

        if (subscriptions.Count == 0)
            _timedSubscriptions.Remove(hash);
    }

    private bool TryRemoveTimedSubscriptionLocked(ulong hash, Action<BeaconHandle> handle, out Action<BeaconHandle> wrappedHandler)
    {
        wrappedHandler = null;

        if (!_timedSubscriptions.TryGetValue(hash, out var subscriptions))
            return false;

        for (int i = subscriptions.Count - 1; i >= 0; i--)
        {
            var subscription = subscriptions[i];

            if (subscription.Handler != handle)
                continue;

            wrappedHandler = subscription.WrappedHandler;
            subscriptions.RemoveAt(i);

            if (subscriptions.Count == 0)
                _timedSubscriptions.Remove(hash);

            return true;
        }

        return false;
    }
}
