// ============================================================================
//  Shader.cs
// ============================================================================
//  Renderer-neutral .shader asset. Backend programs are created lazily through
//  ShaderProgram and the active IGraphicsDevice.
// ============================================================================

using System.Numerics;
using System.Text;
using Void.Engine.Graphics.Rendering;
using Void.Engine.Graphics.Shaders;

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// Shader asset containing vertex and fragment source. Existing files remain
/// compatible with [vertex]/[fragment] and default to GLSL.
/// </summary>
public sealed class Shader : IAsset, IShader
{
    private ShaderProgram _program;
    private string _vertexSource;
    private string _fragmentSource;
    private ShaderLanguage _language = ShaderLanguage.Glsl;

    public uint Id { get; }
    public string Tag { get; }
    public byte[] Data { get; }
    public bool IsValid { get; private set; }
    public AssetType Type => AssetType.Normal;
    public DateTime LastAccessTime { get; set; }

    /// <summary>Gets the renderer-neutral runtime shader program.</summary>
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

    ~Shader() => Dispose();

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

    public void Unload()
    {
        if (!IsValid && _program == null)
            return;

        _program?.Dispose();
        _program = null;
        IsValid = false;
    }

    public void Dispose()
    {
        Unload();
        GC.SuppressFinalize(this);
    }

    public void SetUniform(string name, float value) => EnsureProgram().SetUniform(name, value);
    public void SetUniform(string name, int value) => EnsureProgram().SetUniform(name, value);
    public void SetUniform(string name, Vect2 value) => EnsureProgram().SetUniform(name, value);
    public void SetUniform(string name, Vect3 value) => EnsureProgram().SetUniform(name, value);
    public void SetUniform(string name, Vect4 value) => EnsureProgram().SetUniform(name, value);
    public void SetUniform(string name, Color color) => EnsureProgram().SetUniform(name, color);
    public void SetUniform(string name, Texture texture) => EnsureProgram().SetUniform(name, texture);
    public void SetUniform(string name, Matrix4x4 matrix) => EnsureProgram().SetUniform(name, matrix);
    public void SetCurrentTexture(string name) => EnsureProgram().SetCurrentTexture(name);
    public void Bind() => EnsureProgram().Bind();
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

    public override string ToString()
        => $"ShaderAsset({Id}, {Tag}, {IsValid}, {_language})";
}
