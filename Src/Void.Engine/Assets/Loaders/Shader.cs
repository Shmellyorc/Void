// ============================================================================
//  Shader.cs
// ============================================================================
//  Renderer-neutral shader asset loaded from VOID .shader files.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Text;
using Void.Engine.Graphics.Rendering;
using Void.Engine.Graphics.Shaders;

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Represents a renderer-neutral shader asset containing vertex and fragment source code.
/// </summary>
/// <remarks>
/// <para>
/// VOID shader files use <c>[vertex]</c> and <c>[fragment]</c> sections. An optional
/// <c>[language]</c> section selects the shader language; GLSL is used when no language is specified.
/// The active renderer creates the backend program lazily through <see cref="ShaderProgram"/>.
/// </para>
/// <para>
/// Uniform values can be assigned directly on the asset before it is submitted through a batcher.
/// </para>
/// <code>
/// Shader shader = AssetManager.Instance.Load&lt;Shader&gt;("Shaders/wave.shader");
/// shader.SetUniform("uTime", elapsedSeconds);
///
/// spriteBatch.SetShader(shader);
/// spriteBatch.Begin();
/// spriteBatch.Draw(texture, position, Color.White);
/// spriteBatch.End();
/// spriteBatch.ClearShader();
/// </code>
/// </remarks>
public sealed class Shader : IAsset, IShader
{
    private ShaderProgram _program;
    private string _vertexSource;
    private string _fragmentSource;
    private ShaderLanguage _language = ShaderLanguage.Glsl;

    /// <summary>
    /// Gets the identifier assigned to this shader asset.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized asset tag or source path.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the original encoded bytes of the VOID shader file.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets whether the shader source has been parsed and a runtime program is available.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the lifecycle type for this asset.
    /// </summary>
    public AssetType Type => AssetType.Normal;

    /// <summary>
    /// Gets or sets the most recent time this shader was accessed.
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets the renderer-neutral runtime shader program, loading the asset when necessary.
    /// </summary>
    public ShaderProgram Program
    {
        get
        {
            if (_program == null && !IsValid)
                Load();
            return _program;
        }
    }

    internal Shader(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data ?? Array.Empty<byte>();
        Tag = tag;
        LastAccessTime = DateTime.Now;
    }

    /// <summary>
    /// Releases shader resources if the asset was not disposed explicitly.
    /// </summary>
    ~Shader() => Dispose();

    /// <summary>
    /// Parses the shader source and creates the renderer-neutral runtime program.
    /// </summary>
    /// <remarks>
    /// Calling this method on an already loaded shader refreshes <see cref="LastAccessTime"/> without rebuilding the program.
    /// </remarks>
    public void Load()
    {
        if (IsValid)
        {
            LastAccessTime = DateTime.Now;
            return;
        }

        if (string.IsNullOrEmpty(_vertexSource) && string.IsNullOrEmpty(_fragmentSource))
            (_language, _vertexSource, _fragmentSource) = ParseShader(Encoding.UTF8.GetString(Data));

        _program = new ShaderProgram(_vertexSource, _fragmentSource, _language);
        LastAccessTime = DateTime.Now;
        IsValid = true;
    }

    /// <summary>
    /// Releases the runtime shader program while retaining the source data for later reloading.
    /// </summary>
    public void Unload()
    {
        if (!IsValid && _program == null)
            return;

        _program?.Dispose();
        _program = null;
        IsValid = false;
    }

    /// <summary>
    /// Releases resources owned by this shader asset.
    /// </summary>
    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sets a floating-point uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to assign.</param>
    public void SetUniform(string name, float value) => EnsureProgram().SetUniform(name, value);

    /// <summary>
    /// Sets an integer uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to assign.</param>
    public void SetUniform(string name, int value) => EnsureProgram().SetUniform(name, value);

    /// <summary>
    /// Sets a two-component vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to assign.</param>
    public void SetUniform(string name, Vect2 value) => EnsureProgram().SetUniform(name, value);

    /// <summary>
    /// Sets a three-component vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to assign.</param>
    public void SetUniform(string name, Vect3 value) => EnsureProgram().SetUniform(name, value);

    /// <summary>
    /// Sets a four-component vector uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The value to assign.</param>
    public void SetUniform(string name, Vect4 value) => EnsureProgram().SetUniform(name, value);

    /// <summary>
    /// Sets a color uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="color">The color to assign.</param>
    public void SetUniform(string name, Color color) => EnsureProgram().SetUniform(name, color);

    /// <summary>
    /// Sets a texture uniform value.
    /// </summary>
    /// <param name="name">The sampler uniform name.</param>
    /// <param name="texture">The texture to assign.</param>
    public void SetUniform(string name, Texture texture) => EnsureProgram().SetUniform(name, texture);

    /// <summary>
    /// Sets a VOID-owned 4x4 matrix uniform value.
    /// </summary>
    /// <param name="name">The uniform name.</param>
    /// <param name="matrix">The matrix to assign.</param>
    public void SetUniform(string name, Matrix matrix) => EnsureProgram().SetUniform(name, matrix);

    /// <summary>
    /// Marks a sampler uniform to use the texture supplied by the current draw operation.
    /// </summary>
    /// <param name="name">The sampler uniform name.</param>
    public void SetCurrentTexture(string name) => EnsureProgram().SetCurrentTexture(name);

    /// <summary>
    /// Binds the runtime shader program on the active graphics device.
    /// </summary>
    public void Bind() => EnsureProgram().Bind();

    /// <summary>
    /// Unbinds the runtime shader program when one has been created.
    /// </summary>
    public void Unbind() => _program?.Unbind();

    bool IShader.IsValid => IsValid && _program != null && _program.IsValid;

    private ShaderProgram EnsureProgram()
    {
        if (_program == null)
            Load();

        LastAccessTime = DateTime.Now;
        return _program;
    }

    private static (ShaderLanguage language, string vertex, string fragment) ParseShader(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("Shader source cannot be null or empty.");

        source = source.Replace("\r\n", "\n").Replace('\r', '\n');

        var vertex = new StringBuilder();
        var fragment = new StringBuilder();
        var language = new StringBuilder();
        StringBuilder current = null;

        foreach (string rawLine in source.Split('\n'))
        {
            string tag = rawLine.Trim();

            if (tag.Equals("[vertex]", StringComparison.OrdinalIgnoreCase))
            {
                current = vertex;
                continue;
            }

            if (tag.Equals("[fragment]", StringComparison.OrdinalIgnoreCase))
            {
                current = fragment;
                continue;
            }

            if (tag.Equals("[language]", StringComparison.OrdinalIgnoreCase))
            {
                current = language;
                continue;
            }

            current?.AppendLine(rawLine);
        }

        string vertexSource = vertex.ToString().Trim();
        string fragmentSource = fragment.ToString().Trim();

        if (vertexSource.Length == 0)
            throw new InvalidOperationException("Shader file is missing a non-empty [vertex] section.");
        if (fragmentSource.Length == 0)
            throw new InvalidOperationException("Shader file is missing a non-empty [fragment] section.");

        ShaderLanguage shaderLanguage = ParseLanguage(language.ToString().Trim());
        return (shaderLanguage, vertexSource, fragmentSource);
    }

    private static ShaderLanguage ParseLanguage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ShaderLanguage.Glsl;

        string normalized = value.Trim().Replace("-", string.Empty).Replace("_", string.Empty);
        return normalized.ToLowerInvariant() switch
        {
            "glsl" => ShaderLanguage.Glsl,
            "hlsl" => ShaderLanguage.Hlsl,
            "spirv" => ShaderLanguage.SpirV,
            "dxil" => ShaderLanguage.Dxil,
            "msl" => ShaderLanguage.Msl,
            "custom" => ShaderLanguage.Custom,
            _ => throw new InvalidOperationException($"Unknown shader language '{value}'.")
        };
    }

    /// <summary>
    /// Returns a diagnostic string describing this shader asset.
    /// </summary>
    /// <returns>A string containing the asset identifier, tag, validity, and shader language.</returns>
    public override string ToString()
        => $"ShaderAsset({Id}, {Tag}, {IsValid}, {_language})";
}
