using System;
using System.IO;
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
        var compressionLevel = level switch
        {
            1 => CompressionLevel.Fastest,
            2 => CompressionLevel.Fastest,
            3 => CompressionLevel.Fastest,
            <= 5 => CompressionLevel.Optimal,
            9 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };

        if (algorithm == CompressionAlgorithm.Deflate)
        {
            using var outputStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(outputStream, compressionLevel))
            {
                deflateStream.Write(data);
            }
            return outputStream.ToArray();
        }
        else if (algorithm == CompressionAlgorithm.Brotli)
        {
            using var outputStream = new MemoryStream();
            using (var brotliStream = new BrotliStream(outputStream, compressionLevel))
            {
                brotliStream.Write(data);
            }
            return outputStream.ToArray();
        }

        throw new NotSupportedException($"Compression algorithm {algorithm} not supported.");
    }

    private static byte[] DecompressInternal(ReadOnlySpan<byte> data, int uncompressedSize, CompressionAlgorithm algorithm)
    {
        var result = new byte[uncompressedSize];

        using var inputStream = new MemoryStream(data.ToArray());

        Stream decompressionStream = algorithm switch
        {
            CompressionAlgorithm.Deflate => new DeflateStream(inputStream, CompressionMode.Decompress),
            CompressionAlgorithm.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress),
            _ => throw new NotSupportedException($"Compression algorithm {algorithm} not supported.")
        };

        using (decompressionStream)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < uncompressedSize)
            {
                int bytesRead = decompressionStream.Read(result, totalBytesRead, uncompressedSize - totalBytesRead);
                if (bytesRead == 0)
                    break;
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead != uncompressedSize)
                throw new InvalidDataException($"Decompressed size mismatch. Expected {uncompressedSize}, got {totalBytesRead}");
        }

        return result;
    }
}