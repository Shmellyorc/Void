// ============================================================================
//  ContentWriter.cs
// ============================================================================
//  Manifest-tracked binary writer for VOID save data.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Xml.Serialization;

namespace Void.Engine.Saves;

/// <summary>
/// Writes supported save values while recording their order in a type manifest.
/// </summary>
/// <remarks>
/// <para>
/// Use the manifest-tracked overloads declared by this class when implementing
/// <see cref="ContentTypeWriterReader{T}.Write(T, ContentWriter)"/>. Matching
/// <see cref="ContentReader"/> calls must read the same supported types in the
/// same order.
/// </para>
/// <para>
/// Inherited <see cref="BinaryWriter"/> overloads that are not overridden here do
/// not create manifest entries and should not be used for save-schema fields.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// protected override void Write(PlayerSave data, ContentWriter writer)
/// {
///     writer.Write(data.Name);
///     writer.Write(data.Level);
///     writer.Write(data.Position);
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class ContentWriter : BinaryWriter
{
    private readonly List<WriteType> _manifest = new();

    internal ContentWriter(Stream stream) : base(stream) { }

    internal WriteType[] Manifest => _manifest.ToArray();

    /// <summary>Writes a <see cref="Vect2"/> and records its manifest entry.</summary>
    /// <param name="value">The value to write.</param>
    public void Write(Vect2 value)
    {
        _manifest.Add(WriteType.Vect2);
        base.Write(value.X);
        base.Write(value.Y);
    }

    /// <summary>Writes a <see cref="Rect2"/> and records its manifest entry.</summary>
    /// <param name="value">The value to write.</param>
    public void Write(Rect2 value)
    {
        _manifest.Add(WriteType.Rect2);
        base.Write(value.X);
        base.Write(value.Y);
        base.Write(value.Width);
        base.Write(value.Height);
    }

    /// <summary>Writes a <see cref="Color"/> and records its manifest entry.</summary>
    /// <param name="value">The value to write.</param>
    public void Write(Color value)
    {
        _manifest.Add(WriteType.Color);
        base.Write(value.R);
        base.Write(value.G);
        base.Write(value.B);
        base.Write(value.A);
    }

    /// <summary>
    /// Writes a UTF-8 string and records its manifest entry. A null value is
    /// stored as an empty string.
    /// </summary>
    /// <param name="value">The value to write.</param>
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

    /// <summary>Writes a 32-bit signed integer and records its manifest entry.</summary>
    public override void Write(int value)
    {
        _manifest.Add(WriteType.Int32);
        base.Write(value);
    }

    /// <summary>Writes a 32-bit floating-point value and records its manifest entry.</summary>
    public override void Write(float value)
    {
        _manifest.Add(WriteType.Single);
        base.Write(value);
    }

    /// <summary>Writes a Boolean and records its manifest entry.</summary>
    public override void Write(bool value)
    {
        _manifest.Add(WriteType.Boolean);
        base.Write(value);
    }

    /// <summary>Writes an unsigned byte and records its manifest entry.</summary>
    public override void Write(byte value)
    {
        _manifest.Add(WriteType.Byte);
        base.Write(value);
    }

    /// <summary>Writes a 64-bit signed integer and records its manifest entry.</summary>
    public override void Write(long value)
    {
        _manifest.Add(WriteType.Int64);
        base.Write(value);
    }

    /// <summary>Writes a 64-bit floating-point value and records its manifest entry.</summary>
    public override void Write(double value)
    {
        _manifest.Add(WriteType.Double);
        base.Write(value);
    }

    /// <summary>Serializes an object as XML and records one object manifest entry.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="value">The value to serialize. Null values are preserved as null.</param>
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
