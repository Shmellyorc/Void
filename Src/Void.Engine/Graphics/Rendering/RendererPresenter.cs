namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Renderer-neutral final blit used by Window to scale the engine render target
/// into the native backbuffer. It uses the backend's built-in 2D program.
/// </summary>
internal sealed class RendererPresenter : IDisposable
{
    private readonly IGraphicsDevice _device;
    private readonly IGraphicsShaderProgram _shader;
    private readonly IGraphicsBuffer _vertexBuffer;
    private readonly Vertex[] _vertices = new Vertex[6];
    private bool _disposed;

    internal RendererPresenter(IGraphicsDevice device, IGraphicsShaderProgram shader)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));

        _vertexBuffer = device.CreateBuffer(new BufferDescription(
            checked(6 * Vertex.Layout.Stride),
            BufferType.Vertex,
            BufferUsage.Stream,
            Vertex.Layout));
    }

    internal void Present(
        IGraphicsTexture source,
        Rect2 destination,
        int backbufferWidth,
        int backbufferHeight)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);

        if (backbufferWidth <= 0 || backbufferHeight <= 0 || destination.Width <= 0f || destination.Height <= 0f)
            return;

        float left = destination.Left;
        float top = destination.Top;
        float right = destination.Right;
        float bottom = destination.Bottom;

        float sourceWidth = source.Description.Width;
        float sourceHeight = source.Description.Height;

        // Render-target textures are vertically inverted relative to VOID's
        // top-left coordinate convention. Flip V only for this final present.
        Vect2 uvTL = new(0f, sourceHeight);
        Vect2 uvTR = new(sourceWidth, sourceHeight);
        Vect2 uvBR = new(sourceWidth, 0f);
        Vect2 uvBL = new(0f, 0f);
        Color white = Color.White;

        _vertices[0] = new Vertex(new Vect2(left, top), white, uvTL);
        _vertices[1] = new Vertex(new Vect2(right, top), white, uvTR);
        _vertices[2] = new Vertex(new Vect2(right, bottom), white, uvBR);
        _vertices[3] = new Vertex(new Vect2(left, top), white, uvTL);
        _vertices[4] = new Vertex(new Vect2(right, bottom), white, uvBR);
        _vertices[5] = new Vertex(new Vect2(left, bottom), white, uvBL);

        _device.UpdateBuffer(_vertexBuffer, _vertices);
        _device.SetRenderTarget(null);

        _shader.SetUniform("uViewProjection", CreatePixelProjection(backbufferWidth, backbufferHeight));
        _shader.SetUniform("uUseTexture", 1);
        _shader.SetUniform("uTextureSize", new Vect2(sourceWidth, sourceHeight));
        _shader.SetTexture("uTexture", source);

        var command = new RenderCommand(
            _vertexBuffer,
            PrimitiveType.Triangles,
            0,
            6,
            global::Void.Engine.Graphics.BlendMode.None,
            source,
            _shader);

        _device.Draw(command);
    }

    private static Matrix4x4 CreatePixelProjection(int width, int height)
    {
        float scaleX = 2f / width;
        float scaleY = -2f / height;

        return new Matrix4x4(
            scaleX, 0f, 0f, 0f,
            0f, scaleY, 0f, 0f,
            0f, 0f, 1f, 0f,
            -1f, 1f, 0f, 1f);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _vertexBuffer.Dispose();
        _disposed = true;
    }
}
