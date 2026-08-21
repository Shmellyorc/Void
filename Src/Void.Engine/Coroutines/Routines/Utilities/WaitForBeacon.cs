// ============================================================================
//  WaitForBeacon.cs
// ============================================================================
//  A coroutine that waits for a beacon to be published.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;

namespace Void.Engine.Coroutines.Routines.Utilities;

/// <summary>
/// A coroutine that waits for a beacon to be published on a specific topic.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WaitForBeacon"/> class pauses the coroutine execution until
/// a beacon is published on the specified topic that matches the optional
/// predicate. It can also timeout if a timeout duration is provided.
/// </para>
/// <para>
/// This is useful for:
/// <list type="bullet">
///   <item><description>Waiting for events from other systems</description></item>
///   <item><description>Decoupled communication between coroutines</description></item>
///   <item><description>Waiting for asynchronous operations to signal completion</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Wait for any beacon on the "PlayerDied" topic
/// var beaconWait = new WaitForBeacon("PlayerDied");
/// yield return beaconWait;
/// 
/// // Access the result
/// var handle = beaconWait.Result;
/// 
/// // Wait with predicate filtering
/// var waitWithFilter = new WaitForBeacon(
///     "DamageEvent",
///     h => h.Source == "Player"
/// );
/// yield return waitWithFilter;
/// 
/// // Wait with timeout
/// var waitWithTimeout = new WaitForBeacon(
///     "NetworkResponse",
///     timeoutSeconds: 5f
/// );
/// yield return waitWithTimeout;
/// 
/// if (waitWithTimeout.Result == null)
///     Console.WriteLine("Timed out!");
/// 
/// // Wait with enum topic
/// var enumWait = new WaitForBeacon(MyTopics.GameStarted);
/// yield return enumWait;
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be used on the main thread.
/// </para>
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
    /// Gets the beacon handle that was received, or null if timed out.
    /// </summary>
    public BeaconHandle? Result { get; private set; }

    /// <summary>
    /// Gets the current value of the coroutine. Always returns null.
    /// </summary>
    public object Current => null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaitForBeacon"/> class.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="predicate">An optional predicate to filter beacons.</param>
    /// <param name="timeoutSeconds">The maximum time to wait, or -1 for no timeout.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="topic"/> is null or empty.</exception>
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
    /// Initializes a new instance of the <see cref="WaitForBeacon"/> class using an enum topic.
    /// </summary>
    /// <param name="topic">The enum representing the topic to subscribe to.</param>
    /// <param name="predicate">An optional predicate to filter beacons.</param>
    /// <param name="timeoutSeconds">The maximum time to wait, or -1 for no timeout.</param>
    public WaitForBeacon(Enum topic, Func<BeaconHandle, bool> predicate = null, float timeoutSeconds = -1f)
        : this(topic.ToEnumString(), predicate, timeoutSeconds) { }

    /// <summary>
    /// Advances the coroutine by one frame.
    /// </summary>
    /// <returns><see langword="true"/> if still waiting; otherwise, <see langword="false"/>.</returns>
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
    /// Resets the coroutine to its initial state. Not supported.
    /// </summary>
    public void Reset() => throw new NotSupportedException();

    /// <summary>
    /// Disposes the coroutine and unsubscribes from the beacon topic.
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