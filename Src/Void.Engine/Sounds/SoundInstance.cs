// ============================================================================
//  SoundInstance.cs
// ============================================================================
//  Represents a single sound instance with playback control, volume management,
//  panning, pitch adjustment, and event notifications.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Sounds;

/// <summary>
/// Defines the playback status of a sound instance.
/// </summary>
public enum SoundStatus
{
    /// <summary>
    /// The sound is stopped and not playing.
    /// </summary>
    Stopped,

    /// <summary>
    /// The sound is paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// The sound is currently playing.
    /// </summary>
    Playing,
}

/// <summary>
/// Represents a playable sound instance with full playback control, volume management,
/// panning, pitch adjustment, and event notifications.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SoundInstance"/> class provides comprehensive control over
/// individual sound playback including volume, pitch, panning, looping, and
/// priority management. It supports category-based volume control and raises
/// events for completion, stopping, looping, and errors.
/// </para>
/// <para>
/// <b>Creation Flow:</b>
/// <list type="number">
///   <item><description>Load a <see cref="Sound"/> asset through <see cref="AssetManager.Load{T}"/></description></item>
///   <item><description>Call <see cref="Sound.CreateInstance"/> which obtains an instance from the <see cref="SoundInstancePool"/></description></item>
///   <item><description>The pool initializes the instance with the sound buffer and priority</description></item>
///   <item><description>Call <see cref="Play"/> to begin playback</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lifecycle Management:</b>
/// Sound instances are managed by the <see cref="SoundInstancePool"/> singleton.
/// The pool maintains a fixed number of pre-allocated instances (default: 255)
/// that are reused to avoid garbage collection pressure. When an instance is
/// obtained, it moves from the available queue to the active list. When playback
/// completes or the instance is disposed, it is reset and returned to the
/// available queue for reuse.
/// </para>
/// <para>
/// <b>Voice Allocation:</b>
/// If all instances are active and a new sound needs to play, the pool will:
/// <list type="bullet">
///   <item><description>Recycle any stopped instances first</description></item>
///   <item><description>Steal the lowest priority playing instance if the new sound has higher priority</description></item>
///   <item><description>Steal the oldest playing instance as a fallback</description></item>
///   <item><description>Return <see langword="null"/> if no instance can be allocated</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Load sound asset through AssetManager
/// var soundAsset = AssetManager.Instance.Load&lt;Sound&gt;("explosion.wav");
/// 
/// // Create a sound instance from the asset (pool handles allocation)
/// var sound = soundAsset.CreateInstance(SoundCategory.SFX);
/// sound.Volume = 0.8f;
/// sound.Pan = -0.5f;
/// sound.Play();
/// 
/// // Handle completion - instance auto-returns to pool
/// sound.SoundCompleted += (s, e) =>
/// {
///     Console.WriteLine("Sound finished playing");
///     sound.Dispose(); // Returns the instance to the available pool
/// };
/// </code>
/// </para>
/// <para>
/// <b>Volume System:</b>
/// The effective volume is calculated as: <c>RawVolume × CategoryVolume</c>.
/// The raw volume is set per-instance, while the category volume is controlled
/// globally through <see cref="SoundHelper.SetCategoryVolume{T}"/>.
/// </para>
/// <para>
/// <b>Event Order:</b>
/// <list type="number">
///   <item><description><see cref="SoundLooped"/> - Fired each loop iteration (if looping)</description></item>
///   <item><description><see cref="SoundCompleted"/> - Fired when natural playback ends</description></item>
///   <item><description><see cref="SoundStopped"/> - Fired when stopped manually or interrupted</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Asset Eviction Awareness:</b>
/// If the underlying <see cref="Sound"/> asset is evicted from the AssetManager
/// cache, the instance remains valid as long as it holds a reference to the
/// sound buffer. However, attempting to create new instances from an evicted
/// asset will automatically reload it.
/// </para>
/// <para>
/// <b>Update Loop:</b>
/// The <see cref="SoundInstancePool"/> runs a background task that updates all
/// active sound instances at 60Hz. This task advances playback time, fires
/// loop events, detects completion, and automatically returns completed
/// instances to the pool.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed on the
/// main thread or synchronized appropriately. The underlying pool uses locks
/// internally for thread safety.
/// </para>
/// </remarks>
public sealed class SoundInstance : IDisposable
{
    private SFSound _sfmlSound;
    private SFSoundBuffer _buffer;
    private bool _isInitialized;
    private float _playTime;
    private bool _isDisposed;
    private string _soundName;
    private bool _hasNotifiedCompletion;
    private int _loopCount;
    private bool _wasPlaying;
    private bool _wasPaused;

    /// <summary>
    /// Gets a value indicating whether the sound instance has been disposed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// Gets or sets the category of the sound for volume grouping.
    /// </summary>
    public Enum Category { get; internal set; }

    /// <summary>
    /// Gets the current playback status of the sound.
    /// </summary>
    public SoundStatus Status
    {
        get
        {
            if (_isDisposed || !_isInitialized)
                return SoundStatus.Stopped;

            return (SoundStatus)_sfmlSound.Status;
        }
    }

    /// <summary>
    /// Gets or sets the volume of the sound between 0 and 1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The volume is affected by category volume, so the actual output volume
    /// is the raw volume multiplied by the category volume.
    /// </para>
    /// <para>
    /// Setting this property to 0 mutes the sound, while 1 is maximum volume.
    /// </para>
    /// </remarks>
    public float Volume
    {
        get => _volume;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;
            if (MathHelper.AlmostEquals(value, _rawVolume, MathHelper.Epsilon))
                return;

            _rawVolume = Math.Clamp(value, 0f, 1f);

            if (Category != null)
                ApplyCategoryVolume(Category);
            else
            {
                _volume = _rawVolume;
                _sfmlSound.Volume = _volume * 100f;
            }
        }
    }
    private float _rawVolume = 1f;
    private float _volume = 1f;

    internal void ApplyCategoryVolume(Enum category)
    {
        if (_isDisposed || !_isInitialized)
            return;

        float categoryVolume = 1f;
        if (category != null)
        {
            var method = typeof(SoundHelper).GetMethod(nameof(SoundHelper.GetCategoryVolume));
            var genericMethod = method.MakeGenericMethod(category.GetType());
            categoryVolume = (float)genericMethod.Invoke(null, new[] { category });
        }

        _volume = _rawVolume * categoryVolume;
        _sfmlSound.Volume = _volume * 100f;
    }

    /// <summary>
    /// Gets or sets the pitch of the sound between 0.1 and 10.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pitch values above 1 speed up playback and increase pitch, while values
    /// below 1 slow down playback and decrease pitch. A value of 1 is normal speed.
    /// </para>
    /// </remarks>
    public float Pitch
    {
        get => _pitch;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;
            if (MathHelper.AlmostEquals(value, _pitch, MathHelper.Epsilon))
                return;

            _pitch = Math.Clamp(value, 0.1f, 10f);
            _sfmlSound.Pitch = _pitch;
        }
    }
    private float _pitch = 1f;

    /// <summary>
    /// Gets or sets the pan of the sound between -1 (left) and 1 (right).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Panning controls the stereo balance of the sound. A value of -1 pans
    /// fully to the left, 1 pans fully to the right, and 0 centers the sound.
    /// </para>
    /// <para>
    /// When pan is set to near zero, the sound is reset to normal stereo positioning.
    /// </para>
    /// </remarks>
    public float Pan
    {
        get => _pan;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;

            _pan = Math.Clamp(value, -1f, 1f);

            if (MathHelper.AlmostZero(_pan, MathHelper.Epsilon))
            {
                _sfmlSound.RelativeToListener = false;
                _sfmlSound.Position = new(0f, 0f, 0f);
                _pan = 0f;
                return;
            }

            _sfmlSound.RelativeToListener = true;
            _sfmlSound.Position = new(_pan, 0f, 0f);
        }
    }
    private float _pan = 0f;

    /// <summary>
    /// Gets or sets whether the sound should loop.
    /// </summary>
    public bool Looping
    {
        get => _looping;
        set
        {
            if (_isDisposed)
                return;

            _looping = value;

            if (_isInitialized && _sfmlSound != null)
                _sfmlSound.IsLooping = _looping;
        }
    }
    private bool _looping;

    /// <summary>
    /// Gets the current playback time of the sound in seconds.
    /// </summary>
    public float PlayTime => _playTime;

    /// <summary>
    /// Gets the total duration of the sound in seconds.
    /// </summary>
    public float Duration => _buffer != null && !_buffer.IsInvalid ? _buffer.Duration.AsSeconds() : 0f;

    /// <summary>
    /// Gets the playback progress as a value between 0 and 1.
    /// </summary>
    public float Progress => Duration > 0 ? Math.Clamp(_playTime / Duration, 0f, 1f) : 0f;

    /// <summary>
    /// Gets the number of times the sound has looped.
    /// </summary>
    public int LoopCount => _loopCount;

    /// <summary>
    /// Gets a value indicating whether the sound is currently playing.
    /// </summary>
    public bool IsPlaying => Status == SoundStatus.Playing;

    /// <summary>
    /// Gets a value indicating whether the sound is currently paused.
    /// </summary>
    public bool IsPaused => Status == SoundStatus.Paused;

    /// <summary>
    /// Gets a value indicating whether the sound is currently stopped.
    /// </summary>
    public bool IsStopped => Status == SoundStatus.Stopped;

    /// <summary>
    /// Gets a value indicating whether the sound has completed playback and stopped.
    /// </summary>
    public bool IsComplete => IsStopped && _hasNotifiedCompletion;

    /// <summary>
    /// Gets a value indicating whether the sound instance is valid and ready for use.
    /// </summary>
    public bool IsValid => _isInitialized && !_isDisposed;

    /// <summary>
    /// Gets or sets the priority of the sound for voice allocation.
    /// </summary>
    public SoundPriority Priority { get; set; } = SoundPriority.Normal;

    /// <summary>
    /// Gets the name of the sound.
    /// </summary>
    public string SoundName
    {
        get => _soundName;
        internal set => _soundName = value;
    }

    /// <summary>
    /// Occurs when the sound completes playback.
    /// </summary>
    public event EventHandler<SoundCompletedEventArgs> SoundCompleted;

    /// <summary>
    /// Occurs when the sound stops playing.
    /// </summary>
    public event EventHandler<SoundStoppedEventArgs> SoundStopped;

    /// <summary>
    /// Occurs when the sound loops.
    /// </summary>
    public event EventHandler<SoundLoopedEventArgs> SoundLooped;

    /// <summary>
    /// Occurs when an error occurs during sound playback.
    /// </summary>
    public event EventHandler<SoundErrorEventArgs> SoundError;

    internal SoundInstance()
    {
        _isInitialized = false;
        _playTime = 0f;
        _isDisposed = false;
        _hasNotifiedCompletion = false;
        _loopCount = 0;
        _wasPlaying = false;
        _wasPaused = false;
        _looping = false;
    }

    internal void Initialize(SFSoundBuffer buffer, Enum category = null, SoundPriority priority = SoundPriority.Normal)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SoundInstance));

        if (_isInitialized && Status != SoundStatus.Stopped)
            Stop();

        _buffer = buffer;

        if (_sfmlSound == null)
            _sfmlSound = new SFSound(buffer);
        else
            _sfmlSound.SoundBuffer = buffer;

        Category = category;
        Priority = priority;
        _isInitialized = true;
        _playTime = 0f;
        _hasNotifiedCompletion = false;
        _loopCount = 0;
        _wasPlaying = false;
        _wasPaused = false;

        _looping = false;
        if (_sfmlSound != null)
            _sfmlSound.IsLooping = false;
    }

    internal void Update(float deltaTime)
    {
        if (_isDisposed || !_isInitialized)
            return;

        try
        {
            if (Status == SoundStatus.Playing)
            {
                _playTime += deltaTime;

                if (Looping && _buffer != null && _playTime >= _buffer.Duration.AsSeconds())
                {
                    _loopCount++;
                    _playTime = 0f;
                    SoundLooped?.Invoke(this, new SoundLoopedEventArgs(this, _loopCount));
                }

                if (!Looping && !_hasNotifiedCompletion && _buffer != null && _playTime >= _buffer.Duration.AsSeconds())
                {
                    _hasNotifiedCompletion = true;
                    SoundCompleted?.Invoke(this, new SoundCompletedEventArgs(this, false, _loopCount));
                }
            }
            else if (Status == SoundStatus.Stopped && _isInitialized)
            {
                if (!_hasNotifiedCompletion)
                {
                    _hasNotifiedCompletion = true;
                    SoundStopped?.Invoke(this, new SoundStoppedEventArgs(this, _wasPlaying, _wasPaused));
                }
            }
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Error during sound update."));
        }
    }

    /// <summary>
    /// Starts playing the sound.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the sound instance is not initialized.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the sound instance has been disposed.</exception>
    public void Play()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Sound instance not initialized.");
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SoundInstance));

        try
        {
            _hasNotifiedCompletion = false;
            _wasPlaying = true;
            _wasPaused = false;

            if (_sfmlSound != null)
                _sfmlSound.IsLooping = _looping;

            _sfmlSound.Play();
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to play sound."));
            throw;
        }
    }

    /// <summary>
    /// Pauses the sound playback.
    /// </summary>
    public void Pause()
    {
        if (_isDisposed)
            return;

        try
        {
            if (Status == SoundStatus.Playing)
            {
                _wasPaused = true;
                _wasPlaying = false;
                _sfmlSound.Pause();
            }
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to pause sound."));
        }
    }

    /// <summary>
    /// Stops the sound playback and resets to the beginning.
    /// </summary>
    public void Stop()
    {
        if (_isDisposed)
            return;

        try
        {
            bool wasPlaying = Status == SoundStatus.Playing;
            bool wasPaused = Status == SoundStatus.Paused;

            _sfmlSound.Stop();

            if (!_hasNotifiedCompletion)
            {
                SoundStopped?.Invoke(this, new SoundStoppedEventArgs(this, wasPlaying, wasPaused));
                _hasNotifiedCompletion = true;
            }
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to stop sound."));
        }
    }

    internal void Reset()
    {
        if (_isDisposed || !_isInitialized || _sfmlSound == null)
        {
            _buffer = null;
            _isInitialized = false;
            _playTime = 0f;
            Priority = SoundPriority.Normal;
            _hasNotifiedCompletion = false;
            _loopCount = 0;
            _wasPlaying = false;
            _wasPaused = false;
            _rawVolume = 1f;
            _volume = 1f;
            _looping = false;
            _pitch = 1f;
            _pan = 0f;
            Category = null;
            _isDisposed = false;
            return;
        }

        try
        {
            if (_sfmlSound.CPointer != IntPtr.Zero)
            {
                _sfmlSound?.Stop();
                _sfmlSound.IsLooping = false;
                _sfmlSound.SoundBuffer = null;
            }

            _buffer = null;
            _isInitialized = false;
            _playTime = 0f;
            _hasNotifiedCompletion = false;
            _loopCount = 0;
            _wasPlaying = false;
            _wasPaused = false;
            _looping = false;
            _rawVolume = 1f;
            _volume = 1f;
            _pitch = 1f;
            _pan = 0f;
            Category = null;
            _isDisposed = false;
        }
        catch (ObjectDisposedException)
        {
            _sfmlSound = null;
            _buffer = null;
            _isInitialized = false;
            _playTime = 0f;
            _hasNotifiedCompletion = false;
            _loopCount = 0;
            _wasPlaying = false;
            _wasPaused = false;
            _looping = false;
            _rawVolume = 1f;
            _volume = 1f;
            _pitch = 1f;
            _pan = 0f;
            Category = null;
            _isDisposed = false;
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to reset sound."));
        }
    }

    /// <summary>
    /// Disposes the sound instance and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            _sfmlSound?.Stop();
            _sfmlSound?.Dispose();
            _buffer = null;
            _isDisposed = true;
            _isInitialized = false;
        }
        catch (Exception ex)
        {
            Logger.Instance.ErrorWithCategory("Sound",
                "Error during SoundInstance disposal: {0}", ex.Message);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}