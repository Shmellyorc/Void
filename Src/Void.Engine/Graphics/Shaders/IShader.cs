namespace Void.Engine.Graphics.Shaders;

public interface IShader
{
    void SetUniform(string name, float value);
    void SetUniform(string name, int value);
    void SetUniform(string name, Vect2 value);
    void SetUniform(string name, Vect3 value);
    void SetUniform(string name, Vect4 value);
    void SetUniform(string name, Color color);
    void SetUniform(string name, SFTexture texture);
    void SetUniform(string name, Matrix4x4 matrix);
    void SetUniform(string name, SFShader.CurrentTextureType currentTexture);
    void Bind();
    void Unbind();
    bool IsValid { get; }
}