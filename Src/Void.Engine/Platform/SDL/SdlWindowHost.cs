// ============================================================================
//  SdlWindowHost.cs
// ============================================================================
//  SDL native window, event handling, display state, and graphics context hosting.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;
using Void.Engine.Systems;

namespace Void.Engine.Platform.SDL;

/// <summary>
/// Internal SDL-backed native window. It owns only platform/window/context concerns;
/// rendering remains owned by the selected renderer backend.
/// </summary>
internal sealed class SdlWindowHost : IDisposable
{
    private IntPtr _window;
    private IntPtr _glContext;
    private bool _disposed;
    private WindowMode _mode;
    private FullscreenStyle _fullscreenStyle;
    private int _fullscreenWidth;
    private int _fullscreenHeight;
    private float _fullscreenRefreshRate;

    public IntPtr Handle => _window;
    public bool IsOpen { get; private set; } = true;
    public Vect2 Size { get; private set; }
    public DisplayId Display => SdlPlatform.GetDisplayForWindow(_window);
    public WindowCapabilities Capabilities => SdlPlatform.GetWindowCapabilities();

    public event Action<int, int> Resized;
    public event Action FocusGained;
    public event Action FocusLost;
    public event Action CloseRequested;
    public event Action<int> MouseWheelScrolled;
    public event Action<DisplayChangedEvent> DisplayChanged;

    public SdlWindowHost(
        int width,
        int height,
        string title,
        RendererWindowFlags rendererFlags,
        GraphicsVersion graphicsVersion,
        bool vsync,
        WindowMode mode = WindowMode.Windowed,
        int displayIndex = 0,
        FullscreenStyle fullscreenStyle = FullscreenStyle.Desktop,
        int fullscreenWidth = 0,
        int fullscreenHeight = 0,
        float fullscreenRefreshRate = 0f)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        SdlPlatform.Acquire();

        try
        {
            // Fullscreen is deliberately entered only after the window has been
            // placed on the requested display. This makes initial monitor choice
            // deterministic instead of implicitly targeting the primary display.
            SDL3.SDL.WindowFlags flags = GetInitialWindowFlags(mode);

            if ((rendererFlags & RendererWindowFlags.OpenGL) != 0)
            {
                ConfigureOpenGL(graphicsVersion);
                flags |= SDL3.SDL.WindowFlags.OpenGL;
            }

            if ((rendererFlags & RendererWindowFlags.Vulkan) != 0)
                flags |= SDL3.SDL.WindowFlags.Vulkan;

            _window = SDL3.SDL.CreateWindow(title ?? string.Empty, width, height, flags);
            if (_window == IntPtr.Zero)
                throw new InvalidOperationException($"SDL window creation failed: {SDL3.SDL.GetError()}");

            Size = new Vect2(width, height);
            _mode = WindowMode.Windowed;
            _fullscreenStyle = fullscreenStyle;
            _fullscreenWidth = fullscreenWidth;
            _fullscreenHeight = fullscreenHeight;
            _fullscreenRefreshRate = fullscreenRefreshRate;

            // Resolve the requested monitor only after this host owns the
            // persistent SDL session.
            DisplayId target = SdlPlatform.GetDisplayId(displayIndex);

            if ((rendererFlags & RendererWindowFlags.OpenGL) != 0)
            {
                _glContext = SDL3.SDL.GLCreateContext(_window);
                if (_glContext == IntPtr.Zero)
                    throw new InvalidOperationException($"SDL OpenGL context creation failed: {SDL3.SDL.GetError()}");

                if (!SDL3.SDL.GLMakeCurrent(_window, _glContext))
                    throw new InvalidOperationException($"SDL could not make the OpenGL context current: {SDL3.SDL.GetError()}");

                if (!SDL3.SDL.GLSetSwapInterval(vsync ? 1 : 0))
                    throw new InvalidOperationException($"SDL could not set the OpenGL swap interval: {SDL3.SDL.GetError()}");
            }

            ApplyWindowStateCore(
                mode,
                target,
                fullscreenStyle,
                fullscreenWidth,
                fullscreenHeight,
                fullscreenRefreshRate,
                initial: true);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void PollEvents()
    {
        ThrowIfDisposed();

        while (SDL3.SDL.PollEvent(out var e))
        {
            SDL3.SDL.EventType type = (SDL3.SDL.EventType)e.Type;
            switch (type)
            {
                case SDL3.SDL.EventType.Quit:
                case SDL3.SDL.EventType.WindowCloseRequested:
                    if (IsOpen)
                    {
                        IsOpen = false;
                        CloseRequested?.Invoke();
                    }
                    break;

                case SDL3.SDL.EventType.WindowResized:
                    if (e.Window.Data1 > 0 && e.Window.Data2 > 0)
                    {
                        Size = new Vect2(e.Window.Data1, e.Window.Data2);
                        Resized?.Invoke(e.Window.Data1, e.Window.Data2);
                    }
                    break;

                case SDL3.SDL.EventType.WindowFocusGained:
                    FocusGained?.Invoke();
                    break;

                case SDL3.SDL.EventType.WindowFocusLost:
                    FocusLost?.Invoke();
                    break;

                case SDL3.SDL.EventType.MouseWheel:
                {
                    int delta = e.Wheel.IntegerY != 0
                        ? e.Wheel.IntegerY
                        : (int)MathF.Round(e.Wheel.Y);
                    if (delta != 0)
                        MouseWheelScrolled?.Invoke(delta);
                    break;
                }

                case SDL3.SDL.EventType.DisplayOrientation:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.OrientationChanged);
                    break;
                case SDL3.SDL.EventType.DisplayAdded:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.Added);
                    break;
                case SDL3.SDL.EventType.DisplayRemoved:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.Removed);
                    break;
                case SDL3.SDL.EventType.DisplayMoved:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.Moved);
                    break;
                case SDL3.SDL.EventType.DisplayDesktopModeChanged:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.DesktopModeChanged);
                    break;
                case SDL3.SDL.EventType.DisplayCurrentModeChanged:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.CurrentModeChanged);
                    break;
                case SDL3.SDL.EventType.DisplayContentScaleChanged:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.ContentScaleChanged);
                    break;
                case SDL3.SDL.EventType.UsableBoundsChanged:
                    RaiseDisplayChanged(e.Display.DisplayID, DisplayChangeKind.WorkAreaChanged);
                    break;
            }
        }
    }

    public void SetSize(int width, int height)
    {
        ThrowIfDisposed();
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        if (!SDL3.SDL.SetWindowSize(_window, width, height))
            throw new InvalidOperationException($"SDL could not resize the window: {SDL3.SDL.GetError()}");

        Size = new Vect2(width, height);
    }

    public void SetTitle(string title)
    {
        ThrowIfDisposed();
        if (!SDL3.SDL.SetWindowTitle(_window, title ?? string.Empty))
            throw new InvalidOperationException($"SDL could not set the window title: {SDL3.SDL.GetError()}");
    }

    public void SetMode(WindowMode mode)
        => ApplyWindowState(
            mode,
            Display,
            _fullscreenStyle,
            _fullscreenWidth,
            _fullscreenHeight,
            _fullscreenRefreshRate);

    public void SetDisplay(DisplayId display, bool center = true)
        => ApplyWindowState(
            _mode,
            display,
            _fullscreenStyle,
            _fullscreenWidth,
            _fullscreenHeight,
            _fullscreenRefreshRate);

    /// <summary>
    /// Applies display, window mode, and fullscreen policy as one atomic platform
    /// transition. Keeping these together avoids briefly entering fullscreen on
    /// the wrong monitor or applying an exclusive mode to the previous display.
    /// </summary>
    public void ApplyWindowState(
        WindowMode mode,
        DisplayId display,
        FullscreenStyle fullscreenStyle,
        int fullscreenWidth,
        int fullscreenHeight,
        float fullscreenRefreshRate)
    {
        ThrowIfDisposed();

        if (!display.IsValid || !SdlPlatform.TryGetDisplay(display, out _))
            throw new InvalidOperationException("The requested display is not connected.");

        ApplyWindowStateCore(
            mode,
            display,
            fullscreenStyle,
            fullscreenWidth,
            fullscreenHeight,
            fullscreenRefreshRate,
            initial: false);
    }

    private void ApplyWindowStateCore(
        WindowMode mode,
        DisplayId display,
        FullscreenStyle fullscreenStyle,
        int fullscreenWidth,
        int fullscreenHeight,
        float fullscreenRefreshRate,
        bool initial)
    {
        if (fullscreenStyle == FullscreenStyle.Exclusive)
        {
            if (fullscreenWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(fullscreenWidth));
            if (fullscreenHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(fullscreenHeight));
            if (fullscreenRefreshRate < 0f)
                throw new ArgumentOutOfRangeException(nameof(fullscreenRefreshRate));
        }

        DisplayId currentDisplay = Display;
        bool changingDisplay =
            display.IsValid &&
            currentDisplay.IsValid &&
            display != currentDisplay;

        // Wayland deliberately owns placement of normal top-level windows.
        // Exclusive fullscreen is the one portable path where a requested SDL
        // display mode carries the target output with it.
        if (IsWayland &&
            !initial &&
            changingDisplay &&
            !(mode == WindowMode.Fullscreen && fullscreenStyle == FullscreenStyle.Exclusive))
        {
            throw new PlatformNotSupportedException(
                "Wayland does not allow applications to move normal top-level windows between displays. " +
                "Use exclusive fullscreen to select a specific display, or let the compositor place windowed/desktop-fullscreen windows.");
        }

        if (_mode == WindowMode.Fullscreen)
            Require(SDL3.SDL.SetWindowFullscreen(_window, false), "leave fullscreen mode");

        switch (mode)
        {
            case WindowMode.Windowed:
                ClearFullscreenMode();

                if (!IsWayland)
                    MoveWindowToDisplay(display, center: true, usableBounds: true);

                Require(SDL3.SDL.SetWindowBordered(_window, true), "enable window borders");
                Require(SDL3.SDL.SetWindowResizable(_window, true), "enable window resizing");
                break;

            case WindowMode.Borderless:
                ClearFullscreenMode();

                if (!IsWayland)
                    MoveWindowToDisplay(display, center: true, usableBounds: true);

                Require(SDL3.SDL.SetWindowBordered(_window, false), "disable window borders");
                Require(SDL3.SDL.SetWindowResizable(_window, false), "disable window resizing");
                break;

            case WindowMode.Fullscreen:
                Require(SDL3.SDL.SetWindowBordered(_window, false), "disable window borders");

                if (fullscreenStyle == FullscreenStyle.Exclusive)
                {
                    ConfigureExclusiveFullscreen(
                        display,
                        fullscreenWidth,
                        fullscreenHeight,
                        fullscreenRefreshRate);
                }
                else
                {
                    ClearFullscreenMode();

                    // SDL desktop fullscreen targets the display containing the
                    // window. Windows/X11 let us place it first; Wayland does not.
                    if (!IsWayland)
                        MoveWindowToDisplay(display, center: true, usableBounds: false);
                }

                Require(SDL3.SDL.SetWindowFullscreen(_window, true), "enter fullscreen mode");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        _mode = mode;
        _fullscreenStyle = fullscreenStyle;
        _fullscreenWidth = fullscreenWidth;
        _fullscreenHeight = fullscreenHeight;
        _fullscreenRefreshRate = fullscreenRefreshRate;
    }

    private unsafe void ConfigureExclusiveFullscreen(
        DisplayId display,
        int width,
        int height,
        float refreshRate)
    {
        DisplayInfo info = SdlPlatform.GetDisplay(display);

        if (!SDL3.SDL.GetClosestFullscreenDisplayMode(
                display.NativeValue,
                width,
                height,
                refreshRate,
                true,
                out SDL3.SDL.DisplayMode nativeMode))
        {
            string requestedRate = refreshRate > 0f ? $" @ {refreshRate:0.###} Hz" : string.Empty;
            throw new InvalidOperationException(
                $"SDL could not find fullscreen mode {width}x{height}{requestedRate} " +
                $"for display {info.Index} ('{info.Name}'): {SDL3.SDL.GetError()}");
        }

        SDL3.SDL.DisplayMode* mode = &nativeMode;
        Require(
            SDL3.SDL.SetWindowFullscreenMode(_window, (IntPtr)mode),
            "select the exclusive fullscreen display mode");
    }

    private void ClearFullscreenMode()
    {
        Require(
            SDL3.SDL.SetWindowFullscreenMode(_window, IntPtr.Zero),
            "clear the exclusive fullscreen display mode");
    }

    private void MoveWindowToDisplay(DisplayId display, bool center, bool usableBounds)
    {
        Rect2 area = SdlPlatform.GetDisplayBounds(display, usable: usableBounds);

        int x = (int)area.X;
        int y = (int)area.Y;

        if (center)
        {
            x += Math.Max(0, ((int)area.Width - (int)Size.X) / 2);
            y += Math.Max(0, ((int)area.Height - (int)Size.Y) / 2);
        }

        Require(
            SDL3.SDL.SetWindowPosition(_window, x, y),
            "move the window to the requested display");
    }

    public void Close()
    {
        if (_disposed)
            return;
        IsOpen = false;
    }

    public nint GetProcAddress(string name)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_glContext == IntPtr.Zero)
            return 0;

        return SDL3.SDL.GLGetProcAddress(name);
    }

    public void SwapBuffers()
    {
        ThrowIfDisposed();

        if (_glContext == IntPtr.Zero)
            throw new InvalidOperationException("The active renderer does not own an SDL OpenGL context.");

        if (!SDL3.SDL.GLSwapWindow(_window))
            throw new InvalidOperationException($"SDL buffer swap failed: {SDL3.SDL.GetError()}");
    }

    public bool TrySetSwapInterval(int interval)
    {
        if (_disposed || _glContext == IntPtr.Zero)
            return false;

        return SDL3.SDL.GLSetSwapInterval(interval);
    }

    /// <summary>
    /// Attempts to retrieve a borrowed platform-native handle for an external
    /// renderer backend. Unsupported handle kinds return false and zero.
    /// </summary>
    public bool TryGetNativeHandle(NativeWindowHandleKind kind, out nint handle)
    {
        handle = 0;

        if (_disposed || _window == IntPtr.Zero)
            return false;

        uint windowProperties = SDL3.SDL.GetWindowProperties(_window);
        if (windowProperties == 0)
            return false;

        handle = (Capabilities.Backend, kind) switch
        {
            // Win32
            (NativeWindowBackend.Windows, NativeWindowHandleKind.Window)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWin32HWNDPointer),

            (NativeWindowBackend.Windows, NativeWindowHandleKind.Display)
                => GetCurrentDisplayPointer(SDL3.SDL.Props.DisplayWindowsHMonitorPointer),

            (NativeWindowBackend.Windows, NativeWindowHandleKind.Instance)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWin32InstancePointer),

            (NativeWindowBackend.Windows, NativeWindowHandleKind.Surface)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWin32HDCPointer),

            // X11 / XWayland
            (NativeWindowBackend.X11, NativeWindowHandleKind.Window)
                => GetNumber(windowProperties, SDL3.SDL.Props.WindowX11WindowNumber),

            (NativeWindowBackend.X11, NativeWindowHandleKind.Display)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowX11DisplayPointer),

            // Native Wayland
            (NativeWindowBackend.Wayland, NativeWindowHandleKind.Window)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWaylandSurfacePointer),

            (NativeWindowBackend.Wayland, NativeWindowHandleKind.Display)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWaylandDisplayPointer),

            (NativeWindowBackend.Wayland, NativeWindowHandleKind.Surface)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowWaylandSurfacePointer),

            // Cocoa
            (NativeWindowBackend.Cocoa, NativeWindowHandleKind.Window)
                => GetPointer(windowProperties, SDL3.SDL.Props.WindowCocoaWindowPointer),

            _ => 0
        };

        return handle != 0;
    }

    private static nint GetPointer(uint properties, string name)
        => SDL3.SDL.GetPointerProperty(properties, name, IntPtr.Zero);

    private static nint GetNumber(uint properties, string name)
    {
        long value = SDL3.SDL.GetNumberProperty(properties, name, 0);
        return value == 0 ? 0 : unchecked((nint)value);
    }

    private nint GetCurrentDisplayPointer(string name)
    {
        DisplayId display = Display;
        if (!display.IsValid)
            return 0;

        uint displayProperties = SDL3.SDL.GetDisplayProperties(display.NativeValue);
        if (displayProperties == 0)
            return 0;

        return SDL3.SDL.GetPointerProperty(displayProperties, name, IntPtr.Zero);
    }


    public void GetMouseState(Span<bool> buttons, out int x, out int y)
    {
        ThrowIfDisposed();

        if (buttons.Length < 5)
            throw new ArgumentException("Mouse button span must contain at least five elements.", nameof(buttons));

        SDL3.SDL.MouseButtonFlags flags = SDL3.SDL.GetMouseState(out float mouseX, out float mouseY);

        x = (int)MathF.Round(mouseX);
        y = (int)MathF.Round(mouseY);

        buttons[0] = (flags & SDL3.SDL.MouseButtonFlags.Left) != 0;
        buttons[1] = (flags & SDL3.SDL.MouseButtonFlags.Right) != 0;
        buttons[2] = (flags & SDL3.SDL.MouseButtonFlags.Middle) != 0;
        buttons[3] = (flags & SDL3.SDL.MouseButtonFlags.X1) != 0;
        buttons[4] = (flags & SDL3.SDL.MouseButtonFlags.X2) != 0;
    }

    public void SetMousePosition(int x, int y)
    {
        ThrowIfDisposed();
        SDL3.SDL.WarpMouseInWindow(_window, x, y);
    }

    public static void SetGlobalMousePosition(int x, int y)
    {
        SDL3.SDL.WarpMouseGlobal(x, y);
    }

    public unsafe bool TrySetIcon(int width, int height, byte[] rgbaPixels)
    {
        if (_disposed || _window == IntPtr.Zero || width <= 0 || height <= 0 || rgbaPixels == null)
            return false;

        int expected = checked(width * height * 4);
        if (rgbaPixels.Length != expected)
            return false;

        fixed (byte* pixels = rgbaPixels)
        {
            IntPtr surface = SDL3.SDL.CreateSurfaceFrom(
                width,
                height,
                BitConverter.IsLittleEndian
                    ? SDL3.SDL.PixelFormat.ABGR8888
                    : SDL3.SDL.PixelFormat.RGBA8888,
                (IntPtr)pixels,
                checked(width * 4));

            if (surface == IntPtr.Zero)
                return false;

            try
            {
                return SDL3.SDL.SetWindowIcon(_window, surface);
            }
            finally
            {
                SDL3.SDL.DestroySurface(surface);
            }
        }
    }

    private void RaiseDisplayChanged(uint nativeDisplayId, DisplayChangeKind kind)
    {
        var change = new DisplayChangedEvent(new DisplayId(nativeDisplayId), kind);
        DisplayManager.NotifyChanged(change.Display, change.Kind);
        DisplayChanged?.Invoke(change);
    }

    private static SDL3.SDL.WindowFlags GetInitialWindowFlags(WindowMode mode)
        => mode switch
        {
            WindowMode.Borderless => SDL3.SDL.WindowFlags.Borderless,
            _ => SDL3.SDL.WindowFlags.Resizable
        };

    private static bool IsWayland
        => SdlPlatform.GetWindowBackend() == NativeWindowBackend.Wayland;

    private static void ConfigureOpenGL(GraphicsVersion version)
    {
        SDL3.SDL.GLResetAttributes();

        if (!SDL3.SDL.GLSetAttribute(SDL3.SDL.GLAttr.ContextMajorVersion, checked((int)version.Major)) ||
            !SDL3.SDL.GLSetAttribute(SDL3.SDL.GLAttr.ContextMinorVersion, checked((int)version.Minor)) ||
            !SDL3.SDL.GLSetAttribute(SDL3.SDL.GLAttr.ContextProfileMask, (int)SDL3.SDL.GLProfile.Core) ||
            !SDL3.SDL.GLSetAttribute(SDL3.SDL.GLAttr.DoubleBuffer, 1))
        {
            throw new InvalidOperationException($"SDL could not configure the OpenGL context: {SDL3.SDL.GetError()}");
        }
    }

    private static void Require(bool success, string operation)
    {
        if (!success)
            throw new InvalidOperationException($"SDL could not {operation}: {SDL3.SDL.GetError()}");
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_glContext != IntPtr.Zero)
        {
            SDL3.SDL.GLDestroyContext(_glContext);
            _glContext = IntPtr.Zero;
        }

        if (_window != IntPtr.Zero)
        {
            SDL3.SDL.DestroyWindow(_window);
            _window = IntPtr.Zero;
        }

        IsOpen = false;
        _disposed = true;
        SdlPlatform.Release();
    }
}
