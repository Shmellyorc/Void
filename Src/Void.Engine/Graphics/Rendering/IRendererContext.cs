namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Window/platform services exposed to renderer implementations.
/// Normal game code should use VOID's Window API instead.
/// </summary>
public interface IRendererContext
{
    GameSettings Settings { get; }
    Vect2 WindowSize { get; }
    Vect2 RenderSize { get; }

    /// <summary>
    /// Opaque handle owned by VOID's window system.
    /// </summary>
    nint WindowSystemHandle { get; }

    /// <summary>
    /// Resolves a graphics API function pointer when the active platform supports it.
    /// The built-in OpenGL renderer uses this with Silk.NET.OpenGL.
    /// </summary>
    nint GetProcAddress(string name);

    /// <summary>
    /// Swaps the platform window buffers for APIs which present this way, such as OpenGL.
    /// </summary>
    void SwapBuffers();

    /// <summary>
    /// Attempts to change the platform swap interval. OpenGL uses 0 for off and 1 for VSync.
    /// </summary>
    bool TrySetSwapInterval(int interval);

    /// <summary>
    /// Attempts to retrieve a platform-native handle for custom renderers.
    /// </summary>
    bool TryGetNativeHandle(NativeWindowHandleKind kind, out nint handle);
}
