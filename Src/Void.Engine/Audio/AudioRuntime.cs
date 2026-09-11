using System;
using Silk.NET.OpenAL;

namespace Void.Engine.Audio;

/// <summary>
/// Internal OpenAL device/context owner. All AL calls are serialized and the context
/// is made current only for the duration of a call so the sound-pool worker thread
/// and the game thread can safely share the same device.
/// </summary>
internal static unsafe class AudioRuntime
{
    private static readonly object Sync = new();

    private static AL _al;
    private static ALContext _alc;
    private static Device* _device;
    private static Context* _context;
    private static bool _initialized;
    private static bool _shutdown;

    static AudioRuntime()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
    }

    public static uint CreateBuffer(ReadOnlySpan<byte> pcm16, int channels, int sampleRate)
    {
        if (pcm16.IsEmpty)
            throw new ArgumentException("PCM data cannot be empty.", nameof(pcm16));
        if (channels is not (1 or 2))
            throw new NotSupportedException($"OpenAL PCM upload currently supports mono or stereo data, not {channels} channels.");

        lock (Sync)
        {
            EnsureInitializedLocked();
            MakeCurrentLocked();
            try
            {
                uint buffer = _al.GenBuffer();
                BufferFormat format = channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;
                fixed (byte* ptr = pcm16)
                    _al.BufferData(buffer, format, ptr, pcm16.Length, sampleRate);
                return buffer;
            }
            finally
            {
                ClearCurrentLocked();
            }
        }
    }

    public static void DeleteBuffer(uint buffer)
    {
        if (buffer == 0)
            return;

        Execute(al => al.DeleteBuffer(buffer));
    }

    public static uint CreateSource()
    {
        return Execute(al =>
        {
            uint source = al.GenSource();
            // Panning uses source position; disable distance attenuation so changing
            // Pan cannot unexpectedly change the perceived volume.
            al.SetSourceProperty(source, SourceFloat.RolloffFactor, 0f);
            return source;
        });
    }

    public static void DeleteSource(uint source)
    {
        if (source == 0)
            return;
        Execute(al => al.DeleteSource(source));
    }

    public static void BindBuffer(uint source, uint buffer)
        => Execute(al => al.SetSourceProperty(source, SourceInteger.Buffer, unchecked((int)buffer)));

    public static void SetGain(uint source, float gain)
        => Execute(al => al.SetSourceProperty(source, SourceFloat.Gain, Math.Max(0f, gain)));

    public static void SetPitch(uint source, float pitch)
        => Execute(al => al.SetSourceProperty(source, SourceFloat.Pitch, Math.Clamp(pitch, 0.5f, 2f)));

    public static void SetLooping(uint source, bool looping)
        => Execute(al => al.SetSourceProperty(source, SourceBoolean.Looping, looping));

    public static void SetPan(uint source, float pan)
    {
        pan = Math.Clamp(pan, -1f, 1f);
        Execute(al =>
        {
            bool relative = MathF.Abs(pan) > 0.0001f;
            al.SetSourceProperty(source, SourceBoolean.SourceRelative, relative);
            al.SetSourceProperty(source, SourceVector3.Position, relative ? pan : 0f, 0f, 0f);
        });
    }

    public static void Play(uint source) => Execute(al => al.SourcePlay(source));
    public static void Pause(uint source) => Execute(al => al.SourcePause(source));
    public static void Stop(uint source) => Execute(al => al.SourceStop(source));

    public static void Shutdown()
    {
        lock (Sync)
        {
            if (!_initialized || _shutdown)
                return;

            _shutdown = true;
            try
            {
                _alc.MakeContextCurrent(_context);
            }
            catch
            {
                // Best-effort shutdown.
            }

            try { _alc.MakeContextCurrent(null); } catch { }
            try { if (_context != null) _alc.DestroyContext(_context); } catch { }
            try { if (_device != null) _alc.CloseDevice(_device); } catch { }
            try { _al?.Dispose(); } catch { }
            try { _alc?.Dispose(); } catch { }

            _context = null;
            _device = null;
            _al = null;
            _alc = null;
            _initialized = false;
        }
    }

    private static void Execute(Action<AL> action)
    {
        lock (Sync)
        {
            EnsureInitializedLocked();
            MakeCurrentLocked();
            try
            {
                action(_al);
            }
            finally
            {
                ClearCurrentLocked();
            }
        }
    }

    private static T Execute<T>(Func<AL, T> action)
    {
        lock (Sync)
        {
            EnsureInitializedLocked();
            MakeCurrentLocked();
            try
            {
                return action(_al);
            }
            finally
            {
                ClearCurrentLocked();
            }
        }
    }

    private static void EnsureInitializedLocked()
    {
        if (_initialized)
            return;
        if (_shutdown)
            throw new ObjectDisposedException(nameof(AudioRuntime));

        _alc = ALContext.GetApi();
        _al = AL.GetApi();
        _device = _alc.OpenDevice("");
        if (_device == null)
            throw new InvalidOperationException("OpenAL could not open the default audio device.");

        _context = _alc.CreateContext(_device, null);
        if (_context == null)
        {
            _alc.CloseDevice(_device);
            _device = null;
            throw new InvalidOperationException("OpenAL could not create an audio context.");
        }

        _alc.MakeContextCurrent(_context);
        _al.GetError(); // clear any pre-existing error state
        _alc.MakeContextCurrent(null);
        _initialized = true;
    }

    private static void MakeCurrentLocked()
        => _alc.MakeContextCurrent(_context);

    private static void ClearCurrentLocked()
        => _alc.MakeContextCurrent(null);
}
