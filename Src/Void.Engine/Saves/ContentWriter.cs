// ============================================================================
//  ContentWriter.cs
// ============================================================================
//  Binary writer with manifest tracking and game-specific type support
//  for secure save file generation.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Void.Engine.Saves;

/// <summary>
/// Provides a binary writer with manifest tracking and game-specific type
/// support for generating secure save files.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ContentWriter"/> works in tandem with <see cref="ContentReader"/>
/// to ensure data integrity during save/load operations. Every write operation
/// is recorded in a manifest that tracks the order and types of all values
/// written. This manifest is later used by <see cref="ContentReader"/> to
/// verify that values are read in the exact same order with matching types.
/// </para>
/// <para>
/// The manifest tracking system provides protection against:
/// <list type="bullet">
///   <item><description>Data corruption from incomplete writes or storage errors</description></item>
///   <item><description>Version mismatches where save file structure has changed</description></item>
///   <item><description>Programming errors where read order doesn't match write order</description></item>
///   <item><description>Malicious tampering with save data</description></item>
/// </list>
/// </para>
/// <para>
/// The writer is typically used inside a derived <see cref="ContentTypeWriterReader{T}"/>
/// implementation. The manifest is automatically generated during the write
/// process and embedded in the save file.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Called from within a ContentTypeWriterReader&lt;T&gt; implementation
/// protected override void Write(T data, ContentWriter writer)
/// {
///     // Write values in the order they should be read
///     writer.Write(data.Position);
///     writer.Write(data.Health);
///     writer.Write(data.PlayerName);
///     writer.WriteObject(data.Inventory);
///     
///     // The writer automatically tracks the manifest
///     // The manifest is embedded in the save file for verification
/// }
/// </code>
/// </para>
/// <para>
/// <b>Important Notes:</b>
/// <list type="bullet">
///   <item><description>All write operations must be performed in the exact same order as their corresponding read operations</description></item>
///   <item><description>The manifest is automatically generated and should not be manually modified</description></item>
///   <item><description>String values are written with a 7-bit encoded length prefix followed by UTF-8 bytes</description></item>
///   <item><description>Custom objects are serialized using XML serialization</description></item>
///   <item><description>Null objects are written as a single boolean flag (false)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. Each writer instance should be used on a single thread.
/// </para>
/// </remarks>
public sealed class ContentWriter : BinaryWriter
{
    private readonly List<WriteType> _manifest = new();

    internal ContentWriter(Stream stream) : base(stream) { }

    internal WriteType[] Manifest => _manifest.ToArray();

    /// <summary>
    /// Writes a <see cref="Vect2"/> value to the stream and records it in the manifest.
    /// </summary>
    /// <param name="value">The <see cref="Vect2"/> value to write.</param>
    public void Write(Vect2 value)
    {
        _manifest.Add(WriteType.Vect2);

        base.Write(value.X);
        base.Write(value.Y);
    }

    /// <summary>
    /// Writes a <see cref="Rect2"/> value to the stream and records it in the manifest.
    /// </summary>
    /// <param name="value">The <see cref="Rect2"/> value to write.</param>
    public void Write(Rect2 value)
    {
        _manifest.Add(WriteType.Rect2);

        base.Write(value.X);
        base.Write(value.Y);
        base.Write(value.Width);
        base.Write(value.Height);
    }

    /// <summary>
    /// Writes a <see cref="Color"/> value to the stream and records it in the manifest.
    /// </summary>
    /// <param name="value">The <see cref="Color"/> value to write.</param>
    public void Write(Color value)
    {
        _manifest.Add(WriteType.Color);

        base.Write(value.R);
        base.Write(value.G);
        base.Write(value.B);
        base.Write(value.A);
    }

    /// <summary>
    /// Writes a string value to the stream using UTF-8 encoding with a 7-bit encoded length prefix.
    /// </summary>
    /// <param name="value">The string value to write.</param>
    /// <remarks>
    /// The string is written as a 7-bit encoded length followed by the UTF-8 bytes.
    /// This format is compatible with the reader's <see cref="ContentReader.ReadString"/> method.
    /// </remarks>
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

    /// <summary>
    /// Writes a 32-bit signed integer to the stream and records it in the manifest.
    /// </summary>
    public override void Write(int value)
    {
        _manifest.Add(WriteType.Int32);
        base.Write(value);
    }

    /// <summary>
    /// Writes a 32-bit floating-point value to the stream and records it in the manifest.
    /// </summary>
    public override void Write(float value)
    {
        _manifest.Add(WriteType.Single);
        base.Write(value);
    }

    /// <summary>
    /// Writes a boolean value to the stream and records it in the manifest.
    /// </summary>
    public override void Write(bool value)
    {
        _manifest.Add(WriteType.Boolean);
        base.Write(value);
    }

    /// <summary>
    /// Writes an 8-bit unsigned integer to the stream and records it in the manifest.
    /// </summary>
    public override void Write(byte value)
    {
        _manifest.Add(WriteType.Byte);
        base.Write(value);
    }

    /// <summary>
    /// Writes a 64-bit signed integer to the stream and records it in the manifest.
    /// </summary>
    public override void Write(long value)
    {
        _manifest.Add(WriteType.Int64);
        base.Write(value);
    }

    /// <summary>
    /// Writes a 64-bit floating-point value to the stream and records it in the manifest.
    /// </summary>
    public override void Write(double value)
    {
        _manifest.Add(WriteType.Double);
        base.Write(value);
    }

    /// <summary>
    /// Writes a custom object to the stream using XML serialization and records it in the manifest.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize, or null.</param>
    /// <remarks>
    /// <para>
    /// The object is serialized to XML and written as a length-prefixed byte array.
    /// If the value is null, a single boolean flag (false) is written.
    /// </para>
    /// <para>
    /// This method is compatible with <see cref="ContentReader.ReadObject{T}"/>.
    /// </para>
    /// </remarks>
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