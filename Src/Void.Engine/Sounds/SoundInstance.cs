namespace Void.Engine.Sounds;

public sealed class SoundInstance : IDisposable
{
    private readonly SFSound _sfmlSound;
    private SFSoundBuffer _buffer;
    private bool _isInitialized;
    private float _playTime;
    private bool _isDisposed;
    private string _soundName;
    private bool _hasNotifiedCompletion;
    private int _loopCount;
    private bool _wasPlaying;
    private bool _wasPaused;

    public SoundStatus Status
    {
        get
        {
            if (_isDisposed || !_isInitialized)
                return SoundStatus.Stopped;

            return (SoundStatus)_sfmlSound.Status;
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;
            // NOTE: if values are almost simular of est. MathHelper.Epsilon, just fail
            if (MathHelper.AlmostEquals(value, _volume, MathHelper.Epsilon))
                return;

            _volume = Math.Clamp(value, 0f, 1f);
            _sfmlSound.Volume = _volume * 100f;
        }
    }
    private float _volume = 1f;


    public float Pitch
    {
        get => _pitch;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;
            // NOTE: if values are almost simular of est. MathHelper.Epsilon, just fail
            if (MathHelper.AlmostEquals(value, _pitch, MathHelper.Epsilon))
                return;

            _pitch = Math.Clamp(value, 0.1f, 10f);
            _sfmlSound.Pitch = _pitch;
        }
    }
    private float _pitch = 1f;


    public float Pan
    {
        get => _pan;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;

            _pan = Math.Clamp(value, -1f, 1f);

            // NOTE: Near zero, should reset back to normal
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



    public bool Looping
    {
        get => _looping;
        set
        {
            if (_isDisposed || !_isInitialized)
                return;
            if (_looping == value)
                return;

            _looping = value;

            _sfmlSound.Loop = _looping;
        }
    }
    private bool _looping;


    public float PlayTime => _playTime;
    public float Duration => _buffer?.Duration.AsSeconds() ?? 0f;
    public float Progress => Duration > 0 ? Math.Clamp(_playTime / Duration, 0f, 1f) : 0f;
    public int LoopCount => _loopCount;
    public bool IsPlaying => Status == SoundStatus.Playing;
    public bool IsPaused => Status == SoundStatus.Paused;
    public bool IsStopped => Status == SoundStatus.Stopped;
    public bool IsComplete => IsStopped && _hasNotifiedCompletion;
    public bool IsValid => _isInitialized && !_isDisposed;

    public string SoundName
    {
        get => _soundName;
        internal set => _soundName = value;
    }


    public event EventHandler<SoundCompletedEventArgs> SoundCompleted;
    public event EventHandler<SoundStoppedEventArgs> SoundStopped;
    public event EventHandler<SoundLoopedEventArgs> SoundLooped;
    public event EventHandler<SoundErrorEventArgs> SoundError;


    internal SoundInstance()
    {
        _sfmlSound = new SFSound();
        _isInitialized = false;
        _playTime = 0f;
        _isDisposed = false;
        _hasNotifiedCompletion = false;
        _loopCount = 0;
        _wasPlaying = false;
        _wasPaused = false;
    }

    internal void Initialize(SFSoundBuffer buffer)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SoundInstance));

        if (_isInitialized && Status != SoundStatus.Stopped)
            Stop();

        _buffer = buffer;
        _sfmlSound.SoundBuffer = buffer;
        _isInitialized = true;
        _playTime = 0f;
        _hasNotifiedCompletion = false;
        _loopCount = 0;
        _wasPlaying = false;
        _wasPaused = false;
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
                    SoundCompleted?.Invoke(this, new SoundCompletedEventArgs(this, false, _loopCount));
                    _hasNotifiedCompletion = true;
                }
            }
            else if (Status == SoundStatus.Stopped)
            {
                if (!_hasNotifiedCompletion)
                {
                    SoundStopped?.Invoke(this, new SoundStoppedEventArgs(this, _wasPlaying, _wasPaused));
                    _hasNotifiedCompletion = true;
                }
            }
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Error during sound update."));
        }
    }

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
            _sfmlSound.Play();
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to play sound."));
            throw;
        }
    }

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
        try
        {
            _sfmlSound.Stop();
            _sfmlSound.SoundBuffer = null;
            _buffer = null;
            _isInitialized = false;
            _playTime = 0f;
            _hasNotifiedCompletion = false;
            _loopCount = 0;
            _wasPlaying = false;
            _wasPaused = false;
        }
        catch (Exception ex)
        {
            SoundError?.Invoke(this, new SoundErrorEventArgs(this, ex, "Failed to reset sound."));
        }
    }

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
            System.Diagnostics.Debug.WriteLine($"Error during SoundInstance disposable: {ex.Message}");
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }


}
