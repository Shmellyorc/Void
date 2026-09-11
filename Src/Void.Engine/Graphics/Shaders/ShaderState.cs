// ============================================================================
//  ShaderState.cs
// ============================================================================
//  Renderer-neutral explicit shader binding state.
// ============================================================================

namespace Void.Engine.Graphics.Shaders;

internal static class ShaderState
{
    private static ShaderProgram _currentProgram;

    internal static ShaderProgram GetCurrent() => _currentProgram;

    internal static void Bind(ShaderProgram program)
    {
        if (ReferenceEquals(_currentProgram, program))
            return;

        _currentProgram = program;
    }

    internal static void Unbind(ShaderProgram program = null)
    {
        if (program != null && !ReferenceEquals(_currentProgram, program))
            return;

        _currentProgram = null;
    }
}
