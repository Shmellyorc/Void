// ============================================================================
//  PostProcessor.cs
// ============================================================================
//  Shader-based full-target post-processing helper.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Applies a shader to the texture produced by a render target.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="PostProcessor"/> owns an internal <see cref="SpriteBatcher"/> and
/// pooled render target. <see cref="Apply"/> draws the source target's texture
/// through the supplied shader into that target, after which the result can be
/// retrieved as either a texture or render target.
/// </para>
/// <para><code>
/// using var post = new PostProcessor(shader, new Vect2(320, 180));
/// post.Apply(sceneTarget, camera);
/// Texture result = post.GetResultTexture();
/// </code></para>
/// </remarks>
public class PostProcessor : IDisposable
{
    private readonly IShader _shader;
    private readonly SpriteBatcher _batcher;
    private IRenderTarget _renderTarget;
    private bool _disposed;

    /// <summary>Initializes a post-processor and acquires its result render target.</summary>
    /// <param name="shader">Shader used while drawing the source texture.</param>
    /// <param name="size">Initial result-target size. Components are converted to integers.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shader"/> is null.</exception>
    public PostProcessor(IShader shader, Vect2 size)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _batcher = new SpriteBatcher();
        _renderTarget = RenderTarget.Get((int)size.X, (int)size.Y);
    }

    /// <summary>Applies the configured shader to a source render target.</summary>
    /// <param name="sourceTarget">Target whose texture will be processed.</param>
    /// <param name="camera">Optional camera passed to the internal sprite batch.</param>
    /// <remarks>
    /// The method returns without drawing when the processor is disposed, the shader
    /// is invalid, the source target is null, or the source target does not provide a texture.
    /// </remarks>
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

    /// <summary>Gets a texture view of the current processed result.</summary>
    /// <returns>The result texture, or null after the processor has been disposed.</returns>
    public Texture GetResultTexture()
    {
        if (_disposed) return null;
        return _renderTarget?.GetTexture();
    }

    /// <summary>Gets the render target that stores the processed result.</summary>
    /// <returns>The result target, or null after disposal.</returns>
    public IRenderTarget GetResultTarget() => _renderTarget;

    /// <summary>Resizes the pooled result target.</summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    /// <remarks>A call made after disposal is ignored.</remarks>
    public void Resize(int width, int height)
    {
        if (_disposed) return;
        _renderTarget = RenderTarget.Resize(_renderTarget, width, height);
    }

    /// <summary>Disposes the internal batcher and returns the result target to the pool.</summary>
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
