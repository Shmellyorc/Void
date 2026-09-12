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
    /// Gets the native window-system backend SDL actually initialized.
    /// Renderer plugins should use this to interpret handles returned by
    /// <see cref="TryGetNativeHandle"/>.
    /// </summary>
    NativeWindowBackend PlatformBackend { get; }

    /// <summary>
    /// Opaque VOID platform-window handle.
    /// </summary>
    /// <remarks>
    /// This is the SDL window pointer owned by VOID, not an HWND, X11 Window,
    /// wl_surface, NSWindow, or other native operating-system handle.
    /// Custom renderers should normally use <see cref="TryGetNativeHandle"/>.
    /// </remarks>
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
    /// Attempts to retrieve a borrowed platform-native handle for custom renderers.
    /// </summary>
    /// <remarks>
    /// Returned handles remain owned by VOID/SDL and must never be freed or destroyed
    /// by the renderer plugin. They are valid only while the renderer context/window
    /// remains alive. Unsupported handle kinds return <c>false</c> and zero.
    ///
    /// Use <see cref="PlatformBackend"/> to determine the concrete native type.
    /// </remarks>
    bool TryGetNativeHandle(NativeWindowHandleKind kind, out nint handle);
}
