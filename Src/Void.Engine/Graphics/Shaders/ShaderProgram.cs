// ============================================================================
//  ShaderProgram.cs
// ============================================================================
//  Renderer-neutral shader program wrapper. Sources and uniform values belong
//  to VOID; the active renderer owns the actual GPU program implementation.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.Shaders;

/// <summary>
/// Stores renderer-neutral shader sources and uniform values and lazily creates the
/// backend-owned <see cref="IGraphicsShaderProgram"/> on the active graphics device.
/// </summary>
/// <remarks>
/// <para>
/// Shader sources are retained independently of any renderer. The backend program is created
/// only when an active graphics device is available and the program is needed for drawing or
/// explicit binding. If the active graphics device changes, the previous backend program is
/// released and recreated on the new device.
/// </para>
/// <para>
/// Uniform values are cached by name and replayed when a backend program is created. Replacing
/// or clearing a texture uniform also clears any stale backend texture assignment for that name.
/// During VOID's 2D draw path, custom programs receive the conventional <c>uViewProjection</c>,
/// <c>uUseTexture</c>, <c>uTextureSize</c>, and <c>uTexture</c> values when applicable.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Gets the renderer-neutral shader sources used to create backend programs.
    /// </summary>
    public ReadOnlyMemory<ShaderSource> Sources => _sources;

    /// <summary>
    /// Gets whether this program has at least one source, has not been disposed, and has no
    /// known invalid backend program.
    /// </summary>
    /// <remarks>
    /// Before a backend program has been created, this property validates renderer-neutral state
    /// only. Shader compilation and backend-specific validation occur lazily when the active
    /// renderer creates the underlying program.
    /// </remarks>
    public bool IsValid
        => !_disposed && _sources.Length > 0 && (_graphicsProgram?.IsValid ?? true);

    /// <summary>
    /// Creates a renderer-neutral program containing vertex and fragment shader stages.
    /// </summary>
    /// <param name="vertexSource">The non-empty vertex shader source text.</param>
    /// <param name="fragmentSource">The non-empty fragment shader source text.</param>
    /// <param name="language">The shader language understood by the target renderer backend.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="vertexSource"/> or <paramref name="fragmentSource"/> is empty or whitespace.
    /// </exception>
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
    /// Creates a renderer-neutral program from an arbitrary set of shader stages.
    /// </summary>
    /// <param name="sources">The non-empty shader sources to retain for backend creation.</param>
    /// <remarks>
    /// The active renderer may reject unsupported languages, stages, or stage combinations when
    /// the backend program is created.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sources"/> is empty.</exception>
    public ShaderProgram(ReadOnlyMemory<ShaderSource> sources)
    {
        if (sources.IsEmpty)
            throw new ArgumentException("At least one shader source is required.", nameof(sources));

        _sources = sources.ToArray();
    }

    /// <summary>Sets and caches a floating-point uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, float value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _floatUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches an integer uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, int value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _intUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches a two-component vector uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Vect2 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect2Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches a three-component vector uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Vect3 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect3Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches a four-component vector uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Vect4 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _vect4Uniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches a color uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The color to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Color value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _colorUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>Sets and caches a 4x4 matrix uniform value.</summary>
    /// <param name="name">The non-empty shader uniform name.</param>
    /// <param name="value">The matrix to assign.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Matrix4x4 value)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _matrixUniforms[name] = value;
        if (_graphicsProgram?.IsValid == true)
            _graphicsProgram.SetUniform(name, value);
    }

    /// <summary>
    /// Sets and caches a game-facing texture for a named sampler uniform.
    /// </summary>
    /// <param name="name">The non-empty sampler uniform name.</param>
    /// <param name="texture">The texture to assign, or <see langword="null"/> to clear the existing texture assignment.</param>
    /// <remarks>
    /// Texture resources are resolved against the active graphics device at draw time so an
    /// unloaded, reloaded, or device-recreated texture can supply its current backend resource.
    /// Passing <see langword="null"/> removes the cached value and clears any existing backend texture binding for
    /// the same sampler name. If a retained texture cannot currently resolve a backend resource,
    /// the stale backend binding is cleared until that texture becomes available again.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetUniform(string name, Texture texture)
    {
        PrepareUniformName(name);

        if (texture == null)
        {
            ClearUniform(name);
            return;
        }

        // A replacement texture is applied immediately, so avoid an unnecessary
        // unbind/rebind cycle while still clearing every other cached uniform type.
        ClearUniform(name, clearBackendTexture: false);
        _textureUniforms[name] = texture;
        ApplyTextureUniform(name, texture);
    }

    /// <summary>
    /// Marks a sampler uniform as using the texture supplied by the current draw operation.
    /// </summary>
    /// <param name="name">The non-empty sampler uniform name.</param>
    /// <remarks>
    /// Setting this mode replaces any cached value of another supported uniform type with the same name.
    /// VOID already supplies the conventional <c>uTexture</c> sampler automatically. If a draw has
    /// no texture, VOID clears the backend assignment instead of reusing the previous draw texture.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void SetCurrentTexture(string name)
    {
        PrepareUniformName(name);
        ClearUniform(name);
        _currentTextureUniforms.Add(name);
    }

    /// <summary>
    /// Makes this program the explicitly bound renderer-neutral shader program.
    /// </summary>
    /// <remarks>
    /// If a renderer is active, the backend program is created before binding. Batch-level shader
    /// selection still takes precedence over explicitly bound shader state.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a renderer is reported as available but no usable graphics device or backend program can be obtained.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this program has been disposed.</exception>
    public void Bind()
    {
        ThrowIfDisposed();

        if (RendererRuntime.IsAvailable)
            EnsureGraphicsProgram();

        ShaderState.Bind(this);
    }

    /// <summary>
    /// Removes this program from explicitly bound renderer-neutral shader state.
    /// </summary>
    /// <remarks>Calling this method after disposal is a harmless no-op.</remarks>
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

    // Applies VOID's conventional per-draw 2D state before returning the backend program.
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
        else
        {
            // Prevent a texture from the previous draw from remaining visible to
            // the conventional or current-draw samplers on an untextured draw.
            program.SetTexture("uTexture", null);

            foreach (string name in _currentTextureUniforms)
                program.SetTexture(name, null);
        }

        return true;
    }

    /// <summary>
    /// Releases the backend shader program and all cached uniform state owned by this instance.
    /// </summary>
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
        else
            program.SetTexture(name, null);
    }

    private void ClearUniform(string name, bool clearBackendTexture = true)
    {
        _floatUniforms.Remove(name);
        _intUniforms.Remove(name);
        _vect2Uniforms.Remove(name);
        _vect3Uniforms.Remove(name);
        _vect4Uniforms.Remove(name);
        _colorUniforms.Remove(name);
        _matrixUniforms.Remove(name);

        bool hadTextureUniform = _textureUniforms.Remove(name);
        bool hadCurrentTextureUniform = _currentTextureUniforms.Remove(name);

        if (clearBackendTexture &&
            (hadTextureUniform || hadCurrentTextureUniform) &&
            _graphicsProgram?.IsValid == true)
        {
            _graphicsProgram.SetTexture(name, null);
        }
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
