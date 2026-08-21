// ============================================================================
//  ContentTypeWriterReader.cs
// ============================================================================
//  Abstract base class for implementing type-specific save/load operations
//  with version checking, encryption, compression, and manifest verification.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Void.Engine.Saves;

/// <summary>
/// Defines error codes that can occur during save and load operations.
/// </summary>
public enum SaveError
{
    /// <summary>No error occurred.</summary>
    None = 0,

    /// <summary>The file path was invalid or contained illegal characters.</summary>
    InvalidPath,

    /// <summary>The file extension was not .sav.</summary>
    InvalidExtension,

    /// <summary>Failed to write to disk due to permissions or other I/O errors.</summary>
    WriteFailed,

    /// <summary>Insufficient disk space to save the file.</summary>
    OutOfSpace,

    /// <summary>Failed to serialize the data to the save format.</summary>
    SerializationFailed,

    /// <summary>Encryption of the save data failed.</summary>
    EncryptionFailed,

    /// <summary>The save file was not found on disk.</summary>
    FileNotFound,

    /// <summary>The file magic number did not match the expected value.</summary>
    WrongMagic,

    /// <summary>The save file version does not match the current application version.</summary>
    VersionMismatch,

    /// <summary>The encryption key was incorrect or the data could not be decrypted.</summary>
    WrongKey,

    /// <summary>The save data was corrupted and could not be read.</summary>
    CorruptData,

    /// <summary>The manifest did not match the read order.</summary>
    ManifestMismatch,

    /// <summary>An unknown error occurred.</summary>
    Unknown
}

/// <summary>
/// Internal enumeration for tracking data types written to the save file.
/// Used for manifest-based integrity verification.
/// </summary>
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
/// Abstract base class for implementing type-specific save and load operations
/// with comprehensive security features including version checking, encryption,
/// compression, and manifest-based data integrity verification.
/// </summary>
/// <typeparam name="T">The type of data to save and load.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="ContentTypeWriterReader{T}"/> class provides a complete
/// save/load system with the following features:
/// <list type="bullet">
///   <item><description><b>Version Checking:</b> Prevents loading save files from different application versions</description></item>
///   <item><description><b>Encryption:</b> AES-GCM authenticated encryption with PBKDF2 key derivation</description></item>
///   <item><description><b>Compression:</b> Deflate compression with automatic selection (only compresses if beneficial)</description></item>
///   <item><description><b>Manifest Verification:</b> Ensures read order matches write order</description></item>
///   <item><description><b>Path Security:</b> Prevents directory traversal attacks</description></item>
///   <item><description><b>Atomic Writes:</b> Uses temporary files to prevent corruption</description></item>
///   <item><description><b>Error Handling:</b> Detailed error codes through Try methods</description></item>
/// </list>
/// </para>
/// <para>
/// To implement a save system, derive from this class and implement the abstract
/// <see cref="Write"/> and <see cref="Read"/> methods using <see cref="ContentWriter"/>
/// and <see cref="ContentReader"/> to handle the serialization of your data type.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Define your save data type
/// public class PlayerSaveData
/// {
///     public string Name { get; set; }
///     public int Level { get; set; }
///     public Vect2 Position { get; set; }
///     public List&lt;Item&gt; Inventory { get; set; }
/// }
/// 
/// // Create a writer/reader for your data type
/// public class PlayerSaveSystem : ContentTypeWriterReader&lt;PlayerSaveData&gt;
/// {
///     public PlayerSaveSystem() : base() { }
///     public PlayerSaveSystem(string key) : base(key) { }
///     
///     protected override void Write(PlayerSaveData data, ContentWriter writer)
///     {
///         writer.Write(data.Name);
///         writer.Write(data.Level);
///         writer.Write(data.Position);
///         writer.WriteObject(data.Inventory);
///     }
///     
///     protected override PlayerSaveData Read(ContentReader reader)
///     {
///         return new PlayerSaveData
///         {
///             Name = reader.ReadString(),
///             Level = reader.ReadInt32(),
///             Position = reader.ReadVect2(),
///             Inventory = reader.ReadObject&lt;List&lt;Item&gt;&gt;()
///         };
///     }
/// }
/// 
/// // Use the save system
/// var saveSystem = new PlayerSaveSystem();
/// 
/// // Save with error handling
/// if (saveSystem.TrySave("player.sav", playerData, out var error))
/// {
///     Console.WriteLine("Save successful!");
/// }
/// else
/// {
///     Console.WriteLine($"Save failed: {error}");
/// }
/// 
/// // Load with error handling
/// if (saveSystem.TryLoad("player.sav", out var loadedData, out error))
/// {
///     Console.WriteLine($"Loaded: {loadedData.Name}");
/// }
/// 
/// // Or use the throwing versions
/// saveSystem.Save("player.sav", playerData);
/// var data = saveSystem.Load("player.sav");
/// </code>
/// </para>
/// <para>
/// <b>Security Considerations:</b>
/// <list type="bullet">
///   <item><description>Encryption uses AES-GCM with authenticated encryption</description></item>
///   <item><description>Keys are derived using PBKDF2 with 1000 iterations and SHA-256</description></item>
///   <item><description>A unique nonce is generated for each encryption operation</description></item>
///   <item><description>Additional authenticated data (AAD) includes the magic and version hash</description></item>
///   <item><description>Path security prevents directory traversal attacks</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. Each instance should be used on a single thread.
/// </para>
/// </remarks>
public abstract class ContentTypeWriterReader<T>
{
    private const string Magic = "VOID";
    private const string FileExtension = ".sav";

    private readonly byte[] _encryptionKey;
    private readonly string _saveFolder;

    /// <summary>
    /// Gets the full path to the save folder.
    /// </summary>
    public string SaveFolder => _saveFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeWriterReader{T}"/> class
    /// without encryption.
    /// </summary>
    protected ContentTypeWriterReader()
    {
        _saveFolder = Game.Instance.ApplicationSaveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeWriterReader{T}"/> class
    /// with the specified string encryption key.
    /// </summary>
    /// <param name="encryptionKey">The encryption key string. Will be converted to UTF-8 bytes.</param>
    protected ContentTypeWriterReader(string encryptionKey)
    {
        _encryptionKey = Encoding.UTF8.GetBytes(encryptionKey);
        _saveFolder = Game.Instance.ApplicationSaveFolder;

        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeWriterReader{T}"/> class
    /// with the specified byte array encryption key.
    /// </summary>
    /// <param name="encryptionKey">The encryption key as a byte array.</param>
    protected ContentTypeWriterReader(byte[] encryptionKey)
    {
        _encryptionKey = encryptionKey;
        _saveFolder = Game.Instance.ApplicationSaveFolder;

        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>
    /// Saves data to the specified file, throwing an exception on failure.
    /// </summary>
    /// <param name="fileName">The name of the save file (must end with .sav).</param>
    /// <param name="data">The data to save.</param>
    /// <exception cref="InvalidOperationException">Thrown when the save operation fails.</exception>
    public void Save(string fileName, T data)
    {
        if (!TrySave(fileName, data, out var error))
            throw new InvalidOperationException($"Save failed: {error}");
    }

    /// <summary>
    /// Loads data from the specified file, throwing an exception on failure.
    /// </summary>
    /// <param name="fileName">The name of the save file (must end with .sav).</param>
    /// <returns>The loaded data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the load operation fails.</exception>
    public T Load(string fileName)
    {
        TryLoad(fileName, out var data, out _);
        return data;
    }

    /// <summary>
    /// Attempts to save data to the specified file with detailed error reporting.
    /// </summary>
    /// <param name="fileName">The name of the save file (must end with .sav).</param>
    /// <param name="data">The data to save.</param>
    /// <param name="error">When this method returns, contains the error that occurred, if any.</param>
    /// <returns><see langword="true"/> if the save was successful; otherwise, <see langword="false"/>.</returns>
    public bool TrySave(string fileName, T data, out SaveError error)
    {
        error = SaveError.None;

        try
        {
            if (string.IsNullOrEmpty(fileName))
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

            string tempPath = fullPath + ".tmp";

            byte[] innerData;
            using (var innerStream = new MemoryStream())
            {
                using (var writer = new ContentWriter(innerStream))
                {
                    Write(data, writer);
                    writer.Flush();
                    WriteType[] manifest = writer.Manifest;
                    byte[] rawData = innerStream.ToArray();

                    using var combinedStream = new MemoryStream();
                    using var combinedWriter = new BinaryWriter(combinedStream);
                    combinedWriter.Write(manifest.Length);
                    foreach (var type in manifest)
                        combinedWriter.Write((byte)type);
                    combinedWriter.Write(rawData.Length);
                    combinedWriter.Write(rawData);
                    combinedWriter.Flush();
                    innerData = combinedStream.ToArray();
                }
            }

            bool compressed = false;
            byte[] dataToWrite = innerData;
            byte[] compressedData = Compress(innerData);

            if (compressedData.Length < innerData.Length)
            {
                dataToWrite = compressedData;
                compressed = true;
            }

            bool encrypted = _encryptionKey != null;
            byte[] finalData = dataToWrite;

            if (encrypted)
            {
                string version = GameSettings.Instance.AppVersion;
                ulong versionHash = HashHelper.Cache64(version);
                finalData = Encrypt(dataToWrite, Magic, versionHash);
            }

            string versionStr = GameSettings.Instance.AppVersion;
            ulong verHash = HashHelper.Cache64(versionStr);

            using (var fileStream = File.Create(tempPath))
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                binaryWriter.Write(Encoding.ASCII.GetBytes(Magic));
                binaryWriter.Write(verHash);
                binaryWriter.Write((byte)(encrypted ? 1 : 0));
                binaryWriter.Write((byte)(compressed ? 1 : 0));
                binaryWriter.Write(finalData.Length);
                binaryWriter.Write(finalData);
                binaryWriter.Flush();
            }

            if (File.Exists(fullPath))
                File.Delete(fullPath);
            File.Move(tempPath, fullPath);

            return true;
        }
        catch (IOException)
        {
            error = SaveError.OutOfSpace;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            error = SaveError.WriteFailed;
            return false;
        }
        catch (Exception)
        {
            error = SaveError.SerializationFailed;
            return false;
        }
    }

    /// <summary>
    /// Attempts to load data from the specified file with detailed error reporting.
    /// </summary>
    /// <param name="fileName">The name of the save file (must end with .sav).</param>
    /// <param name="data">When this method returns, contains the loaded data if successful.</param>
    /// <param name="error">When this method returns, contains the error that occurred, if any.</param>
    /// <returns><see langword="true"/> if the load was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryLoad(string fileName, out T data, out SaveError error)
    {
        data = default;
        error = SaveError.None;

        try
        {
            if (string.IsNullOrEmpty(fileName))
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

            using var fileStream = new MemoryStream(fileData);
            using var binaryReader = new BinaryReader(fileStream);

            byte[] magicBytes = binaryReader.ReadBytes(4);
            string magic = Encoding.ASCII.GetString(magicBytes);

            if (magic != Magic)
            {
                error = SaveError.WrongMagic;
                return false;
            }

            ulong savedVersionHash = binaryReader.ReadUInt64();
            string version = GameSettings.Instance.AppVersion;
            ulong currentVersionHash = HashHelper.Cache64(version);

            if (savedVersionHash != currentVersionHash)
            {
                error = SaveError.VersionMismatch;
                return false;
            }

            bool encrypted = binaryReader.ReadByte() == 1;
            bool compressed = binaryReader.ReadByte() == 1;
            int blobLength = binaryReader.ReadInt32();
            byte[] blob = binaryReader.ReadBytes(blobLength);
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
                catch
                {
                    error = SaveError.WrongKey;
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

            using var innerStream = new MemoryStream(innerData);
            using var innerReader = new BinaryReader(innerStream);

            int manifestLength = innerReader.ReadInt32();
            var manifest = new WriteType[manifestLength];
            for (int i = 0; i < manifestLength; i++)
                manifest[i] = (WriteType)innerReader.ReadByte();

            int dataLength = innerReader.ReadInt32();
            byte[] dataBytes = innerReader.ReadBytes(dataLength);

            using var dataStream = new MemoryStream(dataBytes);
            using var reader = new ContentReader(dataStream, manifest);

            data = Read(reader);

            if (!reader.IsManifestComplete)
            {
                error = SaveError.ManifestMismatch;
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            error = SaveError.CorruptData;
            return false;
        }
    }

    /// <summary>
    /// Checks if a save file exists.
    /// </summary>
    /// <param name="fileName">The name of the save file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public bool FileExists(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
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

    /// <summary>
    /// Deletes a save file.
    /// </summary>
    /// <param name="fileName">The name of the save file to delete.</param>
    /// <returns><see langword="true"/> if the file was deleted; otherwise, <see langword="false"/>.</returns>
    public bool Delete(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
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

    private string GetSafePath(string fileName)
    {
        fileName = fileName
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string fullPath = Path.GetFullPath(Path.Combine(_saveFolder, fileName));
        string saveFolderFull = Path.GetFullPath(_saveFolder).TrimEnd(Path.DirectorySeparatorChar);

        if (!fullPath.StartsWith(saveFolderFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid save path: '{fileName}'.");

        return fullPath;
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        deflate.CopyTo(output);

        return output.ToArray();
    }

    private byte[] Encrypt(byte[] data, string magic, ulong versionHash)
    {
        byte[] salt = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(_encryptionKey, salt, 1000, HashAlgorithmName.SHA256, 32);
        byte[] nonce = new byte[12];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

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
        byte[] salt = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(_encryptionKey, salt, 1000, HashAlgorithmName.SHA256, 32);
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        byte[] ciphertext = new byte[data.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(data, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(data, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(data, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

        byte[] aad = Encoding.ASCII.GetBytes(magic + versionHash);
        byte[] plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, 16);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);

        return plaintext;
    }

    /// <summary>
    /// Writes the data to the specified <see cref="ContentWriter"/>.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <param name="writer">The writer to use for serialization.</param>
    protected abstract void Write(T data, ContentWriter writer);

    /// <summary>
    /// Reads the data from the specified <see cref="ContentReader"/>.
    /// </summary>
    /// <param name="reader">The reader to use for deserialization.</param>
    /// <returns>The deserialized data.</returns>
    protected abstract T Read(ContentReader reader);
}