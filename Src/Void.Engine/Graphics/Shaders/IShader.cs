// ============================================================================
//  IShader.cs
// ============================================================================
//  Renderer-neutral shader contract used by VOID's batching pipeline.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Defines a renderer-neutral shader that can be selected by VOID's batching pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Implementations should expose their renderer-neutral <see cref="ShaderProgram"/> through
/// <see cref="Program"/> so VOID can create and prepare the backend-owned shader program on
/// the active graphics device. The default <see cref="Program"/> implementation returns
/// <see langword="null"/> for source compatibility and therefore does not replace VOID's
/// renderer shader during batch submission.
/// </para>
/// <para>
/// The texture and current-draw-texture setters also have no-op default implementations.
/// Implementations that support sampler uniforms should override them and forward the values
/// to their <see cref="ShaderProgram"/> or equivalent renderer-neutral state.
/// </para>
/// </remarks>
public interface IShader
{
    /// <summary>Sets a floating-point uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, float value);

    /// <summary>Sets an integer uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, int value);

    /// <summary>Sets a two-component vector uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect2 value);

    /// <summary>Sets a three-component vector uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect3 value);

    /// <summary>Sets a four-component vector uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    void SetUniform(string name, Vect4 value);

    /// <summary>Sets a color uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="color">The color to assign.</param>
    void SetUniform(string name, Color color);

    /// <summary>
    /// Binds a game-facing texture to a named sampler uniform.
    /// </summary>
    /// <param name="name">The sampler uniform name.</param>
    /// <param name="texture">The texture to assign, or <see langword="null"/> to clear the existing texture assignment.</param>
    /// <remarks>
    /// The default implementation does nothing. Implementations that support texture uniforms
    /// should override this member, retain renderer-neutral texture state until submission, and
    /// treat a <see langword="null"/> texture as removal of the previous assignment for that sampler name.
    /// </remarks>
    void SetUniform(string name, Texture texture) { }

    /// <summary>Sets a 4x4 matrix uniform value.</summary>
    /// <param name="name">The shader uniform name.</param>
    /// <param name="matrix">The matrix to assign.</param>
    void SetUniform(string name, Matrix4x4 matrix);

    /// <summary>
    /// Marks a sampler uniform as using the texture associated with the current draw command.
    /// </summary>
    /// <param name="name">The sampler uniform name.</param>
    /// <remarks>
    /// VOID supplies the conventional <c>uTexture</c> sampler automatically for its 2D draw path.
    /// The default implementation does nothing. Override this member when an additional sampler
    /// should follow the current draw texture.
    /// </remarks>
    void SetCurrentTexture(string name) { }

    /// <summary>
    /// Makes this shader the explicitly bound shader for renderer-neutral drawing state.
    /// </summary>
    /// <remarks>
    /// Batch-level shader selection through the batcher's <c>SetShader</c> API takes precedence
    /// over explicitly bound shader state.
    /// </remarks>
    void Bind();

    /// <summary>
    /// Removes this shader from explicitly bound renderer-neutral drawing state.
    /// </summary>
    void Unbind();

    /// <summary>
    /// Gets whether this shader is currently valid for use.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the renderer-neutral shader program used by VOID's batching pipeline.
    /// </summary>
    /// <remarks>
    /// The default implementation returns <see langword="null"/>. Custom shader implementations
    /// that should replace VOID's backend shader during batch submission must override this property
    /// and return a usable <see cref="ShaderProgram"/>.
    /// </remarks>
    ShaderProgram Program => null;
}
