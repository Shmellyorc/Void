using System.Text;

namespace Void.Packer;

public static class PackConstants
{
    // Bootstrap Header (unencrypted, fixed size)
    public const int MagicOffset = 0;
    public const int MagicSize = 4;                             // "SPAC"

    public const int VersionOffset = 4;
    public const int VersionSize = 2;                           // ushort

    public const int FlagOffset = 6;
    public const int FlagSize = 1;                              // byte

    public const int HeaderEncryptedSizeOffset = 7;
    public const int HeaderEncryptedSizeSize = 4;               // uint

    public const int DataEncryptedSizeOffset = 11;
    public const int DataEncryptedSizeSize = 4;                 // uint

    public const int FileCountOffset = 15;
    public const int FileCountSize = 2;                         // ushort

    public const int NonceOffset = 17;
    public const int NonceSize = 12;                            // AES-GCM nonce (96 bits)

    public const int AlgorithmOffset = 29;
    public const int AlgorithmSize = 1;                         // CompressionAlgorithm byte

    public const int ReservedOffset = 30;
    public const int ReservedSize = 2;                          // Padding to 32 bytes

    // Total bootstrap header size (fixed, never changes)
    public const int BootstrapHeaderSize = 32;                  // 4+2+1+4+4+2+12+1+2=32

    // Header Block starts immediately after bootstrap
    public const int HeaderBlockOffset = BootstrapHeaderSize;

    // Header Block structure (inside encrypted header, before file table)
    public const int HeaderVersionOffset = 0;
    public const int HeaderVersionSize = 2;                     // ushort

    public const int HeaderFileTableOffsetOffset = 2;
    public const int HeaderFileTableOffsetSize = 4;             // uint

    public const int HeaderFileCountOffset = 6;
    public const int HeaderFileCountSize = 2;                   // ushort

    public const int HeaderCompressionOffset = 8;
    public const int HeaderCompressionSize = 1;                 // byte

    public const int HeaderReservedOffset = 9;
    public const int HeaderReservedSize = 3;                    // bytes

    // Total header block fixed size (before variable file table)
    public const int HeaderBlockFixedSize = 12;                 // 2+4+2+1+3 = 12

    // File Entry (within decrypted header), fixed offsets
    public const int FileEntryPathLengthOffset = 0;
    public const int FileEntryPathLengthSize = 2;               // ushort

    public const int FileEntryOffsetInDataOffset = 2;
    public const int FileEntryOffsetInDataSize = 4;             // uint

    public const int FileEntryUncompressedSizeOffset = 6;
    public const int FileEntryUncompressedSizeSize = 4;         // uint

    public const int FileEntryStoredSizeOffset = 10;
    public const int FileEntryStoredSizeSize = 4;               // uint

    public const int FileEntryFlagsOffset = 14;
    public const int FileEntryFlagsSize = 1;                    // byte

    public const int FileEntryCRC32Offset = 15;
    public const int FileEntryCRC32Size = 4;                    // uint

    // Fixed size before variable path
    public const int FileEntryFixedSize = 19;                   // 2+4+4+4+1+4 = 19

    // Magic bytes
    public static readonly byte[] MagicBytes = Encoding.UTF8.GetBytes("SPAC");

    public const ushort CurrentVersion = 2;

    // Flag bits
    public const byte FlagHeaderEncrypted = 0x01;
    public const byte FlagHeaderCompressed = 0x02;
    public const byte FlagFileEncrypted = 0x01;
    public const byte FlagFileCompressed = 0x02;
}