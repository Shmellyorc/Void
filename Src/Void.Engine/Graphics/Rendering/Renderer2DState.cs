// ============================================================================
//  Renderer2DState.cs
// ============================================================================
//  Resolves either a custom renderer-neutral shader or the backend default 2D
//  shader for a batch draw.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

internal static class Renderer2DState
{
    internal static bool TryPrepareShader(
        BatchRenderState state,
        out IGraphicsShaderProgram shader,
        out IGraphicsTexture texture)
    {
        ArgumentNullException.ThrowIfNull(state);

        shader = null;
        texture = null;
        state.TryGetGraphicsTexture(out texture);

        ShaderProgram customProgram = state.Shader?.Program ?? ShaderState.GetCurrent();
        if (customProgram != null)
            return customProgram.TryPrepareForDraw(texture, state.ViewProjection, out shader);

        if (!RendererRuntime.TryGetDefault2DShader(out shader))
            return false;

        shader.SetUniform("uViewProjection", state.ViewProjection);

        bool hasTexture = texture != null;
        shader.SetUniform("uUseTexture", hasTexture ? 1 : 0);

        if (hasTexture)
        {
            shader.SetUniform("uTextureSize", new Vect2(texture.Description.Width, texture.Description.Height));
            shader.SetTexture("uTexture", texture);
        }

        return true;
    }
}
