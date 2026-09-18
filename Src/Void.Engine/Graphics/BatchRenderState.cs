// ============================================================================
//  BatchRenderState.cs
// ============================================================================
//  Renderer-neutral state passed from VOID batchers to graphics backends.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics;

/// <summary>
/// Describes the renderer-neutral state applied when a batch submits geometry.
/// </summary>
/// <remarks>
/// Graphics backends consume this state without exposing backend-owned resources
/// through VOID's game-facing <see cref="Texture"/>, <see cref="Font"/>, or shader APIs.
/// </remarks>
public sealed class BatchRenderState
{
    /// <summary>Gets or sets the blend mode used for the submission.</summary>
    public IBlendMode BlendMode { get; set; } = Graphics.BlendMode.Alpha;

    /// <summary>Gets or sets the game-facing texture used for the submission.</summary>
    public Texture Texture { get; set; }

    /// <summary>Gets or sets the shader used for the submission.</summary>
    public IShader Shader { get; set; }

    /// <summary>
    /// Gets or sets the VOID-owned world-to-clip transform used by the built-in 2D pipeline.
    /// </summary>
    public Matrix ViewProjection { get; set; } = Matrix.Identity;

    /// <summary>
    /// Gets or sets the optional scissor rectangle in logical viewport coordinates.
    /// </summary>
    /// <remarks>
    /// The rectangle uses VOID's top-left coordinate convention. A null value disables
    /// scissor clipping for the submission.
    /// </remarks>
    public Rect2? ScissorRectangle { get; set; }

    // Font remains distinct from game Texture so DrawText(Font ...) stays fully
    // extensible for custom font implementations.
    internal Font Font { get; set; }

    // Resolves the backend-owned texture while keeping renderer resources out of
    // the game-facing Texture and Font APIs.
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
