// ============================================================================
//  BeaconManager.cs
// ============================================================================
//  A lightweight publish/subscribe system for decoupled communication using
//  topic-based beacons.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Concurrent;

namespace Void.Engine.Beacons;

/// <summary>
/// A lightweight publish/subscribe system for decoupled communication using
/// topic-based beacons.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BeaconManager"/> provides a simple pub/sub system where
/// subscribers register callbacks for specific topics, and publishers send
/// beacons with optional data payloads.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Decoupled communication between systems</description></item>
///   <item><description>Event-driven architecture</description></item>
///   <item><description>Cross-system notifications without direct references</description></item>
///   <item><description>Plugin and mod communication</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Subscribe to a topic
/// BeaconManager.Instance.Subscribe("PlayerDied", handle =>
/// {
///     Console.WriteLine($"Player died with {handle.Count} data items");
///     var position = handle.Get&lt;Vect2&gt;(0);
/// });
/// 
/// // Subscribe with enum
/// BeaconManager.Instance.Subscribe(MyTopics.GameStarted, handle =>
/// {
///     Console.WriteLine("Game started!");
/// });
/// 
/// // Publish a beacon
/// BeaconManager.Instance.Publish("PlayerDied", playerPosition, playerHealth);
/// 
/// // Unsubscribe
/// BeaconManager.Instance.Unsubscribe("PlayerDied", handler);
/// 
/// // Clear all subscribers
/// BeaconManager.Instance.Clear();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe and uses concurrent collections for
/// subscriber management.
/// </para>
/// </remarks>
public sealed class BeaconManager
{
    private static readonly Lazy<BeaconManager> _instance =
        new Lazy<BeaconManager>(() => new BeaconManager());

    private readonly ConcurrentDictionary<ulong, Action<BeaconHandle>> _topics = [];

    /// <summary>
    /// Gets the singleton instance of the beacon manager.
    /// </summary>
    public static BeaconManager Instance => _instance.Value;

    /// <summary>
    /// Gets the number of subscribed topics.
    /// </summary>
    public int Count => _topics.Count;

    private BeaconManager() { }

    /// <summary>
    /// Subscribes to a beacon topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke when a beacon is published.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="topic"/> or <paramref name="handle"/> is null.</exception>
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

    /// <summary>
    /// Subscribes to a beacon topic using an enum.
    /// </summary>
    /// <param name="topic">The enum representing the topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke when a beacon is published.</param>
    public void Subscribe(Enum topic, Action<BeaconHandle> handle)
        => Subscribe(topic.ToEnumString(), handle);

    /// <summary>
    /// Unsubscribes from a beacon topic.
    /// </summary>
    /// <param name="topic">The topic to unsubscribe from.</param>
    /// <param name="handle">The callback to remove.</param>
    /// <returns><see langword="true"/> if the subscription was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="topic"/> or <paramref name="handle"/> is null.</exception>
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

    /// <summary>
    /// Unsubscribes from a beacon topic using an enum.
    /// </summary>
    /// <param name="topic">The enum representing the topic to unsubscribe from.</param>
    /// <param name="handle">The callback to remove.</param>
    public void Unsubscribe(Enum topic, Action<BeaconHandle> handle)
        => Unsubscribe(topic.ToEnumString(), handle);

    /// <summary>
    /// Publishes a beacon on a topic with optional data.
    /// </summary>
    /// <param name="topic">The topic to publish on.</param>
    /// <param name="data">Optional data payload to include with the beacon.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="topic"/> is null or empty.</exception>
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

    /// <summary>
    /// Publishes a beacon on a topic with optional data using an enum.
    /// </summary>
    /// <param name="topic">The enum representing the topic to publish on.</param>
    /// <param name="data">Optional data payload to include with the beacon.</param>
    public void Publish(Enum topic, params object[] data)
        => Publish(topic.ToEnumString(), data);

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    public void Clear() => _topics.Clear();
}