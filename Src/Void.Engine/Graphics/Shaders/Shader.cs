namespace Void.Engine.Graphics.Shaders;

internal sealed class Shader : IShader, IDisposable
{
    private readonly SFShader _shader;
    private bool _disposed;

    internal SFShader SFShader => _shader;

    internal Shader(string vertexPath, string fragmentPath)
    {
        _shader = new SFShader(vertexPath, null, fragmentPath);
    }

    internal Shader(string vertexSource, string fragmentSource, bool fromMemory)
    {
        _shader = SFShader.FromString(vertexSource, null, fragmentSource);
    }

    public bool IsValid => _shader != null && !_shader.IsInvalid;

    public void SetUniform(string name, float value)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, value);
    }

    public void SetUniform(string name, int value)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, value);
    }

    public void SetUniform(string name, Vect2 value)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, new SFVec2(value.X, value.Y));
    }

    public void SetUniform(string name, Vect3 value)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, new SFVec3(value.X, value.Y, value.Z));
    }

    public void SetUniform(string name, Vect4 value)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, new SFVec4(value.X, value.Y, value.Z, value.W));
    }

    public void SetUniform(string name, Color color)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, color);
    }

    public void SetUniform(string name, SFTexture texture)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, texture);
    }

    public void SetUniform(string name, SFShader.CurrentTextureType currentTexture)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, currentTexture);
    }

    public void SetUniform(string name, Matrix4x4 matrix)
    {
        if (_disposed || !IsValid) return;
        _shader.SetUniform(name, new SFMat4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        ));
    }

    public void Bind()
    {
        if (_disposed || !IsValid) return;
        ShaderState.Bind(_shader);
    }

    public void Unbind()
    {
        if (_disposed) return;
        ShaderState.Unbind();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _shader?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}