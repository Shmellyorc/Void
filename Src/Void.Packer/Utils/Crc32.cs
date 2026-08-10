using System;

namespace Void.Packer.Utils;

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