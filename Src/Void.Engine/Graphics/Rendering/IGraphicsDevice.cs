namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Backend-neutral GPU device used by VOID internals and implemented by custom renderers.
/// </summary>
public interface IGraphicsDevice : IDisposable
{
    RendererCapabilities Capabilities { get; }

    IGraphicsBuffer CreateBuffer(in BufferDescription description);

    IGraphicsTexture CreateTexture(
        in TextureDescription description,
        ReadOnlySpan<byte> initialData = default);

    IGraphicsShaderProgram CreateShaderProgram(in ShaderProgramDescription description);

    IGraphicsRenderTarget CreateRenderTarget(in RenderTargetDescription description);

    void UpdateBuffer<T>(
        IGraphicsBuffer buffer,
        ReadOnlySpan<T> data,
        int byteOffset = 0)
        where T : unmanaged;

    void UpdateTexture(
        IGraphicsTexture texture,
        int x,
        int y,
        int width,
        int height,
        TextureFormat sourceFormat,
        ReadOnlySpan<byte> data);

    void SetRenderTarget(IGraphicsRenderTarget target);
    void Clear(Color color);
    void Draw(in RenderCommand command);

    /// <summary>
    /// Waits until submitted GPU work is complete. Primarily used during teardown,
    /// device recreation, or backend-specific synchronization points.
    /// </summary>
    void WaitIdle();
}
