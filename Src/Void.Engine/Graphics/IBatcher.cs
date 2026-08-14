using Void.Engine.Graphics.RenderTargets;

namespace Void.Engine.Graphics;

[Flags]
public enum TextureEffects
{
    None = 0,
    Horizontal = 1 << 0,
    Vertical = 1 << 1
}

public enum SortMode
{
    Immediate,
    BackToFront,
    FrontToBack,
    Deferred
}

public interface IBatcher : IDisposable
{
    void Begin(SortMode? sort = null, IBlendMode blendMode = null, Camera camera = null, IRenderTarget renderTarget = null);
    void End();
    void Flush();

    bool IsDrawing { get; }
    int DrawCallCount { get; }
    int VertexCount { get; }
    int CommandCount { get; }

    string Name { get; }
    BatchStats Stats { get; }
}
