// ============================================================================
//  IShader.cs
// ============================================================================
//  Defines the contract for shader programs in the rendering pipeline.
//  Provides methods for setting uniforms, binding, and unbinding shaders.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Defines the contract for shader programs in the rendering pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IShader"/> interface provides methods for setting uniform
/// values, binding shaders to the graphics pipeline, and checking shader validity.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Set uniform values of various types (float, int, vectors, colors, textures, matrices)</description></item>
///   <item><description>Bind and unbind shaders for rendering</description></item>
///   <item><description>Check shader validity after compilation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a shader through AssetManager
/// var shader = AssetManager.Instance.Load&lt;Shader&gt;("shaders/glow.shader");
/// 
/// // Set uniforms
/// shader.SetUniform("uTime", 1.5f);
/// shader.SetUniform("uColor", Color.Red);
/// shader.SetUniform("uProjection", projectionMatrix);
/// 
/// // Bind and draw
/// shader.Bind();
/// // ... draw commands ...
/// shader.Unbind();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations are not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public interface IShader
{
    /// <summary>
    /// Sets a float uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="value">The float value to set.</param>
    void SetUniform(string name, float value);

    /// <summary>
    /// Sets an integer uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="value">The integer value to set.</param>
    void SetUniform(string name, int value);

    /// <summary>
    /// Sets a 2D vector uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="value">The vector value to set.</param>
    void SetUniform(string name, Vect2 value);

    /// <summary>
    /// Sets a 3D vector uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="value">The vector value to set.</param>
    void SetUniform(string name, Vect3 value);

    /// <summary>
    /// Sets a 4D vector uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="value">The vector value to set.</param>
    void SetUniform(string name, Vect4 value);

    /// <summary>
    /// Sets a color uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="color">The color value to set.</param>
    void SetUniform(string name, Color color);

    /// <summary>
    /// Sets a texture uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="texture">The SFML texture to bind.</param>
    void SetUniform(string name, SFTexture texture);

    /// <summary>
    /// Sets a 4x4 matrix uniform value on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="matrix">The matrix value to set.</param>
    void SetUniform(string name, Matrix4x4 matrix);

    /// <summary>
    /// Sets a current texture type uniform on the shader program.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader.</param>
    /// <param name="currentTexture">The current texture type.</param>
    void SetUniform(string name, SFShader.CurrentTextureType currentTexture);

    /// <summary>
    /// Binds the shader program to the graphics pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After binding, all subsequent draw calls will use this shader program
    /// until another shader is bound or <see cref="Unbind"/> is called.
    /// </para>
    /// <para>
    /// This method automatically tracks the currently bound shader to avoid
    /// redundant state changes.
    /// </para>
    /// </remarks>
    void Bind();

    /// <summary>
    /// Unbinds the shader program from the graphics pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After unbinding, the graphics pipeline will use the default shader
    /// or no shader for subsequent draw calls.
    /// </para>
    /// <para>
    /// This method should typically be called after rendering with the shader
    /// is complete to avoid unexpected behavior.
    /// </para>
    /// </remarks>
    void Unbind();

    /// <summary>
    /// Gets a value indicating whether the shader program is valid and compiled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the shader is valid and ready for use;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property indicates whether the shader was successfully compiled
    /// and is ready for rendering. If <see langword="false"/>, attempts to
    /// bind or set uniforms will fail silently.
    /// </para>
    /// <para>
    /// A shader may be invalid if compilation failed due to syntax errors,
    /// missing uniforms, or platform compatibility issues.
    /// </para>
    /// </remarks>
    bool IsValid { get; }
}