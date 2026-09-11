// ============================================================================
//  IShader.cs
// ============================================================================
//  Renderer-neutral shader contract used by VOID's batching pipeline.
// ============================================================================

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Defines a game-facing shader without exposing OpenGL, Vulkan, Direct3D,
/// Metal, or SFML shader objects.
/// </summary>
public interface IShader
{
    void SetUniform(string name, float value);
    void SetUniform(string name, int value);
    void SetUniform(string name, Vect2 value);
    void SetUniform(string name, Vect3 value);
    void SetUniform(string name, Vect4 value);
    void SetUniform(string name, Color color);

    /// <summary>
    /// Binds a game-facing texture to a named shader uniform.
    /// The active renderer resolves the backend texture lazily.
    /// </summary>
    void SetUniform(string name, Texture texture) { }

    void SetUniform(string name, Matrix4x4 matrix);

    /// <summary>
    /// Marks a sampler uniform as using the texture associated with the current
    /// draw command. VOID also binds the conventional <c>uTexture</c> uniform
    /// automatically for compatibility with existing SpriteBatcher shaders.
    /// </summary>
    void SetCurrentTexture(string name) { }

    void Bind();
    void Unbind();

    bool IsValid { get; }

    /// <summary>
    /// Gets the renderer-neutral program used by the batching pipeline.
    /// Existing third-party IShader implementations remain source-compatible
    /// through this default implementation, but must expose a program to render
    /// through VOID's pluggable renderer path.
    /// </summary>
    ShaderProgram Program => null;
}
