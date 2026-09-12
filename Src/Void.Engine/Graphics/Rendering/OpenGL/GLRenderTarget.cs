// ============================================================================
//  GLRenderTarget.cs
// ============================================================================
//  OpenGL framebuffer and color-texture implementation for off-screen targets.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Silk.NET.OpenGL;

namespace Void.Engine.Graphics.Rendering.OpenGL;

internal sealed class GLRenderTarget : IGraphicsRenderTarget
{
    private readonly GL _gl;
    private readonly GLStateCache _state;
    private uint _framebuffer;
    private GLTexture _colorTexture;
    private bool _disposed;

    public RenderTargetDescription Description { get; }
    public IGraphicsTexture ColorTexture => _colorTexture;
    public bool IsValid => !_disposed && _framebuffer != 0 && _colorTexture is { IsValid: true };

    internal uint Handle => _framebuffer;

    internal GLRenderTarget(GL gl, GLStateCache state, in RenderTargetDescription description)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        Description = description;

        if (description.SampleCount != 1)
            throw new NotSupportedException("Multisampled render targets are not supported by the built-in OpenGL renderer.");

        if (description.DepthFormat.HasValue)
            throw new NotSupportedException("Depth/stencil attachments are not supported by the built-in OpenGL renderer.");

        var textureDescription = new TextureDescription(
            description.Width,
            description.Height,
            description.ColorFormat,
            TextureUsage.Sampled |
            TextureUsage.RenderTarget |
            TextureUsage.TransferSource |
            TextureUsage.TransferDestination,
            TextureFilter.Nearest,
            TextureFilter.Nearest,
            TextureWrap.ClampToEdge,
            TextureWrap.ClampToEdge,
            sampleCount: 1,
            generateMipmaps: false);

        _colorTexture = new GLTexture(_gl, _state, textureDescription, default);

        try
        {
            _gl.GenFramebuffers(1, out _framebuffer);
            if (_framebuffer == 0)
                throw new InvalidOperationException("OpenGL failed to create a framebuffer object.");

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                _colorTexture.Handle,
                0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if ((uint)status != (uint)GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException(
                    $"OpenGL framebuffer is incomplete. Status: 0x{(uint)status:X}.");
            }
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    internal void Bind()
    {
        ThrowIfDisposed();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_framebuffer != 0)
        {
            _gl.DeleteFramebuffers(1, in _framebuffer);
            _framebuffer = 0;
        }

        _colorTexture?.Dispose();
        _colorTexture = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
