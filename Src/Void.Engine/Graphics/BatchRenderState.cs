// ============================================================================
//  BatchRenderState.cs
// ============================================================================
//  Backend-neutral render state used by VOID batchers.
// ============================================================================

using System.Numerics;
using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics;

/// <summary>
/// Describes the high-level state applied when a batch issues a draw.
/// </summary>
public sealed class BatchRenderState
{
    public IBlendMode BlendMode { get; set; } = Graphics.BlendMode.Alpha;
    public Texture Texture { get; set; }
    public IShader Shader { get; set; }

    /// <summary>World-to-clip transform used by the built-in 2D shader.</summary>
    public Matrix4x4 ViewProjection { get; set; } = Matrix4x4.Identity;

    // Font remains distinct from game Texture so DrawText(Font ...) stays fully
    // extensible for custom font implementations.
    internal Font Font { get; set; }

    /// <summary>
    /// Resolves the renderer-owned texture without exposing backend objects to
    /// game-facing Texture or Font APIs.
    /// </summary>
    internal bool TryGetGraphicsTexture(out IGraphicsTexture texture)
    {
        if (Texture != null)
            return Texture.TryGetGraphicsTexture(out texture);

        if (Font != null)
            return Font.TryGetGraphicsTexture(out texture);

        texture = null;
        return false;
    }
}
