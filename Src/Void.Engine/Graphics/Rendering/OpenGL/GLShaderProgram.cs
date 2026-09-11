using System.Numerics;
using System.Text;
using Silk.NET.OpenGL;

namespace Void.Engine.Graphics.Rendering.OpenGL;

/// <summary>
/// OpenGL implementation of a renderer-owned shader program.
/// The built-in OpenGL backend consumes GLSL source text; other renderer
/// plugins remain free to consume their own ShaderLanguage formats.
/// </summary>
internal sealed class GLShaderProgram : IGraphicsShaderProgram
{
    private const int MaxTrackedTextureUnits = 32;

    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniformLocations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _textureUnits = new(StringComparer.Ordinal);
    private uint _handle;
    private bool _disposed;

    public bool IsValid => !_disposed && _handle != 0;

    internal uint Handle => _handle;

    internal GLShaderProgram(GL gl, in ShaderProgramDescription description)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        _handle = _gl.CreateProgram();
        if (_handle == 0)
            throw new InvalidOperationException("OpenGL failed to create a shader program object.");

        var shaderHandles = new List<uint>(description.Sources.Length);
        var stages = new HashSet<ShaderStage>();

        try
        {
            foreach (ShaderSource source in description.Sources.Span)
            {
                if (!stages.Add(source.Stage))
                    throw new ArgumentException($"Shader program contains more than one {source.Stage} stage.", nameof(description));

                uint shader = Compile(source);
                shaderHandles.Add(shader);
                _gl.AttachShader(_handle, shader);
            }

            _gl.LinkProgram(_handle);
            _gl.GetProgram(_handle, GLEnum.LinkStatus, out int status);

            if (status != (int)GLEnum.True)
            {
                string infoLog = _gl.GetProgramInfoLog(_handle);
                throw new InvalidOperationException(
                    $"OpenGL shader program failed to link.{Environment.NewLine}{infoLog}");
            }
        }
        catch
        {
            foreach (uint shader in shaderHandles)
            {
                if (shader != 0)
                {
                    _gl.DetachShader(_handle, shader);
                    _gl.DeleteShader(shader);
                }
            }

            Dispose();
            throw;
        }

        foreach (uint shader in shaderHandles)
        {
            _gl.DetachShader(_handle, shader);
            _gl.DeleteShader(shader);
        }
    }

    internal void Use()
    {
        ThrowIfDisposed();
        _gl.UseProgram(_handle);
    }

    public void SetUniform(string name, float value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, int value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vect2 value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.Uniform2(location, value.X, value.Y);
    }

    public void SetUniform(string name, Vect3 value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void SetUniform(string name, Vect4 value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    public void SetUniform(string name, Color value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        const float InvByte = 1f / 255f;
        Use();
        _gl.Uniform4(
            location,
            value.R * InvByte,
            value.G * InvByte,
            value.B * InvByte,
            value.A * InvByte);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        Use();
        _gl.UniformMatrix4(location, 1, false, (float*)&value);
    }

    public void SetTexture(string name, IGraphicsTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (texture is not GLTexture glTexture)
            throw new ArgumentException("The texture was not created by the VOID OpenGL backend.", nameof(texture));

        int location = GetUniformLocation(name);
        if (location < 0)
            return;

        if (!_textureUnits.TryGetValue(name, out int unit))
        {
            unit = _textureUnits.Count;
            if (unit >= MaxTrackedTextureUnits)
                throw new InvalidOperationException($"A shader program cannot bind more than {MaxTrackedTextureUnits} tracked textures.");

            _textureUnits.Add(name, unit);
        }

        Use();
        glTexture.Bind(unit);
        _gl.Uniform1(location, unit);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_handle != 0)
        {
            _gl.DeleteProgram(_handle);
            _handle = 0;
        }

        _uniformLocations.Clear();
        _textureUnits.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private uint Compile(in ShaderSource source)
    {
        if (source.Language != ShaderLanguage.Glsl)
        {
            throw new NotSupportedException(
                $"The built-in OpenGL renderer consumes GLSL shader source, not {source.Language}. " +
                "Other renderer plugins may support different shader languages.");
        }

        if (!string.Equals(source.EntryPoint, "main", StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                "GLSL programs used by the built-in OpenGL renderer currently require the standard 'main' entry point.");
        }

        string text = Encoding.UTF8.GetString(source.Data.Span);
        ShaderType type = ToShaderType(source.Stage);

        uint shader = _gl.CreateShader(type);
        if (shader == 0)
            throw new InvalidOperationException($"OpenGL failed to create a {source.Stage} shader object.");

        _gl.ShaderSource(shader, text);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, GLEnum.CompileStatus, out int status);

        if (status == (int)GLEnum.True)
            return shader;

        string infoLog = _gl.GetShaderInfoLog(shader);
        _gl.DeleteShader(shader);

        throw new InvalidOperationException(
            $"OpenGL {source.Stage} shader failed to compile.{Environment.NewLine}{infoLog}");
    }

    private int GetUniformLocation(string name)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_uniformLocations.TryGetValue(name, out int location))
            return location;

        location = _gl.GetUniformLocation(_handle, name);
        _uniformLocations.Add(name, location);
        return location;
    }

    private static ShaderType ToShaderType(ShaderStage stage)
        => stage switch
        {
            ShaderStage.Vertex => ShaderType.VertexShader,
            ShaderStage.Fragment => ShaderType.FragmentShader,
            ShaderStage.Geometry => ShaderType.GeometryShader,
            ShaderStage.Compute => ShaderType.ComputeShader,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported OpenGL shader stage.")
        };

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
