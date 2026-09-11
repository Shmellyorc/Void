// ============================================================================
//  ShaderProgram.cs
// ============================================================================
//  Renderer-neutral shader program wrapper. Sources and uniform values belong
//  to VOID; the active renderer owns the actual GPU program implementation.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Stores renderer-neutral shader sources and uniforms, creating the backend
/// <see cref="IGraphicsShaderProgram"/> lazily on the active graphics device.
/// </summary>
public sealed class ShaderProgram : IDisposable
{
    private readonly ShaderSource[] _sources;

    private readonly Dictionary<string, float> _floatUniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _intUniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vect2> _vect2Uniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vect3> _vect3Uniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vect4> _vect4Uniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Color> _colorUniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Matrix4x4> _matrixUniforms = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Texture> _textureUniforms = new(StringComparer.Ordinal);
    private readonly HashSet<string> _currentTextureUniforms = new(StringComparer.Ordinal);

    private IGraphicsDevice _graphicsDevice;
    private IGraphicsShaderProgram _graphicsProgram;
    private bool _disposed;

    /// <summary>Gets the shader sources used to create backend programs.</summary>
    public ReadOnlyMemory<ShaderSource> Sources => _sources;

    /// <summary>
    /// Gets whether this program has valid sources and is not disposed. When a
    /// renderer is active this also verifies that the backend program can be created.
    /// </summary>
    public bool IsValid
        => !_disposed && _sources.Length > 0 && (_graphicsProgram?.IsValid ?? true);

    /// <summary>
    /// Creates a text shader program with vertex and fragment stages.
    /// Existing .shader assets default to GLSL; custom shader types can choose
    /// another language for renderer plugins that support it.
    /// </summary>
    public ShaderProgram(
        string vertexSource,
        string fragmentSource,
        ShaderLanguage language = ShaderLanguage.Glsl)
    {
        if (string.IsNullOrWhiteSpace(vertexSource))
            throw new ArgumentException("Vertex shader source cannot be empty.", nameof(vertexSource));
        if (string.IsNullOrWhiteSpace(fragmentSource))
            throw new ArgumentException("Fragment shader source cannot be empty.", nameof(fragmentSource));

        _sources =
        [
            new ShaderSource(ShaderStage.Vertex, language, Encoding.UTF8.GetBytes(vertexSource)),
            new ShaderSource(ShaderStage.Fragment, language, Encoding.UTF8.GetBytes(fragmentSource))
        ];
    }

    /// <summary>
    /// Creates a program from arbitrary renderer-neutral shader stages. This is
    /// the path custom shader implementations can use for GLSL, HLSL, SPIR-V,
    /// DXIL, MSL, or another backend-supported language.
    /// </summary>
    public ShaderProgram(ReadOnlyMemory<ShaderSource> sources)
    {
        if (sources.IsEmpty)
            throw new ArgumentException("At least one shader source is required.", nameof(sources));

        _sources = sources.ToArray();
    }

    public void SetUniform(string name, float value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _floatUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, int value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _intUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Vect2 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect2Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Vect3 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect3Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Vect4 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect4Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Color value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _colorUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Matrix4x4 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _matrixUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    public void SetUniform(string name, Texture texture)
    {
        PrepareUniformName(name);
        ClearUniform(name);

        if (texture == null)
            return;

        _textureUniforms[name] = texture;
        ApplyTextureUniform(name, texture);
    }

    /// <summary>Uses the current draw command texture for a named sampler.</summary>
    public void SetCurrentTexture(string name)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _currentTextureUniforms.Add(name);
    }

    /// <summary>
    /// Explicitly binds this renderer-neutral program for code that uses the
    /// legacy Bind/Unbind style. Batch-level SetShader still takes precedence.
    /// </summary>
    public void Bind()
    {
        ThrowIfDisposed();

        if (RendererRuntime.IsAvailable)
            EnsureGraphicsProgram();

        ShaderState.Bind(this);
    }

    public void Unbind()
    {
        if (_disposed)
            return;

        ShaderState.Unbind(this);
    }

    internal bool TryGetGraphicsProgram(out IGraphicsShaderProgram program)
    {
        ThrowIfDisposed();

        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice activeDevice))
        {
            program = null;
            return false;
        }

        if (_graphicsProgram != null && !ReferenceEquals(activeDevice, _graphicsDevice))
            ReleaseGraphicsProgram();

        if (_graphicsProgram == null)
        {
            _graphicsDevice = activeDevice;
            _graphicsProgram = activeDevice.CreateShaderProgram(
                new ShaderProgramDescription(_sources));

            ApplyCachedUniforms(_graphicsProgram);

            Logger.Instance.DebugWithCategory(
                "ShaderProgram",
                "Shader program created for active {0} renderer with {1} stage(s).",
                RendererRuntime.Backend?.Api.ToString() ?? "unknown",
                _sources.Length);
        }

        program = _graphicsProgram;
        return program?.IsValid == true;
    }

    /// <summary>
    /// Applies per-draw VOID state and returns the backend-owned program.
    /// The conventional uniforms are optional; backends ignore missing names.
    /// </summary>
    internal bool TryPrepareForDraw(
        IGraphicsTexture drawTexture,
        Matrix4x4 viewProjection,
        out IGraphicsShaderProgram program)
    {
        if (!TryGetGraphicsProgram(out program))
            return false;

        program.SetUniform("uViewProjection", viewProjection);
        program.SetUniform("uUseTexture", drawTexture != null ? 1 : 0);

        // Texture uniforms can point at resources recreated after an unload or
        // device switch, so resolve them at draw time rather than only at set time.
        foreach ((string name, Texture texture) in _textureUniforms)
            ApplyTextureUniform(program, name, texture);

        if (drawTexture != null)
        {
            program.SetUniform(
                "uTextureSize",
                new Vect2(drawTexture.Description.Width, drawTexture.Description.Height));

            // Existing SpriteBatcher shader behavior automatically supplied the
            // current sprite texture as uTexture. Preserve that convention.
            program.SetTexture("uTexture", drawTexture);

            foreach (string name in _currentTextureUniforms)
                program.SetTexture(name, drawTexture);
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ShaderState.Unbind(this);
        ReleaseGraphicsProgram();

        _floatUniforms.Clear();
        _intUniforms.Clear();
        _vect2Uniforms.Clear();
        _vect3Uniforms.Clear();
        _vect4Uniforms.Clear();
        _colorUniforms.Clear();
        _matrixUniforms.Clear();
        _textureUniforms.Clear();
        _currentTextureUniforms.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IGraphicsShaderProgram EnsureGraphicsProgram()
    {
        if (!TryGetGraphicsProgram(out IGraphicsShaderProgram program))
            throw new InvalidOperationException("No active graphics device is available for this shader program.");

        return program;
    }

    private void ApplyCachedUniforms(IGraphicsShaderProgram program)
    {
        foreach ((string name, float value) in _floatUniforms)
            program.SetUniform(name, value);
        foreach ((string name, int value) in _intUniforms)
            program.SetUniform(name, value);
        foreach ((string name, Vect2 value) in _vect2Uniforms)
            program.SetUniform(name, value);
        foreach ((string name, Vect3 value) in _vect3Uniforms)
            program.SetUniform(name, value);
        foreach ((string name, Vect4 value) in _vect4Uniforms)
            program.SetUniform(name, value);
        foreach ((string name, Color value) in _colorUniforms)
            program.SetUniform(name, value);
        foreach ((string name, Matrix4x4 value) in _matrixUniforms)
            program.SetUniform(name, value);
        foreach ((string name, Texture texture) in _textureUniforms)
            ApplyTextureUniform(program, name, texture);
    }

    private void ApplyTextureUniform(string name, Texture texture)
    {
        if (_graphicsProgram?.IsValid == true)
            ApplyTextureUniform(_graphicsProgram, name, texture);
    }

    private static void ApplyTextureUniform(
        IGraphicsShaderProgram program,
        string name,
        Texture texture)
    {
        if (texture != null && texture.TryGetGraphicsTexture(out IGraphicsTexture graphicsTexture))
            program.SetTexture(name, graphicsTexture);
    }

    private void ClearUniform(string name)
    {
        _floatUniforms.Remove(name);
        _intUniforms.Remove(name);
        _vect2Uniforms.Remove(name);
        _vect3Uniforms.Remove(name);
        _vect4Uniforms.Remove(name);
        _colorUniforms.Remove(name);
        _matrixUniforms.Remove(name);
        _textureUniforms.Remove(name);
        _currentTextureUniforms.Remove(name);
    }

    private void PrepareUniformName(string name)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }

    private void ReleaseGraphicsProgram()
    {
        _graphicsProgram?.Dispose();
        _graphicsProgram = null;
        _graphicsDevice = null;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
