// ============================================================================
//  ShaderProgram.cs
// ============================================================================
//  Internal runtime GPU shader program wrapper. Handles compilation,
//  uniform management, and shader binding.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using Void.Engine.Logs;

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Internal runtime GPU shader program wrapper.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ShaderProgram"/> class wraps an SFML shader and provides
/// methods for setting uniforms, binding, and unbinding. It is used internally
/// by the engine and is not intended for direct use by game code.
/// </para>
/// <para>
/// Shader programs are created from vertex and fragment shader source strings.
/// Compilation is lazy - the shader is only compiled when first used.
/// </para>
/// </remarks>
public sealed class ShaderProgram : IDisposable
{
    private SFShader _shader;
    private readonly string _vertexSource;
    private readonly string _fragmentSource;
    private bool _disposed;
    private bool _isCompiled;

    /// <summary>
    /// Gets the underlying SFML shader.
    /// </summary>
    internal SFShader SFShader => EnsureCompiled();

    /// <summary>
    /// Gets a value indicating whether the shader program is valid and compiled.
    /// </summary>
    public bool IsValid => !_disposed && _isCompiled && _shader != null && !_shader.IsInvalid;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShaderProgram"/> class from source strings.
    /// </summary>
    /// <param name="vertexSource">The vertex shader source code.</param>
    /// <param name="fragmentSource">The fragment shader source code.</param>
    internal ShaderProgram(string vertexSource, string fragmentSource)
    {
        _vertexSource = vertexSource;
        _fragmentSource = fragmentSource;
    }

    /// <summary>
    /// Ensures the shader is compiled, compiling it if necessary.
    /// </summary>
    /// <returns>The compiled SFML shader.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the shader program has been disposed.</exception>
    private SFShader EnsureCompiled()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ShaderProgram));

        if (_isCompiled)
            return _shader;

        try
        {
            _shader = SFShader.FromString(_vertexSource, null, _fragmentSource);
            _isCompiled = true;
            
            Logger.Instance.DebugWithCategory("ShaderProgram", 
                "Shader compiled successfully. Vertex: {0} chars, Fragment: {1} chars", 
                _vertexSource?.Length ?? 0, _fragmentSource?.Length ?? 0);
        }
        catch (Exception ex)
        {
            Logger.Instance.ErrorWithCategory("ShaderProgram", 
                "Failed to compile shader: {0}", ex.Message);
            throw;
        }

        return _shader;
    }

    /// <summary>
    /// Ensures the shader program is valid for the specified operation.
    /// </summary>
    /// <param name="operation">The operation being attempted (for logging).</param>
    /// <returns>True if the shader is valid, false otherwise.</returns>
    private bool EnsureValid(string operation)
    {
        if (_disposed)
        {
            Logger.Instance.WarningWithCategory("ShaderProgram", 
                "Cannot {0} - shader program is disposed.", operation);
            return false;
        }

        // Try to compile if not compiled yet
        if (!_isCompiled)
        {
            try
            {
                EnsureCompiled();
            }
            catch (Exception ex)
            {
                Logger.Instance.WarningWithCategory("ShaderProgram", 
                    "Cannot {0} - compilation failed: {1}", operation, ex.Message);
                return false;
            }
        }

        if (_shader == null || _shader.IsInvalid)
        {
            Logger.Instance.WarningWithCategory("ShaderProgram", 
                "Cannot {0} - shader program is invalid.", operation);
            return false;
        }

        return true;
    }

    #region Uniform Setters

    /// <summary>
    /// Sets a float uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The float value.</param>
    public void SetUniform(string name, float value)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, value);
    }

    /// <summary>
    /// Sets an integer uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The integer value.</param>
    public void SetUniform(string name, int value)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, value);
    }

    /// <summary>
    /// Sets a 2D vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The vector value.</param>
    public void SetUniform(string name, Vect2 value)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, new SFVec2(value.X, value.Y));
    }

    /// <summary>
    /// Sets a 3D vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The vector value.</param>
    public void SetUniform(string name, Vect3 value)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, new SFVec3(value.X, value.Y, value.Z));
    }

    /// <summary>
    /// Sets a 4D vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The vector value.</param>
    public void SetUniform(string name, Vect4 value)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, new SFVec4(value.X, value.Y, value.Z, value.W));
    }

    /// <summary>
    /// Sets a color uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="color">The color value.</param>
    public void SetUniform(string name, Color color)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, color);
    }

    /// <summary>
    /// Sets a texture uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="texture">The texture value.</param>
    public void SetUniform(string name, SFTexture texture)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, texture);
    }

    /// <summary>
    /// Sets a current texture type uniform.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="currentTexture">The current texture type.</param>
    public void SetUniform(string name, SFShader.CurrentTextureType currentTexture)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, currentTexture);
    }

    /// <summary>
    /// Sets a 4x4 matrix uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="matrix">The matrix value.</param>
    public void SetUniform(string name, Matrix4x4 matrix)
    {
        if (!EnsureValid($"set uniform '{name}'")) return;
        _shader.SetUniform(name, new SFMat4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        ));
    }

    #endregion

    /// <summary>
    /// Binds the shader program to the graphics pipeline.
    /// </summary>
    public void Bind()
    {
        if (!EnsureValid("bind shader")) return;
        ShaderState.Bind(_shader);
    }

    /// <summary>
    /// Unbinds the shader program from the graphics pipeline.
    /// </summary>
    public void Unbind()
    {
        if (_disposed)
        {
            Logger.Instance.WarningWithCategory("ShaderProgram", 
                "Cannot unbind shader - shader program is disposed.");
            return;
        }

        ShaderState.Unbind();
    }

    /// <summary>
    /// Disposes the shader program and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _shader?.Dispose();
        _disposed = true;

        Logger.Instance.DebugWithCategory("ShaderProgram", "Shader program disposed.");

        GC.SuppressFinalize(this);
    }
}