// ============================================================================
//  ContentReader.cs
// ============================================================================
//  Binary reader with manifest-based verification for save/load operations.
//  Ensures data integrity by validating the expected read order against
//  the manifest generated during writing.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Saves;

/// <summary>
/// Provides a manifest-verified binary reader for secure deserialization of save data.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ContentReader"/> works in tandem with <see cref="ContentWriter"/>
/// to ensure data integrity during save/load operations. When data is written,
/// a manifest is generated that records the exact order and types of every value
/// written. During reading, this manifest is used to verify that values are read
/// in the identical order with matching types.
/// </para>
/// <para>
/// This verification system provides protection against:
/// <list type="bullet">
///   <item><description>Data corruption from incomplete writes or storage errors</description></item>
///   <item><description>Version mismatches where save file structure has changed</description></item>
///   <item><description>Programming errors where read order doesn't match write order</description></item>
///   <item><description>Malicious tampering with save data</description></item>
/// </list>
/// </para>
/// <para>
/// The reader is typically used inside a derived <see cref="ContentTypeWriterReader{T}"/>
/// implementation. The manifest is provided automatically by the save system
/// and should not be manually constructed.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Called from within a ContentTypeWriterReader&lt;T&gt; implementation
/// protected override T Read(ContentReader reader)
/// {
///     // Read values in the exact same order they were written
///     var position = reader.ReadVect2();
///     var health = reader.ReadInt32();
///     var playerName = reader.ReadString();
///     var inventory = reader.ReadObject&lt;List&lt;Item&gt;&gt;();
///     
///     // The reader automatically verifies the manifest
///     // If the read order doesn't match the write order, an exception is thrown
///     
///     return new T(position, health, playerName, inventory);
/// }
/// </code>
/// </para>
/// <para>
/// <b>Important Notes:</b>
/// <list type="bullet">
///   <item><description>All read operations must be performed in the exact same order as their corresponding write operations</description></item>
///   <item><description><see cref="IsManifestComplete"/> should be checked after reading to ensure all data was consumed</description></item>
///   <item><description>If a manifest mismatch is detected, the save file should be considered corrupt</description></item>
///   <item><description>This class is internal to the save system and should not be instantiated directly by user code</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. Each reader instance should be used on a single thread.
/// </para>
/// </remarks>
public sealed class ContentReader : BinaryReader
{
    private readonly WriteType[] _manifest;
    private int _manifestIndex;

    internal ContentReader(Stream stream, WriteType[] manifest) : base(stream)
    {
        _manifest = manifest;
        _manifestIndex = 0;
    }

    /// <summary>
    /// Gets a value indicating whether all manifest entries have been consumed.
    /// </summary>
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

    /// <summary>
    /// Reads a <see cref="Vect2"/> value from the stream.
    /// </summary>
    /// <returns>The read <see cref="Vect2"/> value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public Vect2 ReadVect2()
    {
        VerifyNext(WriteType.Vect2);
        return new Vect2(base.ReadSingle(), base.ReadSingle());
    }

    /// <summary>
    /// Reads a <see cref="Rect2"/> value from the stream.
    /// </summary>
    /// <returns>The read <see cref="Rect2"/> value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public Rect2 ReadRect2()
    {
        VerifyNext(WriteType.Rect2);
        return new Rect2(
            base.ReadSingle(), base.ReadSingle(),
            base.ReadSingle(), base.ReadSingle()
        );
    }

    /// <summary>
    /// Reads a <see cref="Color"/> value from the stream.
    /// </summary>
    /// <returns>The read <see cref="Color"/> value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public Color ReadColor()
    {
        VerifyNext(WriteType.Color);
        return new Color(
            base.ReadByte(), base.ReadByte(),
            base.ReadByte(), base.ReadByte()
        );
    }

    /// <summary>
    /// Reads a string value from the stream using UTF-8 encoding with a length prefix.
    /// </summary>
    /// <returns>The read string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
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

    /// <summary>
    /// Reads a 32-bit signed integer from the stream.
    /// </summary>
    /// <returns>The read integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override int ReadInt32()
    {
        VerifyNext(WriteType.Int32);
        return base.ReadInt32();
    }

    /// <summary>
    /// Reads a 32-bit floating-point value from the stream.
    /// </summary>
    /// <returns>The read float value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override float ReadSingle()
    {
        VerifyNext(WriteType.Single);
        return base.ReadSingle();
    }

    /// <summary>
    /// Reads a boolean value from the stream.
    /// </summary>
    /// <returns>The read boolean value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override bool ReadBoolean()
    {
        VerifyNext(WriteType.Boolean);
        return base.ReadBoolean();
    }

    /// <summary>
    /// Reads an unsigned byte from the stream.
    /// </summary>
    /// <returns>The read byte value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override byte ReadByte()
    {
        VerifyNext(WriteType.Byte);
        return base.ReadByte();
    }

    /// <summary>
    /// Reads a 64-bit signed integer from the stream.
    /// </summary>
    /// <returns>The read long value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override long ReadInt64()
    {
        VerifyNext(WriteType.Int64);
        return base.ReadInt64();
    }

    /// <summary>
    /// Reads a 64-bit floating-point value from the stream.
    /// </summary>
    /// <returns>The read double value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
    public override double ReadDouble()
    {
        VerifyNext(WriteType.Double);
        return base.ReadDouble();
    }

    /// <summary>
    /// Reads a custom object from the stream using XML serialization.
    /// </summary>
    /// <typeparam name="T">The type of the object to read.</typeparam>
    /// <returns>The deserialized object, or default if the value is null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the manifest indicates a different type was expected.</exception>
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