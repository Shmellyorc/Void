// ============================================================================
//  Crc32.cs
// ============================================================================
//  High-performance CRC32 checksum computation using a precomputed lookup table.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Packer.Utils;

/// <summary>
/// Provides high-performance CRC32 checksum computation using a precomputed lookup table.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Crc32"/> class implements the standard CRC32 algorithm with
/// the polynomial 0xEDB88320, commonly used in ZIP, PNG, and other file formats.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Precomputed lookup table for fast computation</description></item>
///   <item><description>Hardware-accelerated with unsafe pointers for performance</description></item>
///   <item><description>Supports ReadOnlySpan for efficient memory handling</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Compute CRC32 of a byte array
/// byte[] data = Encoding.UTF8.GetBytes("Hello World");
/// uint crc = Crc32.Compute(data);
/// 
/// // Compute CRC32 of a span
/// ReadOnlySpan&lt;byte&gt; span = data;
/// uint spanCrc = Crc32.Compute(span);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. The lookup table is static and read-only.
/// </para>
/// </remarks>
public static unsafe class Crc32
{
    private static readonly uint[] Table;

    static Crc32()
    {
        Table = new uint[256];
        const uint poly = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
                crc = (crc & 1) != 0 ? poly ^ (crc >> 1) : crc >> 1;
            Table[i] = crc;
        }
    }

    /// <summary>
    /// Computes the CRC32 checksum of the specified data.
    /// </summary>
    /// <param name="data">The data to compute the checksum for.</param>
    /// <returns>The CRC32 checksum as a 32-bit unsigned integer.</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            uint crc = 0xFFFFFFFF;
            byte* current = ptr;
            byte* end = ptr + data.Length;
            var table = Table;

            while (current < end)
            {
                crc = table[(crc ^ *current) & 0xFF] ^ (crc >> 8);
                current++;
            }

            return ~crc;
        }
    }
}