// ============================================================================
//  SoundInstancePool.cs
// ============================================================================
//  Manages a pool of reusable sound instances with background update loop,
//  voice allocation, and priority-based instance stealing.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Void.Engine.Sounds;

/// <summary>
/// Manages a pool of reusable sound instances with automatic background updating,
/// voice allocation, and priority-based instance stealing.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SoundInstancePool"/> is a singleton that maintains a fixed
/// number of pre-allocated sound instances (default: 255) to avoid garbage
/// collection pressure from frequent allocations. It handles the complete
/// lifecycle of sound instances from creation to recycling.
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>On creation, the pool pre-allocates <see cref="_defaultMaxSounds"/> instances</description></item>
///   <item><description>Instances are stored in an available queue and an active list</description></item>
///   <item><description>A background task updates all active instances at 60Hz</description></item>
///   <item><description>Completed or stopped instances are automatically returned to the pool</description></item>
///   <item><description>When <see cref="GetInstance"/> is called, an instance is allocated or stolen</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Voice Allocation Strategy:</b>
/// When a new sound needs to play and no instances are available:
/// <list type="bullet">
///   <item><description>Recycle any stopped instances still in the active list</description></item>
///   <item><description>Steal the lowest priority playing instance if the new sound has higher priority</description></item>
///   <item><description>Steal the oldest playing instance as a fallback</description></item>
///   <item><description>Return <see langword="null"/> if no instance can be allocated (pool exhausted)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Background Update Loop:</b>
/// The pool runs a background task that updates all active sound instances at
/// 60Hz (16.67ms interval). This loop advances playback time, fires loop events,
/// detects completion, and automatically returns completed instances to the pool.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Get an instance from the pool (typically called via Sound.CreateInstance)
/// var instance = SoundInstancePool.Instance.GetInstance(SoundPriority.Normal);
/// 
/// // Check pool status
/// int active = SoundInstancePool.Instance.ActiveCount;
/// int available = SoundInstancePool.Instance.AvailableCount;
/// bool exhausted = SoundInstancePool.Instance.IsExhausted;
/// 
/// // Stop all sounds
/// SoundInstancePool.Instance.StopAll();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// The pool uses locks internally for thread safety. The background update loop
/// runs on a separate thread and is safe to use with the main thread.
/// </para>
/// </remarks>
public sealed class SoundInstancePool : IDisposable
{
    private static readonly Lazy<SoundInstancePool> _instance =
        new Lazy<SoundInstancePool>(() => new SoundInstancePool());

    /// <summary>
    /// Gets the singleton instance of the sound instance pool.
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
    /// Occurs when a new sound instance is created from the pool.
    /// </summary>
    public event EventHandler<SoundEventArgs> InstanceCreated;

    /// <summary>
    /// Occurs when a sound instance is recycled by the pool.
    /// </summary>
    public event EventHandler<SoundEventArgs> InstanceRecycled;

    /// <summary>
    /// Occurs when an error occurs in the pool or an instance.
    /// </summary>
    public event EventHandler<SoundErrorEventArgs> InstanceError;



    /// <summary>
    /// The update interval in milliseconds (60 FPS).
    /// </summary>
    public const float UpdateInterval = 16.67f;

    /// <summary>
    /// Gets the number of currently active sound instances.
    /// </summary>
    public int ActiveCount => _activeInstances.Count;

    /// <summary>
    /// Gets the number of available sound instances in the pool.
    /// </summary>
    public int AvailableCount => _availableInstances.Count;

    /// <summary>
    /// Gets the total number of sound instances managed by the pool.
    /// </summary>
    public int TotalInstances => _maxInstances;

    /// <summary>
    /// Gets a value indicating whether the pool is exhausted and no more instances are available.
    /// </summary>
    public bool IsExhausted => _availableInstances.Count == 0 && _activeInstances.Count >= _maxInstances;

    /// <summary>
    /// Gets a value indicating whether the background update task is running.
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

                if (instance.IsStopped)
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
    /// Gets a sound instance from the pool with the specified priority.
    /// </summary>
    /// <param name="newSoundPriority">The priority of the new sound for voice allocation.</param>
    /// <returns>A sound instance ready to use, or <see langword="null"/> if the pool is exhausted.</returns>
    /// <remarks>
    /// <para>
    /// This method attempts to allocate a sound instance using the following strategy:
    /// <list type="number">
    ///   <item><description>If an instance is available in the queue, it is returned immediately</description></item>
    ///   <item><description>If any active instance is stopped, it is recycled</description></item>
    ///   <item><description>If the new sound has higher priority than active sounds, the lowest priority active sound is stolen</description></item>
    ///   <item><description>As a fallback, the oldest active sound is stolen</description></item>
    ///   <item><description>If no instance can be allocated, <see langword="null"/> is returned</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When an instance is stolen, it is stopped, reset, and reinitialized with
    /// the new sound data. The <see cref="InstanceRecycled"/> event is fired.
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

                var recycled = _activeInstances.FirstOrDefault(x => x.IsStopped);
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
                    .ThenBy(x => x.PlayTime)
                    .FirstOrDefault();

                if (lowestPriority != null)
                {
                    lowestPriority.Stop();
                    UnsubscribeFromInstanceEvents(lowestPriority);
                    lowestPriority.Reset();
                    SubscribeToInstanceEvents(lowestPriority);
                    InstanceRecycled?.Invoke(this, new SoundEventArgs(lowestPriority));
                    return lowestPriority;
                }

                var oldest = _activeInstances
                    .Where(x => x.IsPlaying || x.IsPaused)
                    .OrderBy(x => x.PlayTime)
                    .FirstOrDefault();

                if (oldest != null)
                {
                    oldest.Stop();
                    UnsubscribeFromInstanceEvents(oldest);
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
    /// Gets the number of active instances for a specific sound name.
    /// </summary>
    /// <param name="name">The name of the sound to count.</param>
    /// <returns>The number of active instances playing or paused for the specified sound.</returns>
    public int GetActiveInstanceCount(string name)
    {
        lock (_lock)
        {
            return _activeInstances.Count(x => x.SoundName == name && (x.IsPlaying || x.IsPaused));
        }
    }

    /// <summary>
    /// Determines whether any active instances exist for a specific sound name.
    /// </summary>
    /// <param name="name">The name of the sound to check.</param>
    /// <returns><see langword="true"/> if at least one instance is playing or paused; otherwise, <see langword="false"/>.</returns>
    public bool HasActiveInstance(string name)
        => GetActiveInstanceCount(name) > 0;

    /// <summary>
    /// Stops all instances with the specified sound name.
    /// </summary>
    /// <param name="name">The name of the sound to stop.</param>
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
    /// Pauses all instances with the specified sound name.
    /// </summary>
    /// <param name="name">The name of the sound to pause.</param>
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
    /// Resumes all paused instances with the specified sound name.
    /// </summary>
    /// <param name="name">The name of the sound to resume.</param>
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
    /// Disposes the sound instance pool and releases all resources.
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
    /// Gets a list of all active sound instances.
    /// </summary>
    /// <returns>A list containing all active sound instances.</returns>
    public List<SoundInstance> GetActiveInstances()
    {
        lock (_lock)
        {
            return [.. _activeInstances];
        }
    }

    /// <summary>
    /// Applies the specified volume to all active sound instances.
    /// </summary>
    /// <param name="volume">The volume value between 0 and 1 to apply.</param>
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
    /// Stops all active sound instances.
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
    /// Pauses all active sound instances.
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
    /// Resumes all paused sound instances.
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