// ============================================================================
//  GLTexture.cs
// ============================================================================
//  OpenGL 2D texture implementation for renderer-owned GPU textures.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLTexture : IGraphicsTexture
{
    private readonly GL _gl;
    private readonly GLStateCache _state;
    private uint _handle;
    private bool _disposed;

    public TextureDescription Description { get; }
    public bool IsValid => !_disposed && _handle != 0;

    internal uint Handle => _handle;

    internal unsafe GLTexture(
        GL gl,
        GLStateCache state,
        in TextureDescription description,
        ReadOnlySpan<byte> initialData)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        Description = description;

        if (description.SampleCount != 1)
            throw new NotSupportedException("Multisampled textures are not supported by the built-in OpenGL renderer.");

        ValidateColorFormat(description.Format);

        if (!initialData.IsEmpty)
            ValidateDataLength(description.Width, description.Height, description.Format, initialData.Length);

        _handle = _gl.GenTexture();
        if (_handle == 0)
            throw new InvalidOperationException("OpenGL failed to create a texture object.");

        try
        {
            _state.BindTexture2D(0, _handle);
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            SetParameters(description);

            (InternalFormat internalFormat, PixelFormat pixelFormat) = ToFormats(description.Format);

            if (initialData.IsEmpty)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    internalFormat,
                    (uint)description.Width,
                    (uint)description.Height,
                    0,
                    pixelFormat,
                    PixelType.UnsignedByte,
                    null);
            }
            else
            {
                fixed (byte* ptr = initialData)
                {
                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        internalFormat,
                        (uint)description.Width,
                        (uint)description.Height,
                        0,
                        pixelFormat,
                        PixelType.UnsignedByte,
                        ptr);
                }
            }

            if (description.GenerateMipmaps)
                _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void Bind(int textureUnit = 0)
    {
        ThrowIfDisposed();

        if (textureUnit < 0)
            throw new ArgumentOutOfRangeException(nameof(textureUnit));

        _state.BindTexture2D(textureUnit, _handle);
    }

    internal unsafe void Update(
        int x,
        int y,
        int width,
        int height,
        TextureFormat sourceFormat,
        ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (x < 0 || y < 0)
            throw new ArgumentOutOfRangeException(x < 0 ? nameof(x) : nameof(y));
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(width <= 0 ? nameof(width) : nameof(height));
        if (x > Description.Width - width || y > Description.Height - height)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture update region exceeds the texture bounds.");

        ValidateColorFormat(sourceFormat);
        ValidateDataLength(width, height, sourceFormat, data.Length);

        var sourceFormats = ToFormats(sourceFormat);
        PixelFormat pixelFormat = sourceFormats.Pixel;

        _state.BindTexture2D(0, _handle);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        fixed (byte* ptr = data)
        {
            _gl.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                x,
                y,
                (uint)width,
                (uint)height,
                pixelFormat,
                PixelType.UnsignedByte,
                ptr);
        }

        if (Description.GenerateMipmaps)
            _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_handle != 0)
        {
            _state.ForgetTexture(_handle);
            _gl.DeleteTexture(_handle);
            _handle = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void SetParameters(in TextureDescription description)
    {
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)ToFilter(description.MinFilter));
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)ToFilter(description.MagFilter));
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)ToWrap(description.WrapU));
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)ToWrap(description.WrapV));
    }

    private static (InternalFormat Internal, PixelFormat Pixel) ToFormats(TextureFormat format) => format switch
    {
        TextureFormat.R8 => (InternalFormat.R8, PixelFormat.Red),
        TextureFormat.RG8 => (InternalFormat.RG8, PixelFormat.RG),
        TextureFormat.RGB8 => (InternalFormat.Rgb8, PixelFormat.Rgb),
        TextureFormat.RGBA8 => (InternalFormat.Rgba8, PixelFormat.Rgba),
        TextureFormat.SRgba8 => (InternalFormat.Srgb8Alpha8, PixelFormat.Rgba),
        _ => throw new NotSupportedException($"OpenGL color texture format '{format}' is not supported by the built-in renderer.")
    };

    private static GLEnum ToFilter(TextureFilter filter) => filter switch
    {
        TextureFilter.Nearest => GLEnum.Nearest,
        TextureFilter.Linear => GLEnum.Linear,
        _ => throw new ArgumentOutOfRangeException(nameof(filter))
    };

    private static GLEnum ToWrap(TextureWrap wrap) => wrap switch
    {
        TextureWrap.ClampToEdge => GLEnum.ClampToEdge,
        TextureWrap.Repeat => GLEnum.Repeat,
        TextureWrap.MirroredRepeat => GLEnum.MirroredRepeat,
        _ => throw new ArgumentOutOfRangeException(nameof(wrap))
    };

    private static int BytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.R8 => 1,
        TextureFormat.RG8 => 2,
        TextureFormat.RGB8 => 3,
        TextureFormat.RGBA8 or TextureFormat.SRgba8 => 4,
        _ => throw new NotSupportedException($"Pixel size for texture format '{format}' is not supported by the built-in OpenGL renderer.")
    };

    private static void ValidateDataLength(int width, int height, TextureFormat format, int actualLength)
    {
        int expectedLength = checked(width * height * BytesPerPixel(format));
        if (actualLength != expectedLength)
        {
            throw new ArgumentException(
                $"Pixel data length is {actualLength} bytes, but {width}x{height} {format} requires {expectedLength} bytes.");
        }
    }

    private static void ValidateColorFormat(TextureFormat format)
    {
        _ = BytesPerPixel(format);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
