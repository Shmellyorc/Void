// ============================================================================
//  SaveSystem.cs
// ============================================================================
//  Complete save/load system with version checking, encryption, compression,
//  and data integrity verification.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Saves;

/// <summary>
/// Errors that can occur during save/load operations.
/// </summary>
public enum SaveError
{
    None = 0,
    InvalidPath,
    InvalidExtension,
    WriteFailed,
    OutOfSpace,
    SerializationFailed,
    EncryptionFailed,
    FileNotFound,
    WrongMagic,
    VersionMismatch,
    WrongKey,
    CorruptData,
    ManifestMismatch,
    Unknown
}

/// <summary>
/// Internal enum for tracking what types were written to the save file.
/// Used for data integrity verification.
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
/// Binary writer with game-specific type support and manifest tracking.
/// Only the save system can create instances.
/// </summary>
public sealed class ContentWriter : BinaryWriter
{
    private readonly List<WriteType> _manifest = new();

    internal ContentWriter(Stream stream) : base(stream) { }

    internal WriteType[] Manifest => _manifest.ToArray();

    public void Write(Vect2 value)
    {
        _manifest.Add(WriteType.Vect2);
        base.Write(value.X);
        base.Write(value.Y);
    }

    public void Write(Rect2 value)
    {
        _manifest.Add(WriteType.Rect2);
        base.Write(value.X);
        base.Write(value.Y);
        base.Write(value.Width);
        base.Write(value.Height);
    }

    public void Write(Color value)
    {
        _manifest.Add(WriteType.Color);
        base.Write(value.R);
        base.Write(value.G);
        base.Write(value.B);
        base.Write(value.A);
    }

    public override void Write(string value)
    {
        _manifest.Add(WriteType.String);

        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);

        int length = bytes.Length;
        while (length >= 0x80)
        {
            base.Write((byte)(length | 0x80));
            length >>= 7;
        }
        base.Write((byte)length);

        base.Write(bytes);
    }

    public override void Write(int value)
    {
        _manifest.Add(WriteType.Int32);
        base.Write(value);
    }

    public override void Write(float value)
    {
        _manifest.Add(WriteType.Single);
        base.Write(value);
    }

    public override void Write(bool value)
    {
        _manifest.Add(WriteType.Boolean);
        base.Write(value);
    }

    public override void Write(byte value)
    {
        _manifest.Add(WriteType.Byte);
        base.Write(value);
    }

    public override void Write(long value)
    {
        _manifest.Add(WriteType.Int64);
        base.Write(value);
    }

    public override void Write(double value)
    {
        _manifest.Add(WriteType.Double);
        base.Write(value);
    }

    public void WriteObject<T>(T value)
    {
        _manifest.Add(WriteType.Object);

        if (value == null)
        {
            base.Write(false);
            return;
        }

        base.Write(true);

        using var memoryStream = new MemoryStream();
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        serializer.Serialize(memoryStream, value);

        byte[] data = memoryStream.ToArray();
        base.Write(data.Length);
        base.Write(data);
    }
}

/// <summary>
/// Binary reader with game-specific type support and manifest verification.
/// Only the save system can create instances.
/// </summary>
public sealed class ContentReader : BinaryReader
{
    private readonly WriteType[] _manifest;
    private int _manifestIndex;

    internal ContentReader(Stream stream, WriteType[] manifest) : base(stream)
    {
        _manifest = manifest;
        _manifestIndex = 0;
    }

    internal bool IsManifestComplete => _manifestIndex >= _manifest.Length;

    private void VerifyNext(WriteType expected)
    {
        if (_manifestIndex >= _manifest.Length)
            throw new InvalidOperationException($"Save data is corrupt: Expected {expected} but reached end of manifest.");

        var actual = _manifest[_manifestIndex];
        if (actual != expected)
            throw new InvalidOperationException($"Save data is corrupt: Expected {expected} but found {actual} at position {_manifestIndex}.");

        _manifestIndex++;
    }

    public Vect2 ReadVect2()
    {
        VerifyNext(WriteType.Vect2);
        return new Vect2(base.ReadSingle(), base.ReadSingle());
    }

    public Rect2 ReadRect2()
    {
        VerifyNext(WriteType.Rect2);
        return new Rect2(
            base.ReadSingle(), base.ReadSingle(),
            base.ReadSingle(), base.ReadSingle()
        );
    }

    public Color ReadColor()
    {
        VerifyNext(WriteType.Color);
        return new Color(
            base.ReadByte(), base.ReadByte(),
            base.ReadByte(), base.ReadByte()
        );
    }

    public override string ReadString()
    {
        VerifyNext(WriteType.String);

        int length = 0;
        int shift = 0;
        byte b;
        do
        {
            b = base.ReadByte();
            length |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        byte[] bytes = base.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public override int ReadInt32()
    {
        VerifyNext(WriteType.Int32);
        return base.ReadInt32();
    }

    public override float ReadSingle()
    {
        VerifyNext(WriteType.Single);
        return base.ReadSingle();
    }

    public override bool ReadBoolean()
    {
        VerifyNext(WriteType.Boolean);
        return base.ReadBoolean();
    }

    public override byte ReadByte()
    {
        VerifyNext(WriteType.Byte);
        return base.ReadByte();
    }

    public override long ReadInt64()
    {
        VerifyNext(WriteType.Int64);
        return base.ReadInt64();
    }

    public override double ReadDouble()
    {
        VerifyNext(WriteType.Double);
        return base.ReadDouble();
    }

    public T ReadObject<T>()
    {
        VerifyNext(WriteType.Object);

        bool hasValue = base.ReadBoolean();
        if (!hasValue)
            return default;

        int length = base.ReadInt32();
        byte[] data = base.ReadBytes(length);

        using var memoryStream = new MemoryStream(data);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(memoryStream);
    }
}

/// <summary>
/// Abstract base class for saving and loading game data.
/// Handles file I/O, path security, versioning, compression, and encryption.
/// </summary>
public abstract class ContentTypeWriterReader<T>
{
    private const string Magic = "VOID";
    private const string FileExtension = ".sav";

    private readonly byte[] _encryptionKey;
    private readonly string _saveFolder;

    public string SaveFolder => _saveFolder;

    /// <summary>
    /// No encryption. Adaptive compression always on.
    /// </summary>
    protected ContentTypeWriterReader()
    {
        _saveFolder = Game.Instance.ApplicationSaveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>
    /// With encryption. Adaptive compression always on.
    /// </summary>
    protected ContentTypeWriterReader(string encryptionKey)
    {
        _encryptionKey = Encoding.UTF8.GetBytes(encryptionKey);
        _saveFolder = Game.Instance.ApplicationSaveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    /// <summary>
    /// With encryption bytes. Adaptive compression always on.
    /// </summary>
    protected ContentTypeWriterReader(byte[] encryptionKey)
    {
        _encryptionKey = encryptionKey;
        _saveFolder = Game.Instance.ApplicationSaveFolder;
        Directory.CreateDirectory(_saveFolder);
    }

    public void Save(string fileName, T data)
    {
        if (!TrySave(fileName, data, out var error))
            throw new InvalidOperationException($"Save failed: {error}");
    }

    public T Load(string fileName)
    {
        TryLoad(fileName, out var data, out _);
        return data;
    }

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

            // Step 1: Write dev data + manifest to inner buffer
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

            // Step 2: Adaptive compression - always compress if smaller
            bool compressed = false;
            byte[] dataToWrite = innerData;
            byte[] compressedData = Compress(innerData);
            if (compressedData.Length < innerData.Length)
            {
                dataToWrite = compressedData;
                compressed = true;
            }

            // Step 3: Encrypt if key provided
            bool encrypted = _encryptionKey != null;
            byte[] finalData = dataToWrite;
            if (encrypted)
            {
                string version = Game.Instance.Version;
                ulong versionHash = HashHelper.Cache64(version);
                finalData = Encrypt(dataToWrite, Magic, versionHash);
            }

            // Step 4: Write final file
            string versionStr = Game.Instance.Version;
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
            string version = Game.Instance.Version;
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
        fileName = fileName.Replace('\\', Path.DirectorySeparatorChar)
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

    protected abstract void Write(T data, ContentWriter writer);
    protected abstract T Read(ContentReader reader);
}