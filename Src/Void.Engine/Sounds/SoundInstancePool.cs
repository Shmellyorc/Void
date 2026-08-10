namespace Void.Engine.Sounds;

public sealed class SoundInstancePool : IDisposable
{
    private static readonly Lazy<SoundInstancePool> _instance =
        new Lazy<SoundInstancePool>(() => new SoundInstancePool());

    public static SoundInstancePool Instance => _instance.Value;

    private readonly Queue<SoundInstance> _availableInstances;
    private readonly List<SoundInstance> _activeInstances;
    private readonly int _maxInstances;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _updateTask;
    private bool _isDisposed;

    public event EventHandler<SoundEventArgs> InstanceCreated;
    public event EventHandler<SoundEventArgs> InstanceRecycled;
    public event EventHandler<SoundErrorEventArgs> InstanceError;

    public const int DefaultMaxSounds = 255;
    public const float UpdateInterval = 16.67f; // 60fps;
    public int ActiveCount => _activeInstances.Count;
    public int AvailableCount => _availableInstances.Count;
    public int TotalInstances => _maxInstances;
    public bool IsExhausted => _availableInstances.Count == 0 && _activeInstances.Count >= _maxInstances;
    public bool IsRunning => !_updateTask.IsCompleted;

    private SoundInstancePool()
    {
        _maxInstances = DefaultMaxSounds;
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
                // Excepted when shutting down
                break;
            }
        }
    }

    private void Update(float deltaTime)
    {
        lock (_lock)
        {
            var completedInstances = new List<SoundInstance>();

            foreach (var instance in _activeInstances)
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

        var instance = sender as SoundInstance;
        if (instance != null)
        {
            Task.Factory.StartNew(() => ReturnInstance(instance));
        }
    }

    private void OnInstanceSoundLooped(object sender, SoundLoopedEventArgs e)
    {
        // Just forward, no recycling needed. May be used in the future
    }

    private void OnInstanceSoundStopped(object sender, SoundStoppedEventArgs e)
    {
        var instance = sender as SoundInstance;
        if (instance != null)
        {
            Task.Factory.StartNew(() => ReturnInstance(instance));
        }
    }

    private void OnInstanceSoundCompleted(object sender, SoundCompletedEventArgs e)
    {
        var instance = sender as SoundInstance;
        if (instance != null)
        {
            Task.Factory.StartNew(() => ReturnInstance(instance));
        }
    }

    private void UnsubscribeFromInstanceEvents(SoundInstance instance)
    {
        instance.SoundCompleted -= OnInstanceSoundCompleted;
        instance.SoundStopped -= OnInstanceSoundStopped;
        instance.SoundLooped -= OnInstanceSoundLooped;
        instance.SoundError -= OnInstanceSoundError;
    }

    public SoundInstance GetInstance()
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

                var oldest = _activeInstances.OrderBy(s => s.PlayTime).FirstOrDefault();
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
                if (_activeInstances.Remove(instance))
                {
                    instance.Reset();
                    _availableInstances.Enqueue(instance);
                }
            }
            catch (Exception ex)
            {
                InstanceError?.Invoke(this, new SoundErrorEventArgs(instance, ex, "Failed to return instance to pool."));
            }
        }
    }

    public int GetActiveInstanceCount(string name)
    {
        lock (_lock)
        {
            return _activeInstances.Count(x => x.SoundName == name && (x.IsPlaying || x.IsPaused));
        }
    }

    public bool HasActiveInstance(string name)
        => GetActiveInstanceCount(name) > 0;

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
}
