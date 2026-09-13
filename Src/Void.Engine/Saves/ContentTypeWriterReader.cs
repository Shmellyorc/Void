// ============================================================================
//  ContentTypeWriterReader.cs
// ============================================================================
//  Versioned save/load base with optional encryption, compression, and manifest checks.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Void.Engine.Saves;

/// <summary>Identifies failures reported by save and load operations.</summary>
public enum SaveError
{
    /// <summary>No error occurred.</summary>
    None = 0,

    /// <summary>The supplied save path is invalid or escapes the save folder.</summary>
    InvalidPath,

    /// <summary>The save file does not use the <c>.sav</c> extension.</summary>
    InvalidExtension,

    /// <summary>The save file could not be written.</summary>
    WriteFailed,

    /// <summary>The save could not be written because the destination ran out of space.</summary>
    OutOfSpace,

    /// <summary>The application data could not be serialized.</summary>
    SerializationFailed,

    /// <summary>The save payload could not be encrypted.</summary>
    EncryptionFailed,

    /// <summary>The requested save file does not exist.</summary>
    FileNotFound,

    /// <summary>The file does not contain VOID's save-file signature.</summary>
    WrongMagic,

    /// <summary>The save was written for a different application version.</summary>
    VersionMismatch,

    /// <summary>The encrypted payload could not be authenticated or decrypted with the configured key.</summary>
    WrongKey,

    /// <summary>The save structure or payload is malformed, incomplete, or otherwise unreadable.</summary>
    CorruptData,

    /// <summary>The stored manifest does not match the order or types read by the current schema.</summary>
    ManifestMismatch,

    /// <summary>An unclassified save/load error occurred.</summary>
    Unknown
}

internal enum WriteType : byte
{
    None = 0,
    String = 1,
    Int32 = 2,
    Single = 3,
    Boolean = 4,
    Byte = 5,
    Int64 = 6,
    Double = 7,
    Vect2 = 8,
    Rect2 = 9,
    Color = 10,
    Object = 11
}

/// <summary>
/// Provides versioned save/load handling for a game-defined data type.
/// </summary>
/// <typeparam name="T">The data type written to and read from each save.</typeparam>
/// <remarks>
/// <para>
/// Derive from this class and implement <see cref="Write"/> and <see cref="Read"/>.
/// Saves use the application's current version hash, optional AES-GCM encryption,
/// optional Deflate compression, and a type manifest that verifies schema order.
/// </para>
/// <para>
/// Save names are resolved beneath <see cref="SaveFolder"/>. Relative subfolders
/// are allowed, but paths that resolve outside that folder are rejected.
/// </para>
/// <para>
/// The encrypted and unencrypted file layout remains the same regardless of the
/// concrete save-data type. Changing the application's version intentionally makes
/// older saves return <see cref="SaveError.VersionMismatch"/>.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// public sealed class PlayerSaveSystem : ContentTypeWriterReader&lt;PlayerSave&gt;
/// {
///     protected override void Write(PlayerSave data, ContentWriter writer)
///     {
///         writer.Write(data.Name);
///         writer.Write(data.Level);
///         writer.Write(data.Position);
///     }
///
///     protected override PlayerSave Read(ContentReader reader)
///     {
///         return new PlayerSave
///         {
///             Name = reader.ReadString(),
///             Level = reader.ReadInt32(),
///             Position = reader.ReadVect2()
///         };
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class ContentTypeWriterReader<T>
{
    private const string Magic = "VOID";
    private const string FileExtension = ".sav";

    private readonly byte[] _encryptionKey;
    private readonly string _saveFolder;

    /// <summary>Gets the full directory used by this save handler.</summary>
    public string SaveFolder => _saveFolder;

    /// <summary>Creates a save handler without payload encryption.</summary>
    /// <exception cref="InvalidOperationException">No active <see cref="Game"/> instance exists.</exception>
    protected ContentTypeWriterReader()
    {
        _saveFolder = GetApplicationSaveFolder();
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>Creates a save handler using a UTF-8 encryption password.</summary>
    /// <param name="encryptionKey">The password used to derive the AES-GCM key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="encryptionKey"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No active <see cref="Game"/> instance exists.</exception>
    protected ContentTypeWriterReader(string encryptionKey)
    {
        ArgumentNullException.ThrowIfNull(encryptionKey);

        _encryptionKey = Encoding.UTF8.GetBytes(encryptionKey);
        _saveFolder = GetApplicationSaveFolder();
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>Creates a save handler using the supplied encryption key material.</summary>
    /// <param name="encryptionKey">Bytes used as PBKDF2 input when deriving the AES-GCM key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="encryptionKey"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No active <see cref="Game"/> instance exists.</exception>
    protected ContentTypeWriterReader(byte[] encryptionKey)
    {
        ArgumentNullException.ThrowIfNull(encryptionKey);

        _encryptionKey = (byte[])encryptionKey.Clone();
        _saveFolder = GetApplicationSaveFolder();
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>Saves data and throws when the operation fails.</summary>
    /// <param name="fileName">Relative save name ending in <c>.sav</c>.</param>
    /// <param name="data">The data to save.</param>
    /// <exception cref="InvalidOperationException">The save operation failed.</exception>
    public void Save(string fileName, T data)
    {
        if (!TrySave(fileName, data, out SaveError error))
            throw new InvalidOperationException($"Save failed: {error}");
    }

    /// <summary>Loads data and throws when the operation fails.</summary>
    /// <param name="fileName">Relative save name ending in <c>.sav</c>.</param>
    /// <returns>The loaded value.</returns>
    /// <exception cref="InvalidOperationException">The load operation failed.</exception>
    public T Load(string fileName)
    {
        if (!TryLoad(fileName, out T data, out SaveError error))
            throw new InvalidOperationException($"Load failed: {error}");

        return data;
    }

    /// <summary>Attempts to save data without throwing for normal save failures.</summary>
    /// <param name="fileName">Relative save name ending in <c>.sav</c>.</param>
    /// <param name="data">The data to save.</param>
    /// <param name="error">Receives the failure reason, or <see cref="SaveError.None"/> on success.</param>
    /// <returns><see langword="true"/> when the save completes successfully.</returns>
    public bool TrySave(string fileName, T data, out SaveError error)
    {
        error = SaveError.None;
        string tempPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = SaveError.InvalidPath;
                return false;
            }

            if (!fileName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                error = SaveError.InvalidExtension;
                return false;
            }

            string fullPath;
            try
            {
                fullPath = GetSafePath(fileName);
            }
            catch
            {
                error = SaveError.InvalidPath;
                return false;
            }

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            tempPath = fullPath + ".tmp";

            byte[] innerData;
            using (var innerStream = new MemoryStream())
            using (var writer = new ContentWriter(innerStream))
            {
                Write(data, writer);
                writer.Flush();

                WriteType[] manifest = writer.Manifest;
                byte[] rawData = innerStream.ToArray();

                using var combinedStream = new MemoryStream();
                using var combinedWriter = new BinaryWriter(combinedStream);
                combinedWriter.Write(manifest.Length);
                foreach (WriteType type in manifest)
                    combinedWriter.Write((byte)type);
                combinedWriter.Write(rawData.Length);
                combinedWriter.Write(rawData);
                combinedWriter.Flush();
                innerData = combinedStream.ToArray();
            }

            bool compressed = false;
            byte[] dataToWrite = innerData;
            byte[] compressedData = Compress(innerData);

            if (compressedData.Length < innerData.Length)
            {
                dataToWrite = compressedData;
                compressed = true;
            }

            string version = GameSettings.Instance.AppVersion;
            ulong versionHash = HashHelper.Cache64(version);
            bool encrypted = _encryptionKey != null;
            byte[] finalData = encrypted
                ? Encrypt(dataToWrite, Magic, versionHash)
                : dataToWrite;

            using (var fileStream = File.Create(tempPath))
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write(Encoding.ASCII.GetBytes(Magic));
                binaryWriter.Write(versionHash);
                binaryWriter.Write((byte)(encrypted ? 1 : 0));
                binaryWriter.Write((byte)(compressed ? 1 : 0));
                binaryWriter.Write(finalData.Length);
                binaryWriter.Write(finalData);
                binaryWriter.Flush();
            }

            File.Move(tempPath, fullPath, overwrite: true);
            tempPath = null;
            return true;
        }
        catch (CryptographicException)
        {
            error = SaveError.EncryptionFailed;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            error = SaveError.WriteFailed;
            return false;
        }
        catch (IOException ex)
        {
            error = IsOutOfSpace(ex) ? SaveError.OutOfSpace : SaveError.WriteFailed;
            return false;
        }
        catch
        {
            error = SaveError.SerializationFailed;
            return false;
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempPath))
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Best-effort cleanup must not replace the original save error.
                }
            }
        }
    }

    /// <summary>Attempts to load data without throwing for normal load failures.</summary>
    /// <param name="fileName">Relative save name ending in <c>.sav</c>.</param>
    /// <param name="data">Receives the loaded value on success; otherwise <see langword="default"/>.</param>
    /// <param name="error">Receives the failure reason, or <see cref="SaveError.None"/> on success.</param>
    /// <returns><see langword="true"/> when the save is valid and was read successfully.</returns>
    public bool TryLoad(string fileName, out T data, out SaveError error)
    {
        data = default;
        error = SaveError.None;

        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = SaveError.InvalidPath;
                return false;
            }

            if (!fileName.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                error = SaveError.InvalidExtension;
                return false;
            }

            string fullPath;
            try
            {
                fullPath = GetSafePath(fileName);
            }
            catch
            {
                error = SaveError.InvalidPath;
                return false;
            }

            if (!File.Exists(fullPath))
            {
                error = SaveError.FileNotFound;
                return false;
            }

            byte[] fileData = File.ReadAllBytes(fullPath);
            using var fileStream = new MemoryStream(fileData, writable: false);
            using var binaryReader = new BinaryReader(fileStream);

            byte[] magicBytes = ReadExactBytes(binaryReader, Magic.Length);
            string magic = Encoding.ASCII.GetString(magicBytes);
            if (!string.Equals(magic, Magic, StringComparison.Ordinal))
            {
                error = SaveError.WrongMagic;
                return false;
            }

            ulong savedVersionHash = binaryReader.ReadUInt64();
            ulong currentVersionHash = HashHelper.Cache64(GameSettings.Instance.AppVersion);
            if (savedVersionHash != currentVersionHash)
            {
                error = SaveError.VersionMismatch;
                return false;
            }

            byte encryptedFlag = binaryReader.ReadByte();
            byte compressedFlag = binaryReader.ReadByte();
            if (encryptedFlag > 1 || compressedFlag > 1)
            {
                error = SaveError.CorruptData;
                return false;
            }

            bool encrypted = encryptedFlag == 1;
            bool compressed = compressedFlag == 1;

            int blobLength = binaryReader.ReadInt32();
            byte[] blob = ReadExactBytes(binaryReader, blobLength);
            if (fileStream.Position != fileStream.Length)
            {
                error = SaveError.CorruptData;
                return false;
            }

            byte[] decryptedBlob = blob;
            if (encrypted)
            {
                if (_encryptionKey == null)
                {
                    error = SaveError.WrongKey;
                    return false;
                }

                try
                {
                    decryptedBlob = Decrypt(blob, Magic, savedVersionHash);
                }
                catch (CryptographicException)
                {
                    error = SaveError.WrongKey;
                    return false;
                }
                catch (InvalidDataException)
                {
                    error = SaveError.CorruptData;
                    return false;
                }
            }

            byte[] innerData = decryptedBlob;
            if (compressed)
            {
                try
                {
                    innerData = Decompress(decryptedBlob);
                }
                catch
                {
                    error = SaveError.CorruptData;
                    return false;
                }
            }

            using var innerStream = new MemoryStream(innerData, writable: false);
            using var innerReader = new BinaryReader(innerStream);

            int manifestLength = innerReader.ReadInt32();
            if (manifestLength < 0 || manifestLength > innerStream.Length - innerStream.Position - sizeof(int))
            {
                error = SaveError.CorruptData;
                return false;
            }

            var manifest = new WriteType[manifestLength];
            for (int i = 0; i < manifestLength; i++)
            {
                byte rawType = innerReader.ReadByte();
                if (rawType == (byte)WriteType.None || rawType > (byte)WriteType.Object)
                {
                    error = SaveError.CorruptData;
                    return false;
                }

                manifest[i] = (WriteType)rawType;
            }

            int dataLength = innerReader.ReadInt32();
            byte[] dataBytes = ReadExactBytes(innerReader, dataLength);
            if (innerStream.Position != innerStream.Length)
            {
                error = SaveError.CorruptData;
                return false;
            }

            using var dataStream = new MemoryStream(dataBytes, writable: false);
            using var reader = new ContentReader(dataStream, manifest);

            try
            {
                data = Read(reader);
            }
            catch (ManifestMismatchException)
            {
                data = default;
                error = SaveError.ManifestMismatch;
                return false;
            }

            if (!reader.IsManifestComplete || !reader.IsDataComplete)
            {
                data = default;
                error = SaveError.ManifestMismatch;
                return false;
            }

            return true;
        }
        catch
        {
            data = default;
            error = SaveError.CorruptData;
            return false;
        }
    }

    /// <summary>Checks whether a file exists beneath <see cref="SaveFolder"/>.</summary>
    /// <param name="fileName">Relative file name or subpath.</param>
    /// <returns><see langword="true"/> when the file exists and the path is valid.</returns>
    public bool FileExists(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        try
        {
            return File.Exists(GetSafePath(fileName));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Deletes a file beneath <see cref="SaveFolder"/>.</summary>
    /// <param name="fileName">Relative file name or subpath.</param>
    /// <returns><see langword="true"/> when an existing file was deleted.</returns>
    public bool Delete(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        try
        {
            string fullPath = GetSafePath(fileName);
            if (!File.Exists(fullPath))
                return false;

            File.Delete(fullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetApplicationSaveFolder()
    {
        Game game = Game.Instance;
        if (game == null)
            throw new InvalidOperationException("A Game instance must exist before creating a save handler.");

        return game.ApplicationSaveFolder;
    }

    private string GetSafePath(string fileName)
    {
        string normalized = fileName
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string saveFolderFull = Path.GetFullPath(_saveFolder);
        string fullPath = Path.GetFullPath(Path.Combine(saveFolderFull, normalized));
        string relative = Path.GetRelativePath(saveFolderFull, fullPath);

        if (Path.IsPathRooted(relative) ||
            relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar &&
             relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Invalid save path: '{fileName}'.", nameof(fileName));
        }

        return fullPath;
    }

    private static byte[] ReadExactBytes(BinaryReader reader, int length)
    {
        if (length < 0)
            throw new InvalidDataException("Save data contains a negative length.");

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (length > remaining)
            throw new EndOfStreamException("Save data ended before the declared length was read.");

        byte[] data = reader.ReadBytes(length);
        if (data.Length != length)
            throw new EndOfStreamException("Save data ended before the declared length was read.");

        return data;
    }

    private static bool IsOutOfSpace(IOException exception)
    {
        int code = exception.HResult & 0xFFFF;
        return code is 0x70 or 0x27 or 28;
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
            deflate.Write(data, 0, data.Length);

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data, writable: false);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private byte[] Encrypt(byte[] data, string magic, ulong versionHash)
    {
        byte[] salt = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            _encryptionKey,
            salt,
            1000,
            HashAlgorithmName.SHA256,
            32);

        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        byte[] aad = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] ciphertext = new byte[data.Length];
        byte[] tag = new byte[16];

        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Encrypt(nonce, data, ciphertext, tag, aad);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return result;
    }

    private byte[] Decrypt(byte[] data, string magic, ulong versionHash)
    {
        const int NonceLength = 12;
        const int TagLength = 16;

        if (data.Length < NonceLength + TagLength)
            throw new InvalidDataException("Encrypted save payload is too short.");

        byte[] salt = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            _encryptionKey,
            salt,
            1000,
            HashAlgorithmName.SHA256,
            32);

        byte[] nonce = new byte[NonceLength];
        byte[] tag = new byte[TagLength];
        byte[] ciphertext = new byte[data.Length - NonceLength - TagLength];

        Buffer.BlockCopy(data, 0, nonce, 0, NonceLength);
        Buffer.BlockCopy(data, NonceLength, tag, 0, TagLength);
        Buffer.BlockCopy(data, NonceLength + TagLength, ciphertext, 0, ciphertext.Length);

        byte[] aad = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        return plaintext;
    }

    /// <summary>Writes one instance of <typeparamref name="T"/> to the save payload.</summary>
    /// <param name="data">The value to serialize.</param>
    /// <param name="writer">The manifest-tracked writer.</param>
    protected abstract void Write(T data, ContentWriter writer);

    /// <summary>Reads one instance of <typeparamref name="T"/> from the save payload.</summary>
    /// <param name="reader">The manifest-verified reader.</param>
    /// <returns>The deserialized value.</returns>
    protected abstract T Read(ContentReader reader);
}
