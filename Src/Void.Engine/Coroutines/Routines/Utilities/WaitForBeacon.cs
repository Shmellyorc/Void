// ============================================================================
//  WaitForBeacon.cs
// ============================================================================
//  Coroutine utility that waits for a matching beacon publication.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// Waits until a matching beacon is published on a topic or an optional timeout expires.
/// </summary>
/// <remarks>
/// <para>
/// Subscription begins lazily on the first call to <see cref="MoveNext"/>.
/// A negative timeout disables timeout handling. Non-negative timeouts advance
/// with <see cref="FrameTime.DeltaTime"/> and therefore respect the game's time scale.
/// </para>
/// <para>
/// When a matching beacon arrives, <see cref="Result"/> is set and the routine
/// unsubscribes immediately. If the wait times out, <see cref="Result"/> remains
/// <see langword="null"/>.
/// </para>
/// <code>
/// var wait = new WaitForBeacon(
///     "DamageEvent",
///     handle => handle.TryGet&lt;int&gt;(0, out int damage) &amp;&amp; damage &gt; 0,
///     timeoutSeconds: 5f);
///
/// yield return wait;
///
/// if (wait.Result is BeaconHandle beacon)
/// {
///     int damage = beacon.Get&lt;int&gt;(0);
/// }
/// </code>
/// </remarks>
public sealed class WaitForBeacon : IEnumerator, IDisposable
{
    private readonly string _topic;
    private readonly Func<BeaconHandle, bool> _predicate;
    private readonly float _timeoutSeconds;
    private readonly Action<BeaconHandle> _handler;
    private bool _subscribed;
    private bool _done;
    private float _elapsed;

    /// <summary>
    /// Gets the matching beacon that completed the wait, or <see langword="null"/>
    /// when no matching beacon has completed it.
    /// </summary>
    public BeaconHandle? Result { get; private set; }

    /// <summary>
    /// Gets the value yielded by this routine, which is always <see langword="null"/>.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a beacon wait for a string topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="predicate">
    /// An optional filter. The wait completes only when this returns
    /// <see langword="true"/>.
    /// </param>
    /// <param name="timeoutSeconds">
    /// The maximum scaled-time wait in seconds, or a negative value for no timeout.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="topic"/> is <see langword="null"/> or empty.
    /// </exception>
    public WaitForBeacon(string topic, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic must be non-empty.", nameof(topic));

        _topic = topic;
        _predicate = predicate;
        _timeoutSeconds = timeoutSeconds;
        _handler = OnBeacon;
    }

    /// <summary>
    /// Initializes a beacon wait for an enum topic.
    /// </summary>
    /// <param name="topic">The enum value representing the topic.</param>
    /// <param name="predicate">
    /// An optional filter. The wait completes only when this returns
    /// <see langword="true"/>.
    /// </param>
    /// <param name="timeoutSeconds">
    /// The maximum scaled-time wait in seconds, or a negative value for no timeout.
    /// </param>
    public WaitForBeacon(Enum topic, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
        : this(topic.ToEnumString(), predicate, timeoutSeconds) { }

    /// <summary>
    /// Subscribes when necessary and advances the optional timeout.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> while waiting; otherwise, <see langword="false"/>
    /// after a matching beacon arrives or the timeout expires.
    /// </returns>
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
                Result = null;
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
