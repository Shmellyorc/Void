// ============================================================================
//  SoundInstancePool.cs
// ============================================================================
//  Manages reusable sound instances, background updates, and voice allocation.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Void.Engine.Sounds;

/// <summary>
/// Manages the shared pool of reusable <see cref="SoundInstance"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// The pool pre-allocates the number of instances configured by
/// <see cref="GameSettings.SetAudioLimit(uint)"/> and updates active instances
/// on a background task at approximately 60 Hz.
/// </para>
/// <para>
/// When no unused instance is available, the pool first reuses a completed
/// instance, then may steal a lower-priority active instance, and finally falls
/// back to the longest-playing active instance.
/// </para>
/// <para>
/// Public operations that inspect or modify the active and available collections
/// are synchronized internally. Events can be raised from either the calling
/// thread or the background update thread depending on the operation that caused them.
/// </para>
/// <code>
/// SoundInstancePool pool = SoundInstancePool.Instance;
/// SoundInstance instance = pool.GetInstance(SoundPriority.Normal);
/// </code>
/// </remarks>
public sealed class SoundInstancePool : IDisposable
{
    private static readonly Lazy<SoundInstancePool> _instance =
        new Lazy<SoundInstancePool>(() => new SoundInstancePool());

    /// <summary>
    /// Gets the shared sound-instance pool.
    /// </summary>
    public static SoundInstancePool Instance => _instance.Value;

    private readonly Queue<SoundInstance> _availableInstances;
    private readonly List<SoundInstance> _activeInstances;
    private readonly int _maxInstances;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _updateTask;
    private bool _isDisposed;

    /// <summary>
    /// Occurs when an available instance is allocated from the pool.
    /// </summary>
    public event EventHandler<SoundEventArgs> InstanceCreated;

    /// <summary>
    /// Occurs when an existing instance is reset and reused.
    /// </summary>
    public event EventHandler<SoundEventArgs> InstanceRecycled;

    /// <summary>
    /// Occurs when the pool or one of its managed instances reports an error.
    /// </summary>
    public event EventHandler<SoundErrorEventArgs> InstanceError;

    /// <summary>
    /// Background update interval in milliseconds.
    /// </summary>
    public const float UpdateInterval = 16.67f;

    /// <summary>
    /// Gets the number of instances currently owned by the active list.
    /// </summary>
    public int ActiveCount
    {
        get
        {
            lock (_lock)
                return _activeInstances.Count;
        }
    }

    /// <summary>
    /// Gets the number of unused instances currently available for allocation.
    /// </summary>
    public int AvailableCount
    {
        get
        {
            lock (_lock)
                return _availableInstances.Count;
        }
    }

    /// <summary>
    /// Gets the fixed number of instances managed by the pool.
    /// </summary>
    public int TotalInstances => _maxInstances;

    /// <summary>
    /// Gets whether the pool has no available instance and every managed instance is active.
    /// </summary>
    public bool IsExhausted
    {
        get
        {
            lock (_lock)
                return _availableInstances.Count == 0 && _activeInstances.Count >= _maxInstances;
        }
    }

    /// <summary>
    /// Gets whether the background update task has not completed.
    /// </summary>
    public bool IsRunning => !_updateTask.IsCompleted;

    private SoundInstancePool()
    {
        var maxInstances = GameSettings.Instance.AudioLimit;
        if (maxInstances < 32)
            throw new InvalidOperationException($"AudioLimit must be at least 32 to ensure proper sound pool operation. Current value: {maxInstances}");

        _maxInstances = maxInstances;
        _availableInstances = new Queue<SoundInstance>();
        _activeInstances = new List<SoundInstance>();
        _cancellationTokenSource = new CancellationTokenSource();
        _isDisposed = false;

        for (int i = 0; i < _maxInstances; i++)
        {
            var instance = new SoundInstance();
            SubscribeToInstanceEvents(instance);
            _availableInstances.Enqueue(instance);
        }

        _updateTask = Task.Factory.StartNew(
            UpdateLoop,
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
    }

    private async Task UpdateLoop()
    {
        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var deltaTime = (float)timer.Elapsed.TotalSeconds;
            timer.Restart();

            try
            {
                Update(deltaTime);
            }
            catch (Exception ex)
            {
                InstanceError?.Invoke(this, new SoundErrorEventArgs(null, ex, "Error in background update loop"));
            }

            try
            {
                await Task.Delay((int)UpdateInterval, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void Update(float deltaTime)
    {
        lock (_lock)
        {
            var completedInstances = new List<SoundInstance>();
            var activeSnapshot = _activeInstances.ToList();

            foreach (var instance in activeSnapshot)
            {
                instance.Update(deltaTime);

                if (instance.IsComplete)
                    completedInstances.Add(instance);
            }

            foreach (var instance in completedInstances)
            {
                ReturnInstance(instance);
            }
        }
    }

    private void SubscribeToInstanceEvents(SoundInstance instance)
    {
        instance.SoundCompleted += OnInstanceSoundCompleted;
        instance.SoundStopped += OnInstanceSoundStopped;
        instance.SoundLooped += OnInstanceSoundLooped;
        instance.SoundError += OnInstanceSoundError;
    }

    private void OnInstanceSoundError(object sender, SoundErrorEventArgs e)
    {
        InstanceError?.Invoke(this, e);

        if (sender is SoundInstance instance)
        {
            ReturnInstance(instance);
        }
    }

    private void OnInstanceSoundLooped(object sender, SoundLoopedEventArgs e)
    {
        // Just forward, no recycling needed. May be used in the future
    }

    private void OnInstanceSoundStopped(object sender, SoundStoppedEventArgs e)
    {
        if (sender is SoundInstance instance)
        {
            ReturnInstance(instance);
        }
    }

    private void OnInstanceSoundCompleted(object sender, SoundCompletedEventArgs e)
    {
        if (sender is SoundInstance instance)
        {
            ReturnInstance(instance);
        }
    }

    private void UnsubscribeFromInstanceEvents(SoundInstance instance)
    {
        instance.SoundCompleted -= OnInstanceSoundCompleted;
        instance.SoundStopped -= OnInstanceSoundStopped;
        instance.SoundLooped -= OnInstanceSoundLooped;
        instance.SoundError -= OnInstanceSoundError;
    }

    /// <summary>
    /// Gets an instance from the pool using the requested sound priority for voice allocation.
    /// </summary>
    /// <param name="newSoundPriority">Priority of the sound that will use the returned instance.</param>
    /// <returns>
    /// An allocated or recycled instance, or <see langword="null"/> when no instance can be provided.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Allocation order is: unused instance, completed instance, lower-priority
    /// active instance, then the longest-playing active instance.
    /// </para>
    /// <para>
    /// When stealing among lower-priority instances, the lowest priority is preferred;
    /// ties are resolved by stealing the longest-playing instance.
    /// </para>
    /// </remarks>
    public SoundInstance GetInstance(SoundPriority newSoundPriority = SoundPriority.Normal)
    {
        lock (_lock)
        {
            SoundInstance instance = null;

            try
            {
                if (_availableInstances.Count > 0)
                {
                    instance = _availableInstances.Dequeue();
                    _activeInstances.Add(instance);
                    InstanceCreated?.Invoke(this, new SoundEventArgs(instance));
                    return instance;
                }

                var recycled = _activeInstances.FirstOrDefault(x => x.IsComplete);
                if (recycled != null)
                {
                    UnsubscribeFromInstanceEvents(recycled);
                    recycled.Reset();
                    SubscribeToInstanceEvents(recycled);
                    InstanceRecycled?.Invoke(this, new SoundEventArgs(recycled));
                    return recycled;
                }

                var lowestPriority = _activeInstances
                    .Where(x => x.IsPlaying || x.IsPaused)
                    .Where(x => x.Priority < newSoundPriority)
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.PlayTime)
                    .FirstOrDefault();

                if (lowestPriority != null)
                {
                    // Prevent Stop() from firing the pool callback and returning
                    // this same instance to the available queue while we are
                    // deliberately stealing/reusing it.
                    UnsubscribeFromInstanceEvents(lowestPriority);
                    lowestPriority.Stop();
                    lowestPriority.Reset();
                    SubscribeToInstanceEvents(lowestPriority);
                    InstanceRecycled?.Invoke(this, new SoundEventArgs(lowestPriority));
                    return lowestPriority;
                }

                var oldest = _activeInstances
                    .Where(x => x.IsPlaying || x.IsPaused)
                    .OrderByDescending(x => x.PlayTime)
                    .FirstOrDefault();

                if (oldest != null)
                {
                    // Same rule as priority stealing: detach pool callbacks
                    // before Stop(), otherwise Stop() can enqueue the instance
                    // while this method is still returning it for immediate reuse.
                    UnsubscribeFromInstanceEvents(oldest);
                    oldest.Stop();
                    oldest.Reset();
                    SubscribeToInstanceEvents(oldest);
                    InstanceRecycled?.Invoke(this, new SoundEventArgs(oldest));
                    return oldest;
                }

                InstanceError?.Invoke(this, new SoundErrorEventArgs(null,
                    new InvalidOperationException($"Sound pool exhausted! Max: {_maxInstances}")));
                return null;
            }
            catch (Exception ex)
            {
                InstanceError?.Invoke(this, new SoundErrorEventArgs(instance, ex, "Failed to get sound instance."));
                throw;
            }
        }
    }

    private void ReturnInstance(SoundInstance instance)
    {
        lock (_lock)
        {
            try
            {
                if (!_activeInstances.Contains(instance))
                    return;

                if (_activeInstances.Remove(instance))
                {
                    instance.Reset();

                    if (!_availableInstances.Contains(instance))
                    {
                        _availableInstances.Enqueue(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                InstanceError?.Invoke(this, new SoundErrorEventArgs(instance, ex, "Failed to return instance to pool."));
            }
        }
    }

    /// <summary>
    /// Gets the number of playing or paused instances with the specified sound name.
    /// </summary>
    /// <param name="name">Sound name to count.</param>
    /// <returns>The matching active-instance count.</returns>
    public int GetActiveInstanceCount(string name)
    {
        lock (_lock)
        {
            return _activeInstances.Count(x => x.SoundName == name && (x.IsPlaying || x.IsPaused));
        }
    }

    /// <summary>
    /// Gets whether any playing or paused instance has the specified sound name.
    /// </summary>
    /// <param name="name">Sound name to check.</param>
    /// <returns><see langword="true"/> when a matching active instance exists; otherwise, <see langword="false"/>.</returns>
    public bool HasActiveInstance(string name)
        => GetActiveInstanceCount(name) > 0;

    /// <summary>
    /// Stops every active instance with the specified sound name.
    /// </summary>
    /// <param name="name">Sound name to stop.</param>
    public void StopAllInstances(string name)
    {
        lock (_lock)
        {
            var instances = _activeInstances
                .Where(x => x.SoundName == name)
                .ToList();

            foreach (var instance in instances)
                instance.Stop();
        }
    }

    /// <summary>
    /// Pauses every active instance with the specified sound name.
    /// </summary>
    /// <param name="name">Sound name to pause.</param>
    public void PauseAllInstances(string name)
    {
        lock (_lock)
        {
            var instances = _activeInstances
                .Where(x => x.SoundName == name)
                .ToList();

            foreach (var instance in instances)
                instance.Pause();
        }
    }

    /// <summary>
    /// Resumes every paused instance with the specified sound name.
    /// </summary>
    /// <param name="name">Sound name to resume.</param>
    public void ResumeAllInstances(string name)
    {
        lock (_lock)
        {
            var instances = _activeInstances
                .Where(x => x.SoundName == name && x.IsPaused)
                .ToList();

            foreach (var instance in instances)
                instance.Play();
        }
    }

    /// <summary>
    /// Cancels the update task and disposes every managed sound instance.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _cancellationTokenSource.Cancel();

        try
        {
            _updateTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Task was cancelled, ignore...
        }

        lock (_lock)
        {
            foreach (var instance in _activeInstances)
            {
                UnsubscribeFromInstanceEvents(instance);
                instance.Dispose();
            }
            _activeInstances.Clear();

            foreach (var instance in _availableInstances)
            {
                UnsubscribeFromInstanceEvents(instance);
                instance.Dispose();
            }
            _availableInstances.Clear();
        }

        _cancellationTokenSource.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets a snapshot of the instances currently held in the active list.
    /// </summary>
    /// <returns>A new list containing the current active instances.</returns>
    public List<SoundInstance> GetActiveInstances()
    {
        lock (_lock)
        {
            return [.. _activeInstances];
        }
    }

    /// <summary>
    /// Sets the instance volume of every active sound.
    /// </summary>
    /// <param name="volume">Volume passed to each active instance.</param>
    public void ApplyVolumeToAll(float volume)
    {
        lock (_lock)
        {
            foreach (var instance in _activeInstances)
            {
                instance.Volume = volume;
            }
        }
    }

    /// <summary>
    /// Stops every active sound instance.
    /// </summary>
    public void StopAll()
    {
        lock (_lock)
        {
            var instances = _activeInstances.ToList();
            foreach (var instance in instances)
            {
                instance.Stop();
            }
        }
    }

    /// <summary>
    /// Pauses every active sound instance.
    /// </summary>
    public void PauseAll()
    {
        lock (_lock)
        {
            var instances = _activeInstances.ToList();
            foreach (var instance in instances)
            {
                instance.Pause();
            }
        }
    }

    /// <summary>
    /// Resumes every paused sound instance.
    /// </summary>
    public void ResumeAll()
    {
        lock (_lock)
        {
            var instances = _activeInstances.Where(x => x.IsPaused).ToList();
            foreach (var instance in instances)
            {
                instance.Play();
            }
        }
    }
}
