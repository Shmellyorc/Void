using Void.Engine.Platform.SDL;

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// VOID-owned renderer context backed by the active platform window.
/// SDL remains internal and is never exposed through this public contract.
/// </summary>
internal sealed class RendererContext : IRendererContext
{
    private readonly SdlWindowHost _window;

    public GameSettings Settings { get; }
    public Vect2 WindowSize => _window.Size;
    public Vect2 RenderSize { get; }
    public nint WindowSystemHandle => _window.Handle;
    public NativeWindowBackend PlatformBackend => _window.Capabilities.Backend;

    internal RendererContext(GameSettings settings, SdlWindowHost window)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _window = window ?? throw new ArgumentNullException(nameof(window));
        RenderSize = settings.Viewport * settings.SuperSample;
    }

    public nint GetProcAddress(string name) => _window.GetProcAddress(name);
    public void SwapBuffers() => _window.SwapBuffers();
    public bool TrySetSwapInterval(int interval) => _window.TrySetSwapInterval(interval);

    public bool TryGetNativeHandle(NativeWindowHandleKind kind, out nint handle)
        => _window.TryGetNativeHandle(kind, out handle);
}
