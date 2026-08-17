using Void.Engine.Graphics.RenderTargets;
using Void.Engine.Graphics.Shaders;

namespace Void.Engine.Graphics;

public class PostProcessor : IDisposable
{
    private readonly IShader _shader;
    private readonly SpriteBatcher _batcher;
    private IRenderTarget _renderTarget;
    private bool _disposed;

    public PostProcessor(IShader shader, Vect2 size)
    {
        _shader = shader;
        _batcher = new SpriteBatcher();
        _renderTarget = RenderTarget.Get((int)size.X, (int)size.Y);
    }

    public void Apply(IRenderTarget sourceTarget, Camera camera = null)
    {
        if (_disposed || _shader == null || !_shader.IsValid)
            return;

        Texture sourceTexture = sourceTarget?.GetTexture();

        if (sourceTexture == null)
            return;

        try
        {
            _batcher.SetRenderTarget(_renderTarget);
            _batcher.SetShader(_shader);
            _batcher.Begin(SortMode.Immediate, BlendMode.None, camera);

            _batcher.DrawBypassAtlas(
                sourceTexture,
                new Rect2(0, 0, _renderTarget.Width, _renderTarget.Height),
                sourceTexture.Bounds,
                Color.White,
                0f
            );

            _batcher.End();

            _renderTarget.Display();
        }
        finally
        {
            sourceTexture.Dispose();
        }
    }

    public Texture GetResultTexture()
    {
        if (_disposed) return null;
        return _renderTarget?.GetTexture();
    }

    public IRenderTarget GetResultTarget() => _renderTarget;

    public void Resize(int width, int height)
    {
        if (_disposed) return;
        _renderTarget = RenderTarget.Resize(_renderTarget, width, height);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _batcher?.Dispose();

        if (_renderTarget != null)
        {
            RenderTarget.Return(_renderTarget);
            _renderTarget = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}