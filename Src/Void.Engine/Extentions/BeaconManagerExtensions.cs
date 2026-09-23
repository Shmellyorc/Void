// ============================================================================
//  BeaconManagerExtensions.cs
// ============================================================================
//  Convenience extensions for one-shot, TimeSpan, and delayed beacon behavior.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections;
using System.Threading;

using Void.Engine.Coroutines;

namespace Void.Engine.Beacons;

/// <summary>
/// Provides convenience extensions for <see cref="BeaconManager"/>.
/// </summary>
public static class BeaconManagerExtensions
{
    /// <summary>
    /// Subscribes a callback to a string topic for a <see cref="TimeSpan"/> lifetime.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke while the subscription is active.</param>
    /// <param name="lifetime">The subscription lifetime.</param>
    public static void Subscribe(this BeaconManager manager, string topic, Action<BeaconHandle> handle, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);

        manager.SubscribeTimed(topic, handle, lifetime);
    }

    /// <summary>
    /// Subscribes a callback to an enum topic for a <see cref="TimeSpan"/> lifetime.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke while the subscription is active.</param>
    /// <param name="lifetime">The subscription lifetime.</param>
    public static void Subscribe(this BeaconManager manager, Enum topic, Action<BeaconHandle> handle, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        manager.SubscribeTimed(topic.ToEnumString(), handle, lifetime);
    }

    /// <summary>
    /// Subscribes a callback to a string topic until its first publish.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke once.</param>
    public static void SubscribeOnce(this BeaconManager manager, string topic, Action<BeaconHandle> handle)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(handle);

        manager.Subscribe(topic, CreateOnceHandler(manager, topic, handle));
    }

    /// <summary>
    /// Subscribes a callback to an enum topic until its first publish.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke once.</param>
    public static void SubscribeOnce(this BeaconManager manager, Enum topic, Action<BeaconHandle> handle)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        manager.SubscribeOnce(topic.ToEnumString(), handle);
    }

    /// <summary>
    /// Subscribes a callback to a string topic until its first publish or until
    /// its lifetime expires.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke once.</param>
    /// <param name="lifetime">The maximum subscription lifetime in seconds.</param>
    public static void SubscribeOnce(this BeaconManager manager, string topic, Action<BeaconHandle> handle, float lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(handle);

        manager.Subscribe(topic, CreateOnceHandler(manager, topic, handle), lifetime);
    }

    /// <summary>
    /// Subscribes a callback to an enum topic until its first publish or until
    /// its lifetime expires.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke once.</param>
    /// <param name="lifetime">The maximum subscription lifetime in seconds.</param>
    public static void SubscribeOnce(this BeaconManager manager, Enum topic, Action<BeaconHandle> handle, float lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        manager.SubscribeOnce(topic.ToEnumString(), handle, lifetime);
    }

    /// <summary>
    /// Subscribes a callback to a string topic until its first publish or until
    /// its <see cref="TimeSpan"/> lifetime expires.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="handle">The callback to invoke once.</param>
    /// <param name="lifetime">The maximum subscription lifetime.</param>
    public static void SubscribeOnce(this BeaconManager manager, string topic, Action<BeaconHandle> handle, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(handle);

        manager.SubscribeTimed(topic, CreateOnceHandler(manager, topic, handle), lifetime);
    }

    /// <summary>
    /// Subscribes a callback to an enum topic until its first publish or until
    /// its <see cref="TimeSpan"/> lifetime expires.
    /// </summary>
    /// <param name="manager">The beacon manager that owns the subscription.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="handle">The callback to invoke once.</param>
    /// <param name="lifetime">The maximum subscription lifetime.</param>
    public static void SubscribeOnce(this BeaconManager manager, Enum topic, Action<BeaconHandle> handle, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        manager.SubscribeOnce(topic.ToEnumString(), handle, lifetime);
    }

    /// <summary>
    /// Schedules a string topic to be published after a scaled-time delay.
    /// </summary>
    /// <param name="manager">The beacon manager that will publish the topic.</param>
    /// <param name="topic">The topic to publish.</param>
    /// <param name="seconds">
    /// Delay in scaled seconds. Values less than or equal to zero make the publish
    /// eligible on the next coroutine update.
    /// </param>
    /// <param name="data">Optional payload items delivered to subscribers.</param>
    /// <returns>
    /// A coroutine handle that can be queried or stopped before the publish occurs.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="manager"/> is null, or <paramref name="topic"/> is null or empty.
    /// </exception>
    public static CoroutineHandle PublishDelay(this BeaconManager manager, string topic, float seconds, params object[] data)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrEmpty(topic))
            throw new ArgumentNullException(nameof(topic), "topic is null or empty");

        data ??= Array.Empty<object>();

        return CoroutineManager.Instance.Run(
            seconds,
            PublishAfterDelay(manager, topic, data));
    }

    /// <summary>
    /// Schedules an enum topic to be published after a scaled-time delay.
    /// </summary>
    /// <param name="manager">The beacon manager that will publish the topic.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <param name="seconds">
    /// Delay in scaled seconds. Values less than or equal to zero make the publish
    /// eligible on the next coroutine update.
    /// </param>
    /// <param name="data">Optional payload items delivered to subscribers.</param>
    /// <returns>
    /// A coroutine handle that can be queried or stopped before the publish occurs.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="manager"/> or <paramref name="topic"/> is null.
    /// </exception>
    public static CoroutineHandle PublishDelay(this BeaconManager manager, Enum topic, float seconds, params object[] data)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        return manager.PublishDelay(
            topic.ToEnumString(),
            seconds,
            data ?? Array.Empty<object>());
    }

    private static Action<BeaconHandle> CreateOnceHandler(BeaconManager manager, string topic, Action<BeaconHandle> handle)
    {
        int invoked = 0;
        Action<BeaconHandle> wrappedHandler = null;

        wrappedHandler = beacon =>
        {
            if (Interlocked.Exchange(ref invoked, 1) != 0)
                return;

            manager.Unsubscribe(topic, wrappedHandler);
            handle(beacon);
        };

        return wrappedHandler;
    }

    private static IEnumerator PublishAfterDelay(BeaconManager manager, string topic, object[] data)
    {
        manager.Publish(topic, data);
        yield break;
    }
}
