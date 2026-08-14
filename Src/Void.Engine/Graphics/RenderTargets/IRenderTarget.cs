namespace Void.Engine.Graphics.RenderTargets;

public interface IRenderTarget
{
    void Clear(Color color);
    void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, SFRenderStates states);
    void Display();
    void SetView(Camera camera);
    Vect2 Size { get; }
    int Width { get; }
    int Height { get; }
    bool Srgb { get; }
}