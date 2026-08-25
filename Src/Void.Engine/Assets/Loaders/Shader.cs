// ============================================================================
//  ShaderAsset.cs
// ============================================================================
//  Shader asset that loads .shader files containing vertex and fragment
//  shader source code.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Assets.Loaders;

/// <summary>
/// A shader asset that loads .shader files containing vertex and fragment shader source.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Shader"/> class implements <see cref="IAsset"/> and manages
/// shader source code loading, parsing, and compilation. It supports loading from
/// .shader files that contain both vertex and fragment shader source in a single file.
/// </para>
/// <para>
/// <b>File Format:</b>
/// <code>
/// [vertex]
/// // vertex shader source
/// 
/// [fragment]
/// // fragment shader source
/// </code>
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item><description>Single-file shader loading</description></item>
///   <item><description>Flexible parser - tags can appear in any order</description></item>
///   <item><description>Lazy compilation - shader only compiles when first used</description></item>
///   <item><description>Caches parsed source for fast reloading</description></item>
///   <item><description>Implicit conversion to SFML shader for rendering</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Load a shader through AssetManager
/// var shader = AssetManager.Instance.Load&lt;ShaderAsset&gt;("shaders/myShader.shader");
/// 
/// // Use the shader in rendering
/// batcher.SetShader(shader);
/// batcher.Begin();
/// // ... draw commands ...
/// batcher.End();
/// 
/// // Or access the shader program directly
/// var program = shader.Program;
/// program.SetUniform("uColor", Color.Red);
/// </code>
/// </para>
/// </remarks>
public sealed class Shader : IAsset, IShader
{
    private ShaderProgram _program;
    private string _vertexSource;
    private string _fragmentSource;

    /// <summary>
    /// Gets the unique identifier of the shader asset.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the normalized path or tag used to identify the shader asset.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the raw shader file data bytes.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets a value indicating whether the shader asset is loaded and ready for use.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the asset type of the shader.
    /// </summary>
    public AssetType Type => AssetType.Normal;

    /// <summary>
    /// Gets the last access time of the shader asset for eviction tracking.
    /// </summary>
    public ushort LastAccessTick { get; set; }

    /// <summary>
    /// Gets the underlying shader program.
    /// </summary>
    public ShaderProgram Program => _program;

    /// <summary>
    /// Gets the underlying SFML shader for rendering.
    /// </summary>
    internal SFShader SFShader => _program?.SFShader;

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the asset.</param>
    /// <param name="data">The raw shader file data.</param>
    /// <param name="tag">The path or tag used to identify the asset.</param>
    internal Shader(uint id, byte[] data, string tag)
    {
        Id = id;
        Data = data;
        Tag = tag;
    }

    /// <summary>
    /// Finalizer that ensures resources are cleaned up if <see cref="Dispose"/> wasn't called.
    /// </summary>
    ~Shader() => Dispose();

    /// <summary>
    /// Loads the shader data into memory.
    /// </summary>
    /// <remarks>
    /// This method parses the .shader file and creates a shader program.
    /// The shader is compiled lazily when first used.
    /// </remarks>
    public void Load()
    {
        if (IsValid)
        {
            return;
        }

        if (string.IsNullOrEmpty(_vertexSource) && string.IsNullOrEmpty(_fragmentSource))
            (_vertexSource, _fragmentSource) = ParseShader(Encoding.UTF8.GetString(Data));

        _program = new ShaderProgram(_vertexSource, _fragmentSource);
        IsValid = true;
    }

    /// <summary>
    /// Unloads the shader data from memory.
    /// </summary>
    /// <remarks>
    /// This method disposes the shader program but keeps the parsed source
    /// for fast reloading.
    /// </remarks>
    public void Unload()
    {
        if (!IsValid)
            return;

        _program?.Dispose();
        _program = null;

        IsValid = false;
    }

    /// <summary>
    /// Disposes the shader asset and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (IsValid)
        {
            _program?.Dispose();
            _program = null;

            IsValid = false;
        }

        GC.SuppressFinalize(this);
    }

    #region IShader Implementation

    /// <summary>
    /// Sets a float uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, float value)
        => _program?.SetUniform(name, value);

    /// <summary>
    /// Sets an integer uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, int value)
        => _program?.SetUniform(name, value);

    /// <summary>
    /// Sets a 2D vector uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, Vect2 value)
        => _program?.SetUniform(name, value);

    /// <summary>
    /// Sets a 3D vector uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, Vect3 value)
        => _program?.SetUniform(name, value);

    /// <summary>
    /// Sets a 4D vector uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, Vect4 value)
        => _program?.SetUniform(name, value);

    /// <summary>
    /// Sets a color uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, Color color)
        => _program?.SetUniform(name, color);

    /// <summary>
    /// Sets a texture uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, SFTexture texture)
        => _program?.SetUniform(name, texture);

    /// <summary>
    /// Sets a current texture type uniform on the shader program.
    /// </summary>
    public void SetUniform(string name, SFShader.CurrentTextureType currentTexture)
        => _program?.SetUniform(name, currentTexture);

    /// <summary>
    /// Sets a 4x4 matrix uniform value on the shader program.
    /// </summary>
    public void SetUniform(string name, Matrix4x4 matrix)
        => _program?.SetUniform(name, matrix);

    /// <summary>
    /// Binds the shader program to the graphics pipeline.
    /// </summary>
    public void Bind()
        => _program?.Bind();

    /// <summary>
    /// Unbinds the shader program from the graphics pipeline.
    /// </summary>
    public void Unbind()
        => _program?.Unbind();

    /// <summary>
    /// Gets a value indicating whether the shader program is valid.
    /// </summary>
    bool IShader.IsValid => _program?.IsValid ?? false;

    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Implicitly converts a shader asset to an SFML shader.
    /// </summary>
    /// <param name="asset">The shader asset.</param>
    /// <returns>The underlying SFML shader, or null if the asset is invalid.</returns>
    public static implicit operator SFShader(Shader asset)
    {
        if (asset == null || !asset.IsValid || asset._program == null)
            return null;

        // asset.LastAccessTime = DateTime.Now;
        AssetManager.Instance.Touch(asset);

        return asset._program.SFShader;
    }

    #endregion

    #region Parser

    /// <summary>
    /// Parses a .shader file into vertex and fragment shader source strings.
    /// </summary>
    /// <param name="source">The raw shader file text.</param>
    /// <returns>A tuple containing the vertex and fragment shader source.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the shader file is missing required sections.
    /// </exception>
    private static (string vertex, string fragment) ParseShader(string source)
    {
        if (string.IsNullOrEmpty(source))
            throw new InvalidOperationException("Shader source cannot be null or empty.");

        source = source.Replace("\r\n", "\n").Replace('\r', '\n');

        int vertexStart = source.IndexOf("[vertex]", StringComparison.OrdinalIgnoreCase);
        int fragmentStart = source.IndexOf("[fragment]", StringComparison.OrdinalIgnoreCase);

        if (vertexStart == -1)
            throw new InvalidOperationException("Shader file is missing [vertex] section.");
        if (fragmentStart == -1)
            throw new InvalidOperationException("Shader file is missing [fragment] section.");

        int vertexBegin = vertexStart + "[vertex]".Length;
        int vertexEnd = fragmentStart;

        if (fragmentStart < vertexStart)
        {
            vertexBegin = vertexStart + "[vertex]".Length;
            vertexEnd = source.Length;

            int fragmentBegin = fragmentStart + "[fragment]".Length;
            int fragmentEnd = vertexStart;
            string fragmentSource = source[fragmentBegin..fragmentEnd].Trim();
            string vertexSource = source[vertexBegin..vertexEnd].Trim();

            return (vertexSource, fragmentSource);
        }
        else
        {
            int fragmentBegin = fragmentStart + "[fragment]".Length;
            string vertexSource = source[vertexBegin..vertexEnd].Trim();
            string fragmentSource = source[fragmentBegin..].Trim();

            return (vertexSource, fragmentSource);
        }
    }

    #endregion

    /// <summary>
    /// Returns a string representation of the current shader asset.
    /// </summary>
    public override string ToString()
        => $"ShaderAsset({Id}, {Tag}, {IsValid})";
}