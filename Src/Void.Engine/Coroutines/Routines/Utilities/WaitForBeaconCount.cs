// ============================================================================
//  WaitForBeaconCount.cs
// ============================================================================
//  Coroutine utility that waits for a number of matching beacon publications.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// Waits for a specified number of matching beacons on a topic or an optional timeout.
/// </summary>
/// <remarks>
/// <para>
/// Subscription begins lazily on the first call to <see cref="MoveNext"/>.
/// Only beacons accepted by the optional predicate contribute to the target count.
/// A negative timeout disables timeout handling; otherwise the timeout advances
/// with <see cref="FrameTime.DeltaTime"/>.
/// </para>
/// <para>
/// <see cref="LastBeacon"/> contains the most recent matching beacon received,
/// including when the routine later ends because of a timeout. A null value means
/// that no matching beacon has been received.
/// </para>
/// <code>
/// var wait = new WaitForBeaconCount("EnemyDefeated", 3, timeoutSeconds: 10f);
/// yield return wait;
///
/// if (wait.LastBeacon is BeaconHandle last)
/// {
///     string topic = last.Topic;
/// }
/// </code>
/// </remarks>
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

    /// <summary>
    /// Gets the value yielded by this routine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Gets the most recent matching beacon received, or <see langword="null"/>
    /// if no matching beacon has been received.
    /// </summary>
    public BeaconHandle? LastBeacon { get; private set; }

    /// <summary>
    /// Initializes a counted beacon wait for a string topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="count">The number of matching beacons required to complete.</param>
    /// <param name="predicate">
    /// An optional filter. Only beacons for which this returns
    /// <see langword="true"/> are counted.
    /// </param>
    /// <param name="timeoutSeconds">
    /// The maximum scaled-time wait in seconds, or a negative value for no timeout.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="topic"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is less than or equal to zero.
    /// </exception>
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

    /// <summary>
    /// Initializes a counted beacon wait for an enum topic.
    /// </summary>
    /// <param name="topic">The enum value representing the topic.</param>
    /// <param name="count">The number of matching beacons required to complete.</param>
    /// <param name="predicate">
    /// An optional filter. Only beacons for which this returns
    /// <see langword="true"/> are counted.
    /// </param>
    /// <param name="timeoutSeconds">
    /// The maximum scaled-time wait in seconds, or a negative value for no timeout.
    /// </param>
    public WaitForBeaconCount(Enum topic, int count, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
        : this(topic.ToEnumString(), count, predicate, timeoutSeconds) { }

    /// <summary>
    /// Subscribes when necessary and advances the optional timeout.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while waiting; otherwise, <see langword="false"/>
    /// after the target count is reached or the timeout expires.
    /// </returns>
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

    /// <summary>
    /// Resetting this routine is not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Unsubscribes this wait from its beacon topic when currently subscribed.
    /// </summary>
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
