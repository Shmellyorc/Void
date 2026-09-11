namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Internal access point for the renderer currently owned by the engine.
/// Game code and renderer plugins continue to use the public renderer contracts;
/// this class only lets VOID's built-in systems discover the active device.
/// </summary>
internal static class RendererRuntime
{
    private static IRendererBackend _backend;

    internal static IRendererBackend Backend
        => IsAvailable ? _backend : null;

    internal static bool IsAvailable
        => _backend != null && _backend.IsInitialized && _backend.Device != null;

    internal static bool TryGetDevice(out IGraphicsDevice device)
    {
        if (IsAvailable)
        {
            device = _backend.Device;
            return true;
        }

        device = null;
        return false;
    }

    internal static bool TryGetDefault2DShader(out IGraphicsShaderProgram shader)
    {
        if (IsAvailable && _backend.Default2DShader is { IsValid: true } program)
        {
            shader = program;
            return true;
        }

        shader = null;
        return false;
    }

    internal static void Attach(IRendererBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        if (!backend.IsInitialized)
            throw new InvalidOperationException("A renderer must be initialized before it can become the active renderer.");

        if (_backend != null && !ReferenceEquals(_backend, backend))
            throw new InvalidOperationException(
                $"Renderer '{_backend.Name}' is already active. Dispose it before activating '{backend.Name}'.");

        _backend = backend;
    }

    internal static void Detach(IRendererBackend backend)
    {
        if (ReferenceEquals(_backend, backend))
            _backend = null;
    }
}
