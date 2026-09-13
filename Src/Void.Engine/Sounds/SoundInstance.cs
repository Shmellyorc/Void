// ============================================================================
//  SoundInstance.cs
// ============================================================================
//  Pooled OpenAL sound source with playback state, volume, pitch, pan, and events.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using Void.Engine.Assets.Loaders;
using Void.Engine.Audio;
using Void.Engine.Helpers;
using Void.Engine.Logs;

namespace Void.Engine.Sounds;

/// <summary>
/// Describes the current playback state of a <see cref="SoundInstance"/>.
/// </summary>
public enum SoundStatus
{
    /// <summary>The instance is not currently playing.</summary>
    Stopped,

    /// <summary>The instance is paused.</summary>
    Paused,

    /// <summary>The instance is playing.</summary>
    Playing,
}

/// <summary>
/// Represents a pooled playable sound source backed by OpenAL.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created and recycled by <see cref="SoundInstancePool"/> and are
/// normally obtained from a <see cref="Sound"/> through <see cref="Sound.CreateInstance(Enum)"/>.
/// </para>
/// <para>
/// Volume is combined with the current category and master volumes. Pitch values
/// are accepted from 0.1 through 10, while the OpenAL backend clamps playback to
/// its portable 0.5 through 2.0 range.
/// </para>
/// <code>
/// SoundInstance instance = sound.CreateInstance();
/// if (instance != null)
/// {
///     instance.Volume = 0.8f;
///     instance.Play();
/// }
/// </code>
/// </remarks>
public sealed class SoundInstance : IDisposable
{
    private uint _source;
    private AudioBuffer _buffer;
    private bool _isInitialized;
    private float _playTime;
    private bool _isDisposed;
    private string _soundName;
    private bool _hasNotifiedCompletion;
    private int _loopCount;
    private bool _wasPlaying;
    private bool _wasPaused;
    private SoundStatus _status;

    private float _rawVolume = 1f;
    private float _volume = 1f;
    private float _pitch = 1f;
    private float _pan;
    private bool _looping;

    /// <summary>Gets whether this instance has been disposed.</summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>Gets the optional game-defined category assigned to this instance.</summary>
    public Enum Category { get; internal set; }

    /// <summary>Gets the current playback state.</summary>
    public SoundStatus Status => _isDisposed || !_isInitialized ? SoundStatus.Stopped : _status;

    /// <summary>
    /// Gets the effective volume after category and master volume are applied,
    /// or sets the instance volume before those multipliers.
    /// </summary>
    /// <remarks>The assigned instance volume is clamped to the range zero through one.</remarks>
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
            ApplyCategoryVolume(Category);
        }
    }

    internal void ApplyCategoryVolume(Enum category)
    {
        if (_isDisposed || !_isInitialized)
            return;

        float categoryVolume = 1f;
        if (category != null)
        {
            var method = typeof(SoundHelper).GetMethod(nameof(SoundHelper.GetCategoryVolume));
            var genericMethod = method?.MakeGenericMethod(category.GetType());
            if (genericMethod != null)
                categoryVolume = (float)genericMethod.Invoke(null, new object[] { category });
        }

        _volume = _rawVolume * categoryVolume * SoundHelper.MasterVolume;
        AudioRuntime.SetGain(_source, _volume);
    }

    /// <summary>Gets or sets the requested playback pitch.</summary>
    /// <remarks>
    /// The public value is clamped from 0.1 through 10. The backend applies the
    /// portable OpenAL range of 0.5 through 2.0.
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

            // OpenAL 1.1 guarantees 0.5..2.0. Keep the public API permissive but
            // clamp the backend value to the portable range.
            _pitch = Math.Clamp(value, 0.1f, 10f);
            AudioRuntime.SetPitch(_source, _pitch);
        }
    }

    /// <summary>Gets or sets stereo pan from -1 for left through 1 for right.</summary>
    public float Pan
    {
        get => _pan;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;

            _pan = Math.Clamp(value, -1f, 1f);
            if (MathHelper.AlmostZero(_pan, MathHelper.Epsilon))
                _pan = 0f;
            AudioRuntime.SetPan(_source, _pan);
        }
    }

    /// <summary>Gets or sets whether playback loops continuously.</summary>
    public bool Looping
    {
        get => _looping;
        set
        {
            if (_isDisposed)
                return;

            _looping = value;
            if (_isInitialized)
                AudioRuntime.SetLooping(_source, value);
        }
    }

    /// <summary>Gets the tracked playback time in seconds.</summary>
    public float PlayTime => _playTime;

    /// <summary>Gets the decoded sound duration in seconds.</summary>
    public float Duration => _buffer?.Duration ?? 0f;

    /// <summary>Gets normalized playback progress from zero through one.</summary>
    public float Progress => Duration > 0f ? Math.Clamp(_playTime / Duration, 0f, 1f) : 0f;

    /// <summary>Gets the number of completed loop iterations.</summary>
    public int LoopCount => _loopCount;

    /// <summary>Gets whether the instance is currently playing.</summary>
    public bool IsPlaying => Status == SoundStatus.Playing;

    /// <summary>Gets whether the instance is currently paused.</summary>
    public bool IsPaused => Status == SoundStatus.Paused;

    /// <summary>Gets whether the instance is currently stopped.</summary>
    public bool IsStopped => Status == SoundStatus.Stopped;

    /// <summary>Gets whether the instance has entered a notified stopped/completed state.</summary>
    public bool IsComplete => IsStopped && _hasNotifiedCompletion;

    /// <summary>Gets whether the instance is initialized and not disposed.</summary>
    public bool IsValid => _isInitialized && !_isDisposed;

    /// <summary>Gets or sets the priority used by the sound pool when stealing voices.</summary>
    public SoundPriority Priority { get; set; } = SoundPriority.Normal;

    /// <summary>Gets the source asset name associated with this instance.</summary>
    public string SoundName
    {
        get => _soundName;
        internal set => _soundName = value;
    }

    /// <summary>Occurs when non-looping playback reaches the end of the sound.</summary>
    public event EventHandler<SoundCompletedEventArgs> SoundCompleted;

    /// <summary>Occurs when <see cref="Stop"/> stops the instance.</summary>
    public event EventHandler<SoundStoppedEventArgs> SoundStopped;

    /// <summary>Occurs after each completed loop iteration.</summary>
    public event EventHandler<SoundLoopedEventArgs> SoundLooped;

    /// <summary>Occurs when a playback or update operation reports an exception.</summary>
    public event EventHandler<SoundErrorEventArgs> SoundError;

    internal SoundInstance()
    {
        _status = SoundStatus.Stopped;
    }

    internal void Initialize(AudioBuffer buffer, Enum category = null, SoundPriority priority = SoundPriority.Normal)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SoundInstance));
        if (buffer == null || !buffer.IsValid)
            throw new ArgumentException("Sound buffer is null or invalid.", nameof(buffer));

        if (_isInitialized)
            Reset();

        _source = _source == 0 ? AudioRuntime.CreateSource() : _source;
        _buffer = buffer.AddReference();
        AudioRuntime.BindBuffer(_source, _buffer.Handle);

        Category = category;
        Priority = priority;
        _isInitialized = true;
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
        _status = SoundStatus.Stopped;

        AudioRuntime.SetLooping(_source, false);
        AudioRuntime.SetPitch(_source, 1f);
        AudioRuntime.SetPan(_source, 0f);
        ApplyCategoryVolume(category);
    }

    internal void Update(float deltaTime)
    {
        if (_isDisposed || !_isInitialized)
            return;

        try
        {
            if (_status == SoundStatus.Playing)
            {
                float duration = Duration;
                float effectivePitch = Math.Clamp(_pitch, 0.5f, 2f);
                _playTime += Math.Max(0f, deltaTime) * effectivePitch;

                if (_looping && duration > 0f)
                {
                    while (_playTime >= duration)
                    {
                        _playTime -= duration;
                        _loopCount++;
                        SoundLooped?.Invoke(this, new SoundLoopedEventArgs(this, _loopCount));
                    }
                }
                else if (!_looping && duration > 0f && _playTime >= duration)
                {
                    _playTime = duration;
                    _status = SoundStatus.Stopped;
                    AudioRuntime.Stop(_source);

                    if (!_hasNotifiedCompletion)
                    {
                        _hasNotifiedCompletion = true;
                        SoundCompleted?.Invoke(this, new SoundCompletedEventArgs(this, false, _loopCount));
                    }
                }
            }
            // Do not auto-notify/recycle a freshly initialized instance that
            // has not been played yet. CreateInstance() returns the instance to
            // game code before Play() is called, and the pool update thread can
            // run during that setup window.
            //
            // Explicit Stop() already raises SoundStopped synchronously, while
            // natural completion is handled above, so no generic "Stopped"
            // branch is needed here.
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Error during sound update."));
        }
    }

    /// <summary>Starts or resumes playback.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the instance has not been initialized.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public void Play()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Sound instance not initialized.");
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SoundInstance));

        try
        {
            if (_status == SoundStatus.Stopped)
                _playTime = 0f;

            _hasNotifiedCompletion = false;
            _wasPlaying = true;
            _wasPaused = false;
            AudioRuntime.SetLooping(_source, _looping);
            AudioRuntime.Play(_source);
            _status = SoundStatus.Playing;
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to play sound."));
            throw;
        }
    }

    /// <summary>Pauses playback when the instance is currently playing.</summary>
    public void Pause()
    {
        if (_isDisposed || _status != SoundStatus.Playing)
            return;

        try
        {
            AudioRuntime.Pause(_source);
            _wasPaused = true;
            _wasPlaying = false;
            _status = SoundStatus.Paused;
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to pause sound."));
        }
    }

    /// <summary>Stops playback and raises <see cref="SoundStopped"/> once for the current playback.</summary>
    public void Stop()
    {
        if (_isDisposed || !_isInitialized)
            return;

        try
        {
            bool wasPlaying = _status == SoundStatus.Playing;
            bool wasPaused = _status == SoundStatus.Paused;

            AudioRuntime.Stop(_source);
            _status = SoundStatus.Stopped;
            _playTime = 0f;

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
        try
        {
            if (_source != 0)
            {
                AudioRuntime.Stop(_source);
                AudioRuntime.SetLooping(_source, false);
                AudioRuntime.BindBuffer(_source, 0);
            }
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to reset sound."));
        }
        finally
        {
            _buffer?.Release();
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
            _status = SoundStatus.Stopped;
            Category = null;
            _isDisposed = false;
        }
    }

    /// <summary>Releases the OpenAL source and any retained audio-buffer reference.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            if (_source != 0)
            {
                AudioRuntime.Stop(_source);
                AudioRuntime.BindBuffer(_source, 0);
                AudioRuntime.DeleteSource(_source);
                _source = 0;
            }

            _buffer?.Release();
            _buffer = null;
            _isInitialized = false;
            _status = SoundStatus.Stopped;
            _isDisposed = true;
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
