// ============================================================================
//  IRendererContext.cs
// ============================================================================
//  Window and platform services exposed to renderer implementations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Exposes VOID-owned window and platform services to a renderer backend.
/// </summary>
/// <remarks>
/// <para>
/// Normal game code should use VOID's public window APIs instead. This interface exists
/// for renderer plugins that need to create an API context, surface, or swapchain for
/// the native window VOID owns.
/// </para>
/// <para>
/// Handles returned by this context are borrowed. A renderer must not destroy, free,
/// close, or otherwise assume ownership of the window or native handles supplied by VOID.
/// </para>
/// </remarks>
public interface IRendererContext
{
    /// <summary>Gets the game settings used to create the renderer and window.</summary>
    GameSettings Settings { get; }

    /// <summary>Gets the current native window size in pixels.</summary>
    Vect2 WindowSize { get; }

    /// <summary>
    /// Gets VOID's internal game render size in pixels, including configured supersampling.
    /// </summary>
    Vect2 RenderSize { get; }

    /// <summary>
    /// Gets the native window-system backend that VOID initialized.
    /// </summary>
    /// <remarks>
    /// Renderer plugins can use this value to interpret handles returned by
    /// <see cref="TryGetNativeHandle"/>.
    /// </remarks>
    NativeWindowBackend PlatformBackend { get; }

    /// <summary>
    /// Gets VOID's opaque platform-window handle.
    /// </summary>
    /// <remarks>
    /// This is the SDL window pointer owned by VOID, not an HWND, X11 Window,
    /// wl_surface, NSWindow, or another operating-system-native handle. Renderers
    /// that need a platform-native handle should normally use <see cref="TryGetNativeHandle"/>.
    /// </remarks>
    nint WindowSystemHandle { get; }

    /// <summary>
    /// Resolves a graphics API function pointer when the active platform supports it.
    /// </summary>
    /// <param name="name">The non-empty function name.</param>
    /// <returns>The function pointer, or zero when it is unavailable.</returns>
    /// <remarks>
    /// VOID's built-in OpenGL renderer uses this to load OpenGL through Silk.NET.
    /// Other APIs may not require this service.
    /// </remarks>
    nint GetProcAddress(string name);

    /// <summary>
    /// Swaps native window buffers for presentation models that use a window buffer swap.
    /// </summary>
    /// <remarks>
    /// The built-in OpenGL renderer presents this way. Swapchain-based renderers may use
    /// their own presentation path instead.
    /// </remarks>
    void SwapBuffers();

    /// <summary>Attempts to change the platform swap interval.</summary>
    /// <param name="interval">
    /// The platform-specific interval. OpenGL commonly uses zero for immediate presentation
    /// and one for vertical synchronization.
    /// </param>
    /// <returns><see langword="true"/> when the platform accepted the requested interval.</returns>
    bool TrySetSwapInterval(int interval);

    /// <summary>Attempts to retrieve a borrowed platform-native handle.</summary>
    /// <param name="kind">The native handle to request.</param>
    /// <param name="handle">
    /// When successful, receives the borrowed handle. When unsupported, receives zero.
    /// </param>
    /// <returns><see langword="true"/> when the requested handle is available.</returns>
    /// <remarks>
    /// Returned handles remain owned by VOID and the underlying window system. They are
    /// valid only while the renderer context and window remain alive. Use
    /// <see cref="PlatformBackend"/> to determine the concrete native handle type.
    /// </remarks>
    bool TryGetNativeHandle(NativeWindowHandleKind kind, out nint handle);
}
