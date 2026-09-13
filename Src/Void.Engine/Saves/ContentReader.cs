// ============================================================================
//  ContentReader.cs
// ============================================================================
//  Manifest-verified binary reader for VOID save data.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Xml.Serialization;

namespace Void.Engine.Saves;

internal sealed class ManifestMismatchException : InvalidOperationException
{
    public ManifestMismatchException(string message) : base(message) { }
}

/// <summary>
/// Reads manifest-tracked values written by <see cref="ContentWriter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each supported read verifies the next manifest entry before consuming its
/// payload. Read values in the same order and with the same supported types used
/// by the matching <see cref="ContentWriter"/>.
/// </para>
/// <para>
/// The manifest detects schema/order mismatches. When the save payload is
/// encrypted, AES-GCM separately provides authenticated integrity for that payload.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// protected override PlayerSave Read(ContentReader reader)
/// {
///     return new PlayerSave
///     {
///         Name = reader.ReadString(),
///         Level = reader.ReadInt32(),
///         Position = reader.ReadVect2()
///     };
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class ContentReader : BinaryReader
{
    private readonly WriteType[] _manifest;
    private int _manifestIndex;

    internal ContentReader(Stream stream, WriteType[] manifest) : base(stream)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        _manifest = manifest;
    }

    internal bool IsManifestComplete => _manifestIndex == _manifest.Length;
    internal bool IsDataComplete => BaseStream.Position == BaseStream.Length;

    private void VerifyNext(WriteType expected)
    {
        if (_manifestIndex >= _manifest.Length)
        {
            throw new ManifestMismatchException(
                $"Expected {expected}, but the manifest ended at position {_manifestIndex}.");
        }

        WriteType actual = _manifest[_manifestIndex];
        if (actual != expected)
        {
            throw new ManifestMismatchException(
                $"Expected {expected}, but found {actual} at manifest position {_manifestIndex}.");
        }

        _manifestIndex++;
    }

    private byte[] ReadExactPayload(int length)
    {
        if (length < 0)
            throw new InvalidDataException("Save payload contains a negative length.");

        long remaining = BaseStream.Length - BaseStream.Position;
        if (length > remaining)
            throw new EndOfStreamException("Save payload ended before the declared data length was read.");

        byte[] data = base.ReadBytes(length);
        if (data.Length != length)
            throw new EndOfStreamException("Save payload ended before the declared data length was read.");

        return data;
    }

    /// <summary>Reads a <see cref="Vect2"/> value.</summary>
    /// <returns>The stored vector.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a vector.</exception>
    public Vect2 ReadVect2()
    {
        VerifyNext(WriteType.Vect2);
        return new Vect2(base.ReadSingle(), base.ReadSingle());
    }

    /// <summary>Reads a <see cref="Rect2"/> value.</summary>
    /// <returns>The stored rectangle.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a rectangle.</exception>
    public Rect2 ReadRect2()
    {
        VerifyNext(WriteType.Rect2);
        return new Rect2(
            base.ReadSingle(),
            base.ReadSingle(),
            base.ReadSingle(),
            base.ReadSingle());
    }

    /// <summary>Reads a <see cref="Color"/> value.</summary>
    /// <returns>The stored color.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a color.</exception>
    public Color ReadColor()
    {
        VerifyNext(WriteType.Color);
        return new Color(
            base.ReadByte(),
            base.ReadByte(),
            base.ReadByte(),
            base.ReadByte());
    }

    /// <summary>Reads a UTF-8 string written by <see cref="ContentWriter.Write(string)"/>.</summary>
    /// <returns>The stored string.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a string.</exception>
    public override string ReadString()
    {
        VerifyNext(WriteType.String);

        int length = 0, shift = 0;
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

    /// <summary>Reads a 32-bit signed integer.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a 32-bit integer.</exception>
    public override int ReadInt32()
    {
        VerifyNext(WriteType.Int32);
        return base.ReadInt32();
    }

    /// <summary>Reads a 32-bit floating-point value.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a single-precision value.</exception>
    public override float ReadSingle()
    {
        VerifyNext(WriteType.Single);
        return base.ReadSingle();
    }

    /// <summary>Reads a Boolean value.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a Boolean.</exception>
    public override bool ReadBoolean()
    {
        VerifyNext(WriteType.Boolean);
        return base.ReadBoolean();
    }

    /// <summary>Reads an unsigned byte.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a byte.</exception>
    public override byte ReadByte()
    {
        VerifyNext(WriteType.Byte);
        return base.ReadByte();
    }

    /// <summary>Reads a 64-bit signed integer.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a 64-bit integer.</exception>
    public override long ReadInt64()
    {
        VerifyNext(WriteType.Int64);
        return base.ReadInt64();
    }

    /// <summary>Reads a 64-bit floating-point value.</summary>
    /// <returns>The stored value.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not a double-precision value.</exception>
    public override double ReadDouble()
    {
        VerifyNext(WriteType.Double);
        return base.ReadDouble();
    }

    /// <summary>Reads an object serialized by <see cref="ContentWriter.WriteObject{T}(T)"/>.</summary>
    /// <typeparam name="T">The serialized object type.</typeparam>
    /// <returns>The deserialized value, or <see langword="default"/> when a null value was stored.</returns>
    /// <exception cref="InvalidOperationException">The next manifest entry is not an object.</exception>
    public T ReadObject<T>()
    {
        VerifyNext(WriteType.Object);

        bool hasValue = base.ReadBoolean();

        if (!hasValue)
            return default;

        int length = base.ReadInt32();
        byte[] data = base.ReadBytes(length);

        using var memoryStream = new MemoryStream(data);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(memoryStream);
    }
}
