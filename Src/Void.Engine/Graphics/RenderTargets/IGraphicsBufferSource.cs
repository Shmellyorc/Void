using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.RenderTargets;

/// <summary>
/// Internal bridge used while VOID transitions its legacy render-target path to
/// the pluggable graphics device. It deliberately does not expand the public
/// IVertexBuffer contract.
/// </summary>
internal interface IGraphicsBufferSource
{
    bool TryGetGraphicsBuffer(out IGraphicsBuffer buffer);
}
