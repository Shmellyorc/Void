using System;
using System.Threading;

namespace Void.Engine.Audio;

/// <summary>
/// Reference-counted decoded OpenAL buffer. A Sound asset owns one reference and
/// each live SoundInstance owns another, so asset eviction cannot invalidate audio
/// that is already playing.
/// </summary>
internal sealed class AudioBuffer : IDisposable
{
    private uint _handle;
    private int _references = 1;
    private int _disposed;

    public uint Handle => _handle;
    public float Duration { get; }
    public int Channels { get; }
    public int SampleRate { get; }
    public bool IsValid => _handle != 0 && Volatile.Read(ref _disposed) == 0;

    private AudioBuffer(uint handle, float duration, int channels, int sampleRate)
    {
        _handle = handle;
        Duration = duration;
        Channels = channels;
        SampleRate = sampleRate;
    }

    public static AudioBuffer Create(ReadOnlySpan<byte> encodedData)
    {
        DecodedAudio decoded = AudioDecoder.Decode(encodedData);
        uint handle = AudioRuntime.CreateBuffer(decoded.Pcm16, decoded.Channels, decoded.SampleRate);
        return new AudioBuffer(handle, decoded.Duration, decoded.Channels, decoded.SampleRate);
    }

    public AudioBuffer AddReference()
    {
        if (!IsValid)
            throw new ObjectDisposedException(nameof(AudioBuffer));

        Interlocked.Increment(ref _references);
        return this;
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref _references) != 0)
            return;

        DisposeCore();
    }

    public void Dispose() => Release();

    private void DisposeCore()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        uint handle = _handle;
        _handle = 0;
        if (handle != 0)
            AudioRuntime.DeleteBuffer(handle);
    }
}
