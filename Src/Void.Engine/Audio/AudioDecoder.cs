using System;
using System.Collections.Generic;
using System.IO;
using NAudio.SoundFile;

namespace Void.Engine.Audio;

internal readonly struct DecodedAudio
{
    public readonly byte[] Pcm16;
    public readonly int Channels;
    public readonly int SampleRate;
    public readonly float Duration;

    public DecodedAudio(byte[] pcm16, int channels, int sampleRate, float duration)
    {
        Pcm16 = pcm16;
        Channels = channels;
        SampleRate = sampleRate;
        Duration = duration;
    }
}

/// <summary>
/// Central encoded-audio decoder. OpenAL never sees encoded OGG/WAV/FLAC/MP3 bytes;
/// it only receives normalized PCM16 output from this layer.
/// </summary>
internal static class AudioDecoder
{
    public static DecodedAudio Decode(ReadOnlySpan<byte> encodedData)
    {
        if (encodedData.IsEmpty)
            throw new ArgumentException("Audio data cannot be empty.", nameof(encodedData));

        using var stream = new MemoryStream(encodedData.ToArray(), writable: false);
        using var reader = new SoundFileReader(stream);

        int sourceChannels = reader.WaveFormat.Channels;
        int sampleRate = reader.WaveFormat.SampleRate;

        if (sourceChannels <= 0 || sampleRate <= 0)
            throw new InvalidDataException("Decoded audio has an invalid channel count or sample rate.");

        // Standard OpenAL guarantees mono/stereo PCM. Preserve mono/stereo sources;
        // down-mix wider layouts to mono so the backend remains portable.
        int outputChannels = sourceChannels <= 2 ? sourceChannels : 1;
        var output = new List<short>();
        var input = new float[Math.Max(8192, sourceChannels * 1024)];

        int read;
        while ((read = reader.Read(input.AsSpan())) > 0)
        {
            if (sourceChannels == 1 || sourceChannels == 2)
            {
                for (int i = 0; i < read; i++)
                    output.Add(FloatToPcm16(input[i]));
            }
            else
            {
                int frameCount = read / sourceChannels;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int baseIndex = frame * sourceChannels;
                    float mixed = 0f;
                    for (int channel = 0; channel < sourceChannels; channel++)
                        mixed += input[baseIndex + channel];

                    output.Add(FloatToPcm16(mixed / sourceChannels));
                }
            }
        }

        short[] samples = output.ToArray();
        byte[] pcm = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, pcm, 0, pcm.Length);

        int frameTotal = outputChannels > 0 ? samples.Length / outputChannels : 0;
        float duration = sampleRate > 0 ? (float)frameTotal / sampleRate : 0f;

        return new DecodedAudio(pcm, outputChannels, sampleRate, duration);
    }

    private static short FloatToPcm16(float value)
    {
        value = Math.Clamp(value, -1f, 1f);
        return value >= 0f
            ? (short)MathF.Round(value * short.MaxValue)
            : (short)MathF.Round(value * -short.MinValue);
    }
}
