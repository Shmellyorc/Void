using System;

namespace Void.Engine.Graphics.Shaders;

public static class ShaderProgram
{
    public static IShader Load(string vertexPath, string fragmentPath)
    {
        if (string.IsNullOrEmpty(vertexPath))
            throw new ArgumentNullException(nameof(vertexPath));
        if (string.IsNullOrEmpty(fragmentPath))
            throw new ArgumentNullException(nameof(fragmentPath));

        return new Shader(vertexPath, fragmentPath);
    }

    public static IShader LoadFromMemory(string vertexSource, string fragmentSource)
    {
        if (string.IsNullOrEmpty(vertexSource))
            throw new ArgumentNullException(nameof(vertexSource));
        if (string.IsNullOrEmpty(fragmentSource))
            throw new ArgumentNullException(nameof(fragmentSource));

        return new Shader(vertexSource, fragmentSource, true);
    }
}