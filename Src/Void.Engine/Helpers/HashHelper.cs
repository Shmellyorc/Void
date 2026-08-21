// ============================================================================
//  HashHelper.cs
// ============================================================================
//  High-performance FNV-1a hashing utilities with caching support for
//  both 32-bit and 64-bit hash values.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Helpers;

/// <summary>
/// Provides high-performance FNV-1a hashing utilities with caching support
/// for both 32-bit and 64-bit hash values.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HashHelper"/> class implements the FNV-1a (Fowler-Noll-Vo)
/// non-cryptographic hash algorithm, which is fast and produces high-quality
/// hash values suitable for hash tables, dictionaries, and other data
/// structures.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item><description>32-bit and 64-bit hash variants</description></item>
///   <item><description>Caching for frequently accessed strings</description></item>
///   <item><description>Stack allocation for small strings (≤256 characters)</description></item>
///   <item><description>Array pool usage for large strings to minimize allocations</description></item>
///   <item><description>Enum support via <see cref="EnumHelper.ToEnumString"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Hash a string without caching
/// uint hash32 = HashHelper.Hash32("Hello World");
/// ulong hash64 = HashHelper.Hash64("Hello World");
/// 
/// // Hash with caching (recommended for repeated hashing)
/// uint cached32 = HashHelper.Cache32("Hello World");
/// ulong cached64 = HashHelper.Cache64("Hello World");
/// 
/// // Hash an enum
/// uint enumHash = HashHelper.Cache32(MyEnum.Value);
/// 
/// // Hash byte data
/// byte[] data = Encoding.UTF8.GetBytes("Hello");
/// uint dataHash = HashHelper.Hash32(data);
/// </code>
/// </para>
/// <para>
/// <b>Performance Notes:</b>
/// <list type="bullet">
///   <item><description>FNV-1a is optimized for speed over cryptographic security</description></item>
///   <item><description>Caching uses <see cref="Lazy{T}"/> for thread-safe deferred computation</description></item>
///   <item><description>Small strings are processed on the stack to avoid heap allocations</description></item>
///   <item><description>Large strings use the ArrayPool to reduce GC pressure</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. All methods use concurrent collections or
/// are stateless.
/// </para>
/// </remarks>
public sealed class HashHelper
{
    private const uint Prime32 = 16777619, OffsetBasis32 = 2166136261;
    private const ulong Prime64 = 1099511628211, OffsetBasis64 = 14695981039346656037;

    private static readonly ConcurrentDictionary<string, Lazy<uint>> _cache32 = [];
    private static readonly ConcurrentDictionary<string, Lazy<ulong>> _cache64 = [];

    /// <summary>
    /// Gets a cached 32-bit hash for the specified input string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>The cached 32-bit hash value.</returns>
    public static uint Cache32(string input)
        => _cache32.GetOrAdd(input, new Lazy<uint>(() => Hash32(input))).Value;

    /// <summary>
    /// Gets a cached 32-bit hash for the specified enum.
    /// </summary>
    /// <param name="input">The enum to hash.</param>
    /// <returns>The cached 32-bit hash value.</returns>
    public static uint Cache32(Enum input) => Hash32(input.ToEnumString());

    /// <summary>
    /// Gets a cached 64-bit hash for the specified input string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>The cached 64-bit hash value.</returns>
    public static ulong Cache64(string input)
        => _cache64.GetOrAdd(input, new Lazy<ulong>(() => Hash64(input))).Value;

    /// <summary>
    /// Gets a cached 64-bit hash for the specified enum.
    /// </summary>
    /// <param name="input">The enum to hash.</param>
    /// <returns>The cached 64-bit hash value.</returns>
    public static ulong Cache64(Enum input) => Cache64(input.ToEnumString());

    /// <summary>
    /// Computes a 32-bit FNV-1a hash from a span of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
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

    /// <summary>
    /// Computes a 32-bit FNV-1a hash from a byte array.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
    public static uint Hash32(byte[] data)
        => Hash32((ReadOnlySpan<byte>)data);

    /// <summary>
    /// Computes a 32-bit FNV-1a hash from a string.
    /// </summary>
    /// <param name="data">The string to hash.</param>
    /// <returns>The 32-bit hash value.</returns>
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

    /// <summary>
    /// Computes a 64-bit FNV-1a hash from a span of bytes.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
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

    /// <summary>
    /// Computes a 64-bit FNV-1a hash from a byte array.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    public static ulong Hash64(byte[] data)
        => Hash64((ReadOnlySpan<byte>)data);

    /// <summary>
    /// Computes a 64-bit FNV-1a hash from a string.
    /// </summary>
    /// <param name="data">The string to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
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