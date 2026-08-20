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
