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
