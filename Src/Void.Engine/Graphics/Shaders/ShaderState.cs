namespace Void.Engine.Graphics.Shaders;

public static class ShaderState
{
    private static SFShader _currentShader;

    public static void Bind(SFShader shader)
    {
        if (_currentShader == shader) return;

        SFShader.Bind(shader);
        _currentShader = shader;
    }

    public static SFShader GetCurrent() => _currentShader;

    public static void Unbind()
    {
        if (_currentShader == null) return;

        SFShader.Bind(null);
        _currentShader = null;
    }
}