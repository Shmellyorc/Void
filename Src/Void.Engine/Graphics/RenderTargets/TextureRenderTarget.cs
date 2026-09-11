// ============================================================================
//  TextureRenderTarget.cs
// ============================================================================
//  Renderer-owned off-screen target. No SFML render texture is involved.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.RenderTargets;

internal sealed class TextureRenderTarget : IRenderTarget
{
    private readonly IGraphicsDevice _device;
    private readonly IGraphicsRenderTarget _graphicsTarget;
    private readonly Texture _texture;
    private readonly bool _sRGB;
    private bool _disposed;

    public int Width => _graphicsTarget.Description.Width;
    public int Height => _graphicsTarget.Description.Height;
    public bool Srgb => _sRGB;
    public Vect2 Size => new(Width, Height);

    internal IGraphicsDevice GraphicsDevice => _device;
    internal IGraphicsRenderTarget GraphicsRenderTarget => _graphicsTarget;
    internal IGraphicsTexture GraphicsTexture => _graphicsTarget.ColorTexture;

    internal TextureRenderTarget(int width, int height, bool sRGB = false)
    {
        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice device))
            throw new InvalidOperationException("A renderer must be initialized before creating a render target.");

        _device = device;
        _sRGB = sRGB;
        _graphicsTarget = _device.CreateRenderTarget(new RenderTargetDescription(
            width,
            height,
            sRGB ? TextureFormat.SRgba8 : TextureFormat.RGBA8));

        // The render target owns ColorTexture. This game-facing wrapper does not.
        _texture = new Texture(_device, _graphicsTarget.ColorTexture, AssetType.Atlas);
    }

    public Texture GetTexture()
    {
        ThrowIfDisposed();
        return _texture;
    }

    public void Clear(Color color)
    {
        ThrowIfDisposed();
        _device.SetRenderTarget(_graphicsTarget);
        _device.Clear(color);
    }

    public void Display()
    {
        ThrowIfDisposed();
        // Renderer-owned FBO textures are immediately available after drawing.
    }

    public void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, BatchRenderState states)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(buffer);
        buffer.Draw(this, vertexStart, vertexCount, states);
    }

    public void SetView(Camera camera)
    {
        ThrowIfDisposed();
        // Camera transforms are carried in BatchRenderState.ViewProjection.
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _texture.Dispose(); // non-owning wrapper
        _graphicsTarget.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
