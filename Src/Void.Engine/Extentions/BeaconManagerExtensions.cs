// ============================================================================
//  BeaconManagerExtensions.cs
// ============================================================================
//  Coroutine-backed convenience extensions for delayed beacon publishing.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections;
using Void.Engine.Coroutines;

namespace Void.Engine.Beacons;

/// <summary>
/// Provides convenience extensions for <see cref="BeaconManager"/> without
/// adding timing responsibilities to the beacon manager itself.
/// </summary>
/// <remarks>
/// Delayed publishing is scheduled through <see cref="CoroutineManager"/> and
/// ultimately uses the normal synchronous <see cref="BeaconManager.Publish(string, object[])"/>
/// path when the delay completes.
/// </remarks>
public static class BeaconManagerExtensions
{
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
    public static CoroutineHandle PublishDelay(
        this BeaconManager manager,
        string topic,
        float seconds,
        params object[] data)
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
    public static CoroutineHandle PublishDelay(
        this BeaconManager manager,
        Enum topic,
        float seconds,
        params object[] data)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(topic);

        return manager.PublishDelay(
            topic.ToEnumString(),
            seconds,
            data ?? Array.Empty<object>());
    }

    private static IEnumerator PublishAfterDelay(
        BeaconManager manager,
        string topic,
        object[] data)
    {
        manager.Publish(topic, data);
        yield break;
    }
}
