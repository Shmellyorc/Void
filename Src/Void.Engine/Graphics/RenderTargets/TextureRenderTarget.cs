using Void.Engine.Graphics.RenderTargets;

internal sealed class TextureRenderTarget : IRenderTarget
{
    private readonly SFRenderTexture _texture;
    private readonly Window _window;
    private readonly int _width;
    private readonly int _height;
    private readonly bool _sRGB;
    private bool _disposed;

    public int Width => _width;
    public int Height => _height;
    public bool Srgb => _sRGB;
    public Vect2 Size => new(_width, _height);

    internal SFRenderTexture RenderTexture => _window != null ? _window._renderTexture : _texture;

    internal TextureRenderTarget(int width, int height, bool sRGB = false)
    {
        _width = width;
        _height = height;
        _sRGB = sRGB;
        _texture = new SFRenderTexture((uint)width, (uint)height);
        _window = null;
    }

    internal TextureRenderTarget(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _texture = null;
        _width = (int)_window._renderTexture.Size.X;
        _height = (int)_window._renderTexture.Size.Y;
        _sRGB = false;
    }

    public Texture GetTexture()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TextureRenderTarget));
        
        var renderTexture = RenderTexture;
        if (renderTexture == null || renderTexture.IsInvalid)
            return null;
        
        var image = renderTexture.Texture.CopyToImage();
        
        var sfTexture = new SFTexture(image);
        image.Dispose();
        
        return new Texture(sfTexture);
    }

    public void Clear(Color color)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TextureRenderTarget));
        var texture = RenderTexture;
        if (texture == null || texture.IsInvalid) return;
        texture.Clear(color);
    }

    public void Display()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TextureRenderTarget));
        var texture = RenderTexture;
        if (texture == null || texture.IsInvalid) return;
        texture.Display();
    }

    public void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, SFRenderStates states)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TextureRenderTarget));
        buffer.Draw(this, vertexStart, vertexCount, states);
    }

    public void SetView(Camera camera)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TextureRenderTarget));
        var texture = RenderTexture;
        if (texture == null || texture.IsInvalid) return;
        texture.SetView(camera);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        if (_texture != null)
            _texture.Dispose();
            
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}