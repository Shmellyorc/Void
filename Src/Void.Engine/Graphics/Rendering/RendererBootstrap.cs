using Void.Engine.Graphics.Rendering.OpenGL;
using Void.Engine.Platform.SDL;

namespace Void.Engine.Graphics.Rendering;

/// <summary>Internal construction path for the selected renderer and its SDL window.</summary>
internal static class RendererBootstrap
{
    public static IRendererBackend CreateBackend(GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.RendererFactory?.Invoke() ?? new OpenGLRenderer(settings.OpenGLVersion);
    }

    public static SdlWindowHost CreateWindow(GameSettings settings, IRendererBackend renderer)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return CreateWindow(
            renderer,
            (int)settings.Window.X,
            (int)settings.Window.Y,
            settings.AppTitle,
            settings.VSync,
            settings.Fullscreen ? WindowMode.Fullscreen : WindowMode.Windowed,
            settings.DisplayIndex,
            settings.FullscreenStyle,
            settings.FullscreenWidth,
            settings.FullscreenHeight,
            settings.FullscreenRefreshRate);
    }

    public static SdlWindowHost CreateWindow(
        IRendererBackend renderer,
        int width,
        int height,
        string title,
        bool vsync,
        WindowMode mode,
        int displayIndex = 0,
        FullscreenStyle fullscreenStyle = FullscreenStyle.Desktop,
        int fullscreenWidth = 0,
        int fullscreenHeight = 0,
        float fullscreenRefreshRate = 0f)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        return new SdlWindowHost(
            width,
            height,
            title,
            renderer.RequiredWindowFlags,
            renderer.Version,
            vsync,
            mode,
            displayIndex,
            fullscreenStyle,
            fullscreenWidth,
            fullscreenHeight,
            fullscreenRefreshRate);
    }

    public static RendererContext CreateContext(GameSettings settings, SdlWindowHost window)
        => new(settings, window);
}
