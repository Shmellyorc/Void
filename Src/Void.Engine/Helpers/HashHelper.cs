

namespace Void.Engine.Helpers;

public sealed class HashHelper
{
    private const uint Prime32 = 16777619, OffsetBasis32 = 2166136261;
    private const ulong Prime64 = 1099511628211, OffsetBasis64 = 14695981039346656037;

    private static readonly ConcurrentDictionary<string, Lazy<uint>> _cache32 = [];
    private static readonly ConcurrentDictionary<string, Lazy<ulong>> _cache64 = [];


    public static uint Cache32(string input)
        => _cache32.GetOrAdd(input, new Lazy<uint>(() => Hash32(input))).Value;

    public static ulong Cache64(string input)
        => _cache64.GetOrAdd(input, new Lazy<ulong>(() => Hash64(input))).Value;


    public static uint Hash32(ReadOnlySpan<byte> data)
    {
        var hash = OffsetBasis32;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= Prime32;
        }

        return hash;
    }

    public static uint Hash32(byte[] data)
        => Hash32((ReadOnlySpan<byte>)data);

    public static uint Hash32(string data)
    {
        if (data.Length <= 256)
        {
            Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetMaxByteCount(data.Length)];
            int bytesWritten = Encoding.UTF8.GetBytes(data, buffer);
            return Hash32(buffer[..bytesWritten]);
        }
        else
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(data.Length));

            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(data, buffer);
                return Hash32(buffer.AsSpan(0, bytesWritten));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    public static ulong Hash64(ReadOnlySpan<byte> data)
    {
        var hash = OffsetBasis64;

        foreach (var b in data)
        {
            hash ^= b;
            hash *= Prime64;
        }

        return hash;
    }

    public static ulong Hash64(byte[] data)
        => Hash64((ReadOnlySpan<byte>)data);

    public static ulong Hash64(string data)
    {
        if (data.Length <= 256)
        {
            Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetMaxByteCount(data.Length)];
            int bytesWritten = Encoding.UTF8.GetBytes(data, buffer);
            return Hash64(buffer[..bytesWritten]);
        }
        else
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(data.Length));

            try
            {
                var bytesWritten = Encoding.UTF8.GetBytes(data, buffer);
                return Hash64(buffer.AsSpan(0, bytesWritten));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
