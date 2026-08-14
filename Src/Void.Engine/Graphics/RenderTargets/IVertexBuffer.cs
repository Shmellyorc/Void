using Void.Engine.Graphics.RenderTargets;

public interface IVertexBuffer
{
    SFPrimitiveType PrimitiveType { get; set; }
    void Update(ReadOnlySpan<SFVertex> vertices, uint vertexCount, uint offset);
    void Draw(IRenderTarget target, uint vertexStart, uint vertexCount, SFRenderStates states);
    void Dispose();
}