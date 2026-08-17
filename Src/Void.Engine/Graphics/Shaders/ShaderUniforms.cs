namespace Void.Engine.Graphics.Shaders;

public static class ShaderUniforms
{
    public static void SetTransform(IShader shader, Matrix4x4 matrix, string name = "uTransform")
        => shader?.SetUniform(name, matrix);

    public static void SetProjection(IShader shader, Matrix4x4 matrix, string name = "uProjection")
        => shader?.SetUniform(name, matrix);

    public static void SetView(IShader shader, Matrix4x4 matrix, string name = "uView")
        => shader?.SetUniform(name, matrix);
}