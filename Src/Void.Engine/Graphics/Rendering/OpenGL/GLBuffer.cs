// ============================================================================
//  GLBuffer.cs
// ============================================================================
//  Built-in OpenGL buffer implementation used by the renderer-neutral device.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLBuffer : IGraphicsBuffer
{
    private readonly GL _gl;
    private readonly GLStateCache _state;
    private readonly BufferTargetARB _target;
    private uint _handle;
    private uint _vertexArray;
    private bool _disposed;

    public BufferDescription Description { get; }
    public bool IsValid => !_disposed && _handle != 0;

    internal uint Handle => _handle;
    internal BufferTargetARB Target => _target;

    internal unsafe GLBuffer(GL gl, GLStateCache state, in BufferDescription description)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        Description = description;
        _target = ToTarget(description.Type);

        _handle = _gl.GenBuffer();
        if (_handle == 0)
            throw new InvalidOperationException("OpenGL failed to create a buffer object.");

        // Use the copy-write binding for storage allocation so index-buffer
        // creation never mutates the element-buffer state of the active VAO.
        _state.BindCopyWriteBuffer(_handle);
        _gl.BufferData(
            BufferTargetARB.CopyWriteBuffer,
            (nuint)description.SizeInBytes,
            null,
            ToUsage(description.Usage));
    }

    internal void Bind()
    {
        ThrowIfDisposed();
        _gl.BindBuffer(_target, _handle);
    }

    internal unsafe uint GetOrCreateVertexArray()
    {
        ThrowIfDisposed();

        if (Description.Type != BufferType.Vertex)
            throw new InvalidOperationException("Only a vertex buffer can own a vertex array layout.");

        if (_vertexArray != 0)
            return _vertexArray;

        VertexLayoutDescription layout = Description.VertexLayout
            ?? throw new InvalidOperationException("The vertex buffer does not declare a VertexLayoutDescription.");

        _vertexArray = _gl.GenVertexArray();
        if (_vertexArray == 0)
            throw new InvalidOperationException("OpenGL failed to create a vertex array object.");

        _state.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _handle);

        foreach (VertexAttributeDescription attribute in layout.Attributes.Span)
        {
            (int components, VertexAttribPointerType type, bool normalized) = ToVertexAttribute(attribute.Format);
            uint location = checked((uint)attribute.Location);

            _gl.EnableVertexAttribArray(location);
            _gl.VertexAttribPointer(
                location,
                components,
                type,
                normalized,
                checked((uint)layout.Stride),
                (void*)(nint)attribute.Offset);
        }

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _state.BindVertexArray(0);
        return _vertexArray;
    }

    internal unsafe void Update<T>(ReadOnlySpan<T> data, int byteOffset)
        where T : unmanaged
    {
        ThrowIfDisposed();

        if (byteOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(byteOffset));

        int byteCount = checked(data.Length * sizeof(T));
        if (byteOffset > Description.SizeInBytes - byteCount)
            throw new ArgumentOutOfRangeException(nameof(data), "Buffer update exceeds the allocated buffer size.");

        if (byteCount == 0)
            return;

        // Copy-write is a global buffer binding rather than VAO state, so an
        // index-buffer upload cannot accidentally replace a VAO's EBO binding.
        _state.BindCopyWriteBuffer(_handle);

        fixed (T* ptr = data)
        {
            // A true full-buffer update is a replacement, not a patch. BufferData
            // lets the driver provide fresh storage instead of synchronizing with
            // an older store that may still be in flight on the GPU.
            if (byteOffset == 0 && byteCount == Description.SizeInBytes)
            {
                _gl.BufferData(
                    BufferTargetARB.CopyWriteBuffer,
                    (nuint)Description.SizeInBytes,
                    ptr,
                    ToUsage(Description.Usage));
                return;
            }

            // Partial writes must preserve bytes outside the updated range.
            _gl.BufferSubData(
                BufferTargetARB.CopyWriteBuffer,
                (nint)byteOffset,
                (nuint)byteCount,
                ptr);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_vertexArray != 0)
        {
            _state.ForgetVertexArray(_vertexArray);
            _gl.DeleteVertexArray(_vertexArray);
            _vertexArray = 0;
        }

        if (_handle != 0)
        {
            _state.ForgetBuffer(_handle);
            _gl.DeleteBuffer(_handle);
            _handle = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    private static BufferTargetARB ToTarget(BufferType type) => type switch
    {
        BufferType.Vertex => BufferTargetARB.ArrayBuffer,
        BufferType.Index => BufferTargetARB.ElementArrayBuffer,
        BufferType.Uniform => BufferTargetARB.UniformBuffer,
        BufferType.Storage => BufferTargetARB.ShaderStorageBuffer,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static BufferUsageARB ToUsage(BufferUsage usage) => usage switch
    {
        BufferUsage.Static => BufferUsageARB.StaticDraw,
        BufferUsage.Dynamic => BufferUsageARB.DynamicDraw,
        BufferUsage.Stream => BufferUsageARB.StreamDraw,
        _ => throw new ArgumentOutOfRangeException(nameof(usage))
    };

    private static (int Components, VertexAttribPointerType Type, bool Normalized) ToVertexAttribute(VertexElementFormat format)
        => format switch
        {
            VertexElementFormat.Float => (1, VertexAttribPointerType.Float, false),
            VertexElementFormat.Float2 => (2, VertexAttribPointerType.Float, false),
            VertexElementFormat.Float3 => (3, VertexAttribPointerType.Float, false),
            VertexElementFormat.Float4 => (4, VertexAttribPointerType.Float, false),
            VertexElementFormat.Byte4 => (4, VertexAttribPointerType.Byte, false),
            VertexElementFormat.Byte4Normalized => (4, VertexAttribPointerType.Byte, true),
            VertexElementFormat.UByte4 => (4, VertexAttribPointerType.UnsignedByte, false),
            VertexElementFormat.UByte4Normalized => (4, VertexAttribPointerType.UnsignedByte, true),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported OpenGL vertex element format.")
        };
}
