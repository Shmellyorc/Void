// ============================================================================
//  PostProcessor.cs
// ============================================================================
//  Applies post-processing effects to rendered content using shaders.
//  Renders a source texture through a shader to a render target for
//  effects such as bloom, blur, color grading, and custom filters.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Applies post-processing effects to rendered content using shaders.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PostProcessor"/> class renders a source texture through
/// a shader to a render target, enabling post-processing effects such as:
/// <list type="bullet">
///   <item><description>Bloom and glow effects</description></item>
///   <item><description>Blur and depth of field</description></item>
///   <item><description>Color grading and tone mapping</description></item>
///   <item><description>Custom screen-space effects</description></item>
/// </list>
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Creates a render target for the post-processed result</description></item>
///   <item><description>When <see cref="Apply"/> is called, renders the source texture</description></item>
///   <item><description>Uses the provided shader to process the texture</description></item>
///   <item><description>Results are available via <see cref="GetResultTexture"/> or <see cref="GetResultTarget"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a post-processor with a bloom shader
/// var bloomShader = AssetManager.Instance.Load&lt;ShaderAsset&gt;("shaders/bloom.shader");
/// var postProcessor = new PostProcessor(bloomShader, new Vect2(1920, 1080));
/// 
/// // In your render loop:
/// // 1. Render your scene to a render target
/// // 2. Apply post-processing
/// postProcessor.Apply(sceneTarget, camera);
/// 
/// // 3. Draw the result
/// var result = postProcessor.GetResultTexture();
/// batcher.Draw(result, Vect2.Zero, Color.White);
/// 
/// // Clean up
/// postProcessor.Dispose();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public class PostProcessor : IDisposable
{
    private readonly IShader _shader;
    private readonly SpriteBatcher _batcher;
    private IRenderTarget _renderTarget;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostProcessor"/> class.
    /// </summary>
    /// <param name="shader">The shader to use for post-processing.</param>
    /// <param name="size">The size of the render target.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shader"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// The render target is allocated from the pool using <see cref="RenderTarget.Get(Vect2, bool)"/>.
    /// It will be automatically returned to the pool when the post-processor is disposed.
    /// </para>
    /// </remarks>
    public PostProcessor(IShader shader, Vect2 size)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _batcher = new SpriteBatcher();
        _renderTarget = RenderTarget.Get((int)size.X, (int)size.Y);
    }

    /// <summary>
    /// Applies the post-processing effect to the source render target.
    /// </summary>
    /// <param name="sourceTarget">The source render target containing the scene to process.</param>
    /// <param name="camera">The camera to use for rendering (optional).</param>
    /// <remarks>
    /// <para>
    /// This method renders the source texture through the shader to the
    /// post-processor's render target. The result can be retrieved using
    /// <see cref="GetResultTexture"/> or <see cref="GetResultTarget"/>.
    /// </para>
    /// <para>
    /// If the shader or source target is invalid, this method returns without
    /// performing any rendering.
    /// </para>
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

    /// <summary>
    /// Gets the resulting texture after post-processing.
    /// </summary>
    /// <returns>The processed texture, or <see langword="null"/> if the post-processor is disposed.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the texture from the post-processor's render target.
    /// The texture contains the scene with the post-processing effect applied.
    /// </para>
    /// <para>
    /// The returned texture can be used for further rendering operations or
    /// displayed directly to the screen.
    /// </para>
    /// </remarks>
    public Texture GetResultTexture()
    {
        if (_disposed) return null;
        return _renderTarget?.GetTexture();
    }

    /// <summary>
    /// Gets the render target containing the post-processed result.
    /// </summary>
    /// <returns>The render target with the processed result, or <see langword="null"/> if disposed.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the render target directly, allowing for more
    /// advanced use cases such as chaining multiple post-processors.
    /// </para>
    /// </remarks>
    public IRenderTarget GetResultTarget() => _renderTarget;

    /// <summary>
    /// Resizes the post-processor's render target.
    /// </summary>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    /// <remarks>
    /// <para>
    /// This method resizes the render target using <see cref="RenderTarget.Resize"/>.
    /// The existing render target is returned to the pool and a new one is acquired.
    /// </para>
    /// <para>
    /// This is useful for handling window resize events or dynamically changing
    /// the post-processing resolution.
    /// </para>
    /// </remarks>
    public void Resize(int width, int height)
    {
        if (_disposed) return;
        _renderTarget = RenderTarget.Resize(_renderTarget, width, height);
    }

    /// <summary>
    /// Disposes the post-processor and releases all resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method disposes the batcher and returns the render target to the pool.
    /// After disposal, the post-processor should not be used.
    /// </para>
    /// </remarks>
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