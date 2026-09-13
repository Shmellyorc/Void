// ============================================================================
//  HashHelper.cs
// ============================================================================
//  FNV-1a hashing helpers with optional string-result caches.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Helpers;

/// <summary>
/// Provides 32-bit and 64-bit FNV-1a hashing for byte data and UTF-8 strings.
/// </summary>
/// <remarks>
/// <para>
/// The string cache overloads retain computed results for repeated string keys.
/// Direct hash overloads are allocation-conscious: small strings use stack storage
/// and larger strings rent temporary buffers from <see cref="ArrayPool{T}"/>.
/// </para>
/// <para>
/// FNV-1a is a non-cryptographic hash. Do not use these methods for passwords,
/// signatures, authentication, or other security-sensitive hashing.
/// </para>
/// <code>
/// uint id = HashHelper.Cache32("player.spawn");
/// ulong contentId = HashHelper.Hash64(data);
/// </code>
/// </remarks>
public sealed class HashHelper
{
    private const uint Prime32 = 16777619, OffsetBasis32 = 2166136261;
    private const ulong Prime64 = 1099511628211, OffsetBasis64 = 14695981039346656037;

    private static readonly ConcurrentDictionary<string, Lazy<uint>> _cache32 = [];
    private static readonly ConcurrentDictionary<string, Lazy<ulong>> _cache64 = [];

    /// <summary>Gets a cached 32-bit FNV-1a hash for a string.</summary>
    /// <param name="input">String to encode as UTF-8 and hash.</param>
    /// <returns>The cached 32-bit hash.</returns>
    public static uint Cache32(string input)
        => _cache32.GetOrAdd(input, new Lazy<uint>(() => Hash32(input))).Value;

    /// <summary>
    /// Computes a 32-bit FNV-1a hash for an enum's stable enum-string representation.
    /// </summary>
    /// <param name="input">Enum value to hash.</param>
    /// <returns>The 32-bit hash.</returns>
    /// <remarks>
    /// This overload currently computes the hash directly rather than storing it in
    /// the string cache used by <see cref="Cache32(string)"/>.
    /// </remarks>
    public static uint Cache32(Enum input) => Cache32(input.ToEnumString());

    /// <summary>Gets a cached 64-bit FNV-1a hash for a string.</summary>
    /// <param name="input">String to encode as UTF-8 and hash.</param>
    /// <returns>The cached 64-bit hash.</returns>
    public static ulong Cache64(string input)
        => _cache64.GetOrAdd(input, new Lazy<ulong>(() => Hash64(input))).Value;

    /// <summary>Gets a cached 64-bit FNV-1a hash for an enum's stable enum-string representation.</summary>
    /// <param name="input">Enum value to hash.</param>
    /// <returns>The cached 64-bit hash.</returns>
    public static ulong Cache64(Enum input) => Cache64(input.ToEnumString());

    /// <summary>Computes a 32-bit FNV-1a hash from bytes.</summary>
    /// <param name="data">Bytes to hash.</param>
    /// <returns>The 32-bit hash.</returns>
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

    /// <summary>Computes a 32-bit FNV-1a hash from a byte array.</summary>
    /// <param name="data">Bytes to hash.</param>
    /// <returns>The 32-bit hash.</returns>
    public static uint Hash32(byte[] data)
        => Hash32((ReadOnlySpan<byte>)data);

    /// <summary>Computes a 32-bit FNV-1a hash from the UTF-8 bytes of a string.</summary>
    /// <param name="data">String to hash.</param>
    /// <returns>The 32-bit hash.</returns>
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

    /// <summary>Computes a 64-bit FNV-1a hash from bytes.</summary>
    /// <param name="data">Bytes to hash.</param>
    /// <returns>The 64-bit hash.</returns>
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

    /// <summary>Computes a 64-bit FNV-1a hash from a byte array.</summary>
    /// <param name="data">Bytes to hash.</param>
    /// <returns>The 64-bit hash.</returns>
    public static ulong Hash64(byte[] data)
        => Hash64((ReadOnlySpan<byte>)data);

    /// <summary>Computes a 64-bit FNV-1a hash from the UTF-8 bytes of a string.</summary>
    /// <param name="data">String to hash.</param>
    /// <returns>The 64-bit hash.</returns>
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
