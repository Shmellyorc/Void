using System;
using System.IO.Compression;

namespace Void.Packer.Encryption;

public enum CompressionAlgorithm : byte
{
    None,
    Deflate,
    Brotli
}

public static class AdaptiveCompressor
{
    public static (byte[] Data, bool Compressed) Compress(ReadOnlySpan<byte> data, CompressionAlgorithm algorithm, int level = 6)
    {
        if (data.Length == 0)
            return (Array.Empty<byte>(), false);

        if (data.Length < 128)
            return (data.ToArray(), false);

        byte[] compressed = CompressInternal(data, algorithm, level);

        if (compressed.Length < data.Length)
            return (compressed, true);

        return (data.ToArray(), false);
    }

    public static byte[] Decompress(ReadOnlySpan<byte> data, int uncompressedSize, CompressionAlgorithm algorithm)
    {
        if (algorithm == CompressionAlgorithm.None)
            return data.ToArray();

        return DecompressInternal(data, uncompressedSize, algorithm);
    }

    private static byte[] CompressInternal(ReadOnlySpan<byte> data, CompressionAlgorithm algorithm, int level)
    {
        using var outputStream = new MemoryStream();

        Stream compressionStream = algorithm switch
        {
            CompressionAlgorithm.Deflate => new DeflateStream(outputStream, (CompressionLevel)level, true),
            CompressionAlgorithm.Brotli => new BrotliStream(outputStream, (CompressionLevel)level, true),
            _ => throw new NotSupportedException($"Compression algorithm {algorithm} not supported.")
        };

        using (compressionStream)
        {
            compressionStream.Write(data);
        }

        return outputStream.ToArray();
    }

    private static byte[] DecompressInternal(ReadOnlySpan<byte> data, int uncompressedSize, CompressionAlgorithm algorithm)
    {
        using var inputStream = new MemoryStream(data.ToArray());
        using var outputStream = new MemoryStream(uncompressedSize);

        Stream decompressionStream = algorithm switch
        {
            CompressionAlgorithm.Deflate => new DeflateStream(inputStream, CompressionMode.Decompress, true),
            CompressionAlgorithm.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress, true),
            _ => throw new NotSupportedException($"Compression algorithm {algorithm} not supported.")
        };

        using (decompressionStream)
        {
            decompressionStream.CopyTo(outputStream);
        }

        return outputStream.ToArray();
    }
}