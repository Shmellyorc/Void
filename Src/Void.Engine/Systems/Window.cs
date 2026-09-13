// ============================================================================
//  Window.cs
// ============================================================================
//  Native game-window management, renderer presentation, display selection,
//  fullscreen configuration, scaling, input window state, and window events.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;
using Void.Engine.Graphics.RenderTargets;
using Void.Engine.Platform.SDL;

namespace Void.Engine.Systems;

/// <summary>
/// Defines how the configured game viewport is presented inside the native window.
/// </summary>
public enum WindowScaleMode
{
    /// <summary>
    /// Stretches the viewport to fill the entire window without preserving aspect ratio.
    /// </summary>
    Stretch,

    /// <summary>
    /// Preserves aspect ratio and scales the viewport by a whole-number factor of at least one.
    /// </summary>
    PixelPerfect,

    /// <summary>
    /// Preserves aspect ratio and fits the entire viewport inside the window.
    /// </summary>
    Fit,

    /// <summary>
    /// Preserves aspect ratio and fills the window, allowing content outside the window bounds.
    /// </summary>
    Fill,

    /// <summary>
    /// Presents the viewport at its configured size without scaling.
    /// </summary>
    None
}

/// <summary>
/// Defines the native window presentation mode.
/// </summary>
public enum WindowMode
{
    /// <summary>A normal resizable window.</summary>
    Windowed,

    /// <summary>A borderless window.</summary>
    Borderless,

    /// <summary>A fullscreen window using the configured <see cref="FullscreenStyle"/>.</summary>
    Fullscreen
}

/// <summary>
/// Manages the native game window and the renderer-owned main game surface.
/// </summary>
/// <remarks>
/// <para>
/// Window settings changed through the fluent setter methods are queued until
/// <see cref="ApplyChanges"/> is called. Properties such as <see cref="Mode"/>,
/// <see cref="VSyncEnabled"/>, and the fullscreen properties report the currently
/// applied state.
/// </para>
/// <para>
/// Rendering is performed through VOID's selected renderer backend while native
/// window ownership remains an internal platform detail.
/// </para>
/// <para>
/// Example:
/// <code>
/// using var window = new Window(1280, 720, "My Game");
/// window.SetSize(1600, 900)
///       .SetVSync(false)
///       .ApplyChanges();
/// </code>
/// </para>
/// </remarks>
public sealed class Window : IDisposable
{
    private readonly IRendererBackend _renderer;
    private readonly SdlWindowHost _window;
    private readonly TextureRenderTarget _mainRenderTarget;
    private readonly RendererPresenter _presenter;

    private int _pendingWidth;
    private int _pendingHeight;
    private WindowMode _pendingMode;
    private bool _pendingVSync;
    private string _pendingTitle;
    private DisplayId _pendingDisplay;
    private FullscreenStyle _pendingFullscreenStyle;
    private int _pendingFullscreenWidth;
    private int _pendingFullscreenHeight;
    private float _pendingFullscreenRefreshRate;
    private bool _hasPendingChanges;

    private int _appliedWidth;
    private int _appliedHeight;
    private WindowMode _appliedMode;
    private bool _appliedVSync;
    private string _appliedTitle;
    private DisplayId _appliedDisplay;
    private FullscreenStyle _appliedFullscreenStyle;
    private int _appliedFullscreenWidth;
    private int _appliedFullscreenHeight;
    private float _appliedFullscreenRefreshRate;

    private Vect2 _windowSize;
    private readonly Vect2 _renderSize;
    private bool _isDisposed;
    private readonly int _superSample;
    private readonly WindowScaleMode _scaleMode;
    private byte[] _iconData;

    internal TextureRenderTarget MainRenderTarget => _mainRenderTarget;
    internal IRendererBackend Renderer => _renderer;

    internal void GetMouseState(Span<bool> buttons, out int x, out int y)
        => _window.GetMouseState(buttons, out x, out y);

    internal void SetMousePosition(int x, int y)
        => _window.SetMousePosition(x, y);

    internal void SetGlobalMousePosition(int x, int y)
        => SdlWindowHost.SetGlobalMousePosition(x, y);

    /// <summary>
    /// Gets the current native window size in pixels.
    /// </summary>
    public Vect2 WindowSize => _windowSize;

    /// <summary>
    /// Gets the renderer-owned game surface size.
    /// </summary>
    /// <remarks>
    /// This is the configured viewport multiplied by the supersampling factor.
    /// </remarks>
    public Vect2 RenderSize => _renderSize;

    /// <summary>
    /// Gets the currently applied window mode.
    /// </summary>
    public WindowMode Mode => _appliedMode;

    /// <summary>
    /// Gets whether vertical synchronization is currently enabled.
    /// </summary>
    public bool VSyncEnabled => _appliedVSync;

    /// <summary>
    /// Gets whether the native window currently has input focus.
    /// </summary>
    public bool IsFocused { get; private set; }

    /// <summary>
    /// Gets whether the window is still open and has not been disposed.
    /// </summary>
    public bool IsOpen => !_isDisposed && _window.IsOpen;

    /// <summary>
    /// Gets whether one or more queued window settings have not yet been applied.
    /// </summary>
    public bool HasPendingChanges => _hasPendingChanges;

    /// <summary>
    /// Gets the currently applied fullscreen style.
    /// </summary>
    public FullscreenStyle FullscreenStyle => _appliedFullscreenStyle;

    /// <summary>
    /// Gets the currently applied exclusive-fullscreen width.
    /// </summary>
    public int FullscreenWidth => _appliedFullscreenWidth;

    /// <summary>
    /// Gets the currently applied exclusive-fullscreen height.
    /// </summary>
    public int FullscreenHeight => _appliedFullscreenHeight;

    /// <summary>
    /// Gets the currently applied exclusive-fullscreen refresh rate.
    /// </summary>
    public float FullscreenRefreshRate => _appliedFullscreenRefreshRate;

    /// <summary>
    /// Gets the capabilities of the native window backend initialized for this window.
    /// </summary>
    public WindowCapabilities Capabilities => _window.Capabilities;

    /// <summary>
    /// Gets the active native window-system backend.
    /// </summary>
    public NativeWindowBackend PlatformBackend => Capabilities.Backend;

    /// <summary>
    /// Gets the display the native window currently occupies.
    /// </summary>
    public DisplayId CurrentDisplayId => _window.Display;

    /// <summary>
    /// Gets a fresh snapshot of the display the native window currently occupies.
    /// </summary>
    public DisplayInfo CurrentDisplay => DisplayManager.GetDisplay(CurrentDisplayId);

    /// <summary>
    /// Gets the current enumeration index of the window's display, or <c>-1</c> if disconnected.
    /// </summary>
    public int DisplayIndex => DisplayManager.GetIndex(CurrentDisplayId);

    /// <summary>
    /// Gets or sets the callback invoked when the native window size changes.
    /// </summary>
    public Action<Vect2> OnWindowResized { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the window gains focus.
    /// </summary>
    public Action OnFocusGained { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the window loses focus.
    /// </summary>
    public Action OnFocusLost { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the native window requests to close.
    /// </summary>
    public Action OnWindowClosed { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the mouse wheel is scrolled.
    /// </summary>
    public Action<int> OnMouseWheelScrolled { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the connected-display configuration changes.
    /// </summary>
    public Action<DisplayChangedEvent> OnDisplayChanged { get; set; }

    /// <summary>
    /// Creates a game window and initializes the configured renderer backend.
    /// </summary>
    /// <param name="width">Initial window width in pixels.</param>
    /// <param name="height">Initial window height in pixels.</param>
    /// <param name="title">Initial window title.</param>
    /// <param name="mode">Initial window mode.</param>
    /// <param name="vsync">Whether vertical synchronization is initially enabled.</param>
    /// <param name="iconData">Optional encoded image data used as the window icon.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="width"/> or <paramref name="height"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is null or empty.
    /// </exception>
    public Window(int width, int height, string title, WindowMode mode = WindowMode.Windowed, bool vsync = true, byte[] iconData = null)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));

        Logger.Instance.InfoWithCategory("Window", "Creating SDL window: {0}x{1} '{2}' Mode={3} VSync={4}",
            width, height, title, mode, vsync);

        _pendingWidth = _appliedWidth = width;
        _pendingHeight = _appliedHeight = height;
        _pendingMode = _appliedMode = mode;
        _pendingVSync = _appliedVSync = vsync;
        _pendingTitle = _appliedTitle = title;

        // Keep only the configured display index here. Resolving an SDL display ID
        // before SdlWindowHost owns a persistent SDL session can produce a stale ID
        // after SDL is shut down and initialized again.
        _pendingDisplay = _appliedDisplay = default;

        _pendingFullscreenStyle = _appliedFullscreenStyle = GameSettings.Instance.FullscreenStyle;
        _pendingFullscreenWidth = _appliedFullscreenWidth = GameSettings.Instance.FullscreenWidth;
        _pendingFullscreenHeight = _appliedFullscreenHeight = GameSettings.Instance.FullscreenHeight;
        _pendingFullscreenRefreshRate = _appliedFullscreenRefreshRate = GameSettings.Instance.FullscreenRefreshRate;

        _superSample = GameSettings.Instance.SuperSample;
        _scaleMode = GameSettings.Instance.WindowScaleMode;
        _windowSize = new Vect2(width, height);
        _renderSize = GameSettings.Instance.Viewport * _superSample;

        IRendererBackend renderer = null;
        SdlWindowHost host = null;
        TextureRenderTarget mainTarget = null;
        RendererPresenter presenter = null;

        try
        {
            renderer = RendererBootstrap.CreateBackend(GameSettings.Instance);
            host = RendererBootstrap.CreateWindow(
                renderer,
                width,
                height,
                title,
                vsync,
                mode,
                GameSettings.Instance.DisplayIndex,
                _pendingFullscreenStyle,
                _pendingFullscreenWidth,
                _pendingFullscreenHeight,
                _pendingFullscreenRefreshRate);
            renderer.Initialize(RendererBootstrap.CreateContext(GameSettings.Instance, host));

            // Public Window constructor parameters win over singleton defaults.
            host.TrySetSwapInterval(vsync ? 1 : 0);

            _renderer = renderer;
            _window = host;

            // Resolve the stable runtime handle only after SDL is persistently
            // initialized by SdlWindowHost.
            _pendingDisplay = _appliedDisplay = host.Display;

            HookWindowEvents();
            IsFocused = true;

            mainTarget = new TextureRenderTarget((int)_renderSize.X, (int)_renderSize.Y);
            presenter = new RendererPresenter(renderer.Device, renderer.Default2DShader);
            _mainRenderTarget = mainTarget;
            _presenter = presenter;

            SetIcon(iconData);
        }
        catch
        {
            presenter?.Dispose();
            mainTarget?.Dispose();
            renderer?.Dispose();
            host?.Dispose();
            throw;
        }

        Logger.Instance.InfoWithCategory("Window", "SDL window and renderer created successfully: {0}", _renderer.Name);
    }

    /// <summary>
    /// Queues a new window width.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="width"/> is not positive.
    /// </exception>
    public Window SetWidth(int width)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
        _pendingWidth = width;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a new window height.
    /// </summary>
    /// <param name="height">Height in pixels.</param>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="height"/> is not positive.
    /// </exception>
    public Window SetHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
        _pendingHeight = height;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a new window size.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <returns>This window for fluent configuration.</returns>
    public Window SetSize(int width, int height)
    {
        SetWidth(width);
        SetHeight(height);
        return this;
    }

    /// <summary>
    /// Queues a new window mode.
    /// </summary>
    /// <param name="mode">The window mode to apply.</param>
    /// <returns>This window for fluent configuration.</returns>
    public Window SetMode(WindowMode mode)
    {
        _pendingMode = mode;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a vertical-synchronization setting.
    /// </summary>
    /// <param name="enabled">Whether vertical synchronization should be enabled.</param>
    /// <returns>This window for fluent configuration.</returns>
    public Window SetVSync(bool enabled)
    {
        _pendingVSync = enabled;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a new window title.
    /// </summary>
    /// <param name="title">The title to apply.</param>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is null or empty.
    /// </exception>
    public Window SetTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
        _pendingTitle = title;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a move to a connected display by its current enumeration index.
    /// </summary>
    /// <param name="displayIndex">The current display enumeration index.</param>
    /// <returns>This window for fluent configuration.</returns>
    /// <remarks>
    /// Call <see cref="ApplyChanges"/> to apply the queued display change.
    /// </remarks>
    public Window SetDisplay(int displayIndex)
        => SetDisplay(DisplayManager.GetDisplay(displayIndex).Id);

    /// <summary>
    /// Queues a move to a connected display by stable VOID display handle.
    /// </summary>
    /// <param name="display">The connected display to target.</param>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested display is invalid or no longer connected.
    /// </exception>
    /// <remarks>
    /// Call <see cref="ApplyChanges"/> to apply the queued display change.
    /// </remarks>
    public Window SetDisplay(DisplayId display)
    {
        if (!display.IsValid || !DisplayManager.TryGetDisplay(display, out _))
            throw new InvalidOperationException("The requested display is not connected.");

        _pendingDisplay = display;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues desktop fullscreen using the selected display's current desktop mode.
    /// </summary>
    /// <returns>This window for fluent configuration.</returns>
    /// <remarks>
    /// The selected display keeps its current desktop resolution and refresh rate.
    /// Call <see cref="ApplyChanges"/> to apply the queued state.
    /// </remarks>
    public Window SetDesktopFullscreen()
    {
        _pendingMode = WindowMode.Fullscreen;
        _pendingFullscreenStyle = FullscreenStyle.Desktop;
        _pendingFullscreenWidth = 0;
        _pendingFullscreenHeight = 0;
        _pendingFullscreenRefreshRate = 0f;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues exclusive fullscreen at the requested resolution and refresh rate.
    /// </summary>
    /// <param name="width">Fullscreen width in pixels.</param>
    /// <param name="height">Fullscreen height in pixels.</param>
    /// <param name="refreshRate">
    /// Requested refresh rate, or zero to allow the platform layer to choose the closest mode.
    /// </param>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when width or height is not positive, or when refresh rate is negative.
    /// </exception>
    public Window SetFullscreenMode(int width, int height, float refreshRate = 0f)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (refreshRate < 0f)
            throw new ArgumentOutOfRangeException(nameof(refreshRate));

        _pendingMode = WindowMode.Fullscreen;
        _pendingFullscreenStyle = FullscreenStyle.Exclusive;
        _pendingFullscreenWidth = width;
        _pendingFullscreenHeight = height;
        _pendingFullscreenRefreshRate = refreshRate;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues exclusive fullscreen using a mode returned by <see cref="DisplayManager"/>.
    /// </summary>
    /// <param name="mode">The display mode to use.</param>
    /// <returns>This window for fluent configuration.</returns>
    public Window SetFullscreenMode(DisplayMode mode)
        => SetFullscreenMode(
            checked((int)mode.Width),
            checked((int)mode.Height),
            mode.RefreshRate);

    /// <summary>
    /// Applies all queued window, display, fullscreen, title, and VSync changes.
    /// </summary>
    /// <returns>This window for fluent configuration.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the window has been disposed.</exception>
    public Window ApplyChanges()
    {
        ThrowIfDisposed();
        if (!_hasPendingChanges)
            return this;

        bool sizeChanged = _pendingWidth != _appliedWidth || _pendingHeight != _appliedHeight;
        bool modeChanged = _pendingMode != _appliedMode;
        bool vsyncChanged = _pendingVSync != _appliedVSync;
        bool displayChanged = _pendingDisplay != _appliedDisplay;
        bool fullscreenChanged =
            _pendingFullscreenStyle != _appliedFullscreenStyle ||
            _pendingFullscreenWidth != _appliedFullscreenWidth ||
            _pendingFullscreenHeight != _appliedFullscreenHeight ||
            MathF.Abs(_pendingFullscreenRefreshRate - _appliedFullscreenRefreshRate) > 0.001f;

        if (sizeChanged)
        {
            _window.SetSize(_pendingWidth, _pendingHeight);
            HandleResize(_pendingWidth, _pendingHeight);
        }

        if (displayChanged || modeChanged || fullscreenChanged)
        {
            _window.ApplyWindowState(
                _pendingMode,
                _pendingDisplay,
                _pendingFullscreenStyle,
                _pendingFullscreenWidth,
                _pendingFullscreenHeight,
                _pendingFullscreenRefreshRate);
        }

        if (!string.Equals(_pendingTitle, _appliedTitle, StringComparison.Ordinal))
            _window.SetTitle(_pendingTitle);

        if (vsyncChanged && !_window.TrySetSwapInterval(_pendingVSync ? 1 : 0) && _renderer.Api == GraphicsApi.OpenGL)
            throw new InvalidOperationException("Unable to update the OpenGL swap interval.");

        _appliedWidth = _pendingWidth;
        _appliedHeight = _pendingHeight;
        _appliedMode = _pendingMode;
        _appliedVSync = _pendingVSync;
        _appliedTitle = _pendingTitle;
        _appliedDisplay = _pendingDisplay;
        _appliedFullscreenStyle = _pendingFullscreenStyle;
        _appliedFullscreenWidth = _pendingFullscreenWidth;
        _appliedFullscreenHeight = _pendingFullscreenHeight;
        _appliedFullscreenRefreshRate = _pendingFullscreenRefreshRate;
        _hasPendingChanges = false;

        Logger.Instance.InfoWithCategory(
            "Window",
            "Window changes applied: {0}x{1} {2} VSync={3} Display={4} Fullscreen={5} {6}x{7}@{8:0.###}",
            _appliedWidth,
            _appliedHeight,
            _appliedMode,
            _appliedVSync,
            DisplayIndex,
            _appliedFullscreenStyle,
            _appliedFullscreenWidth,
            _appliedFullscreenHeight,
            _appliedFullscreenRefreshRate);
        return this;
    }

    /// <summary>
    /// Toggles between fullscreen and windowed mode and applies the change immediately.
    /// </summary>
    /// <returns>This window.</returns>
    public Window ToggleFullscreen()
    {
        SetMode(_appliedMode == WindowMode.Fullscreen ? WindowMode.Windowed : WindowMode.Fullscreen);
        return ApplyChanges();
    }

    internal void DispatchEvents()
    {
        ThrowIfDisposed();
        _window.PollEvents();
    }

    internal void BeginRender(Color clearColor)
    {
        ThrowIfDisposed();

        // Clear native backbuffer to black first so Fit/PixelPerfect/None retain
        // clean letterbox regions, then render the game into the off-screen target.
        _renderer.BeginFrame(Color.Black);
        _mainRenderTarget.Clear(clearColor);
    }

    internal void EndRender()
    {
        ThrowIfDisposed();

        _mainRenderTarget.Display();
        Rect2 destination = CalculatePresentationRect();
        _presenter.Present(
            _mainRenderTarget.GraphicsTexture,
            destination,
            (int)_windowSize.X,
            (int)_windowSize.Y);
        _renderer.EndFrame();
    }

    /// <summary>
    /// Closes the native window if it has not already been disposed.
    /// </summary>
    public void Close()
    {
        if (_isDisposed) return;
        _window.Close();
    }

    private void HookWindowEvents()
    {
        _window.Resized += HandleResize;
        _window.FocusGained += () =>
        {
            IsFocused = true;
            OnFocusGained?.Invoke();
        };
        _window.FocusLost += () =>
        {
            IsFocused = false;
            OnFocusLost?.Invoke();
        };
        _window.CloseRequested += () => OnWindowClosed?.Invoke();
        _window.MouseWheelScrolled += delta => OnMouseWheelScrolled?.Invoke(delta);
        _window.DisplayChanged += change =>
        {
            // When the active display disappears SDL may relocate the native
            // window. Refresh our pending/applied handle from SDL after removal.
            if (change.Kind == DisplayChangeKind.Removed && _appliedDisplay == change.Display)
            {
                DisplayId current = _window.Display;
                if (current.IsValid)
                    _pendingDisplay = _appliedDisplay = current;
            }

            OnDisplayChanged?.Invoke(change);
        };
    }

    private void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        bool changed = width != _appliedWidth || height != _appliedHeight;

        _appliedWidth = _pendingWidth = width;
        _appliedHeight = _pendingHeight = height;
        _windowSize = new Vect2(width, height);
        _renderer.Resize(width, height);

        if (changed)
        {
            Logger.Instance.InfoWithCategory("Window", "Window resized to {0}x{1}", width, height);
            OnWindowResized?.Invoke(_windowSize);
        }
    }

    private Rect2 CalculatePresentationRect()
    {
        Vect2 viewport = GameSettings.Instance.Viewport;
        if (viewport.X <= 0f || viewport.Y <= 0f)
            return new Rect2(0f, 0f, _windowSize.X, _windowSize.Y);

        float scaleX = _windowSize.X / viewport.X;
        float scaleY = _windowSize.Y / viewport.Y;

        Vect2 scaledSize;
        switch (_scaleMode)
        {
            case WindowScaleMode.Stretch:
                scaledSize = _windowSize;
                break;

            case WindowScaleMode.PixelPerfect:
            {
                float scale = MathF.Max(1f, MathF.Floor(MathF.Min(scaleX, scaleY)));
                scaledSize = viewport * scale;
                break;
            }

            case WindowScaleMode.Fit:
            {
                float scale = MathF.Min(scaleX, scaleY);
                scaledSize = viewport * scale;
                break;
            }

            case WindowScaleMode.Fill:
            {
                float scale = MathF.Max(scaleX, scaleY);
                scaledSize = viewport * scale;
                break;
            }

            case WindowScaleMode.None:
                scaledSize = viewport;
                break;

            default:
                scaledSize = _windowSize;
                break;
        }

        Vect2 position = (_windowSize - scaledSize) / 2f;
        return new Rect2(position, scaledSize);
    }

    // These legacy helpers retain their original primary-display semantics,
    // but SDL now supplies the display data. A richer multi-monitor API can be
    // layered on top without changing these existing methods.

    /// <summary>
    /// Gets the desktop resolution of the primary display.
    /// </summary>
    /// <returns>The primary display resolution in pixels.</returns>
    public static Vect2 GetDesktopResolution()
        => SdlPlatform.GetPrimaryDesktopResolution();

    /// <summary>
    /// Gets the supported resolutions reported for the primary display.
    /// </summary>
    /// <returns>A list of supported resolutions.</returns>
    public static List<Vect2> GetSupportedResolutions()
        => SdlPlatform.GetPrimarySupportedResolutions();

    /// <summary>
    /// Gets primary-display resolutions matching an aspect ratio within a tolerance.
    /// </summary>
    /// <param name="ratioWidth">Aspect-ratio width component.</param>
    /// <param name="ratioHeight">Aspect-ratio height component.</param>
    /// <param name="tolerance">Maximum absolute difference from the requested ratio.</param>
    /// <returns>A list of matching supported resolutions.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either ratio component is not positive.
    /// </exception>
    public static List<Vect2> GetSupportedResolutionsByAspectRatio(int ratioWidth, int ratioHeight, float tolerance = 0.01f)
    {
        if (ratioWidth <= 0 || ratioHeight <= 0)
            throw new ArgumentOutOfRangeException("Ratio components must be positive.");

        float targetRatio = (float)ratioWidth / ratioHeight;
        var result = new List<Vect2>();
        foreach (var resolution in GetSupportedResolutions())
        {
            float ratio = resolution.X / resolution.Y;
            if (MathF.Abs(ratio - targetRatio) <= tolerance)
                result.Add(resolution);
        }
        return result;
    }

    /// <summary>
    /// Finds the supported primary-display resolution closest to a requested size.
    /// </summary>
    /// <param name="width">Requested width.</param>
    /// <param name="height">Requested height.</param>
    /// <returns>
    /// The closest supported resolution, or the requested size when no supported
    /// resolutions are reported.
    /// </returns>
    public static Vect2 GetClosestSupportedResolution(int width, int height)
    {
        var resolutions = GetSupportedResolutions();
        if (resolutions.Count == 0)
            return new Vect2(width, height);

        Vect2 closest = resolutions[0];
        float closestDist = float.MaxValue;
        foreach (var res in resolutions)
        {
            float dist = MathF.Abs(res.X - width) + MathF.Abs(res.Y - height);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = res;
            }
        }
        return closest;
    }

    /// <summary>
    /// Determines whether a resolution is supported by the primary display.
    /// </summary>
    /// <param name="width">Resolution width.</param>
    /// <param name="height">Resolution height.</param>
    /// <returns><see langword="true"/> when the resolution is supported; otherwise, <see langword="false"/>.</returns>
    public static bool IsResolutionSupported(int width, int height)
        => SdlPlatform.IsPrimaryResolutionSupported(width, height);

    /// <summary>
    /// Loads encoded image data from a file and applies it as the window icon.
    /// </summary>
    /// <param name="iconPath">Path to the icon image file.</param>
    /// <remarks>
    /// Null or empty paths are ignored. Missing files are logged as warnings.
    /// </remarks>
    public void SetIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath)) return;
        if (!File.Exists(iconPath))
        {
            Logger.Instance.WarningWithCategory("Window", "Icon file not found: {0}", iconPath);
            return;
        }
        SetIcon(File.ReadAllBytes(iconPath));
    }

    /// <summary>
    /// Applies encoded image data as the window icon.
    /// </summary>
    /// <param name="iconData">Encoded image data.</param>
    /// <remarks>
    /// Null or empty data is ignored. Decode or platform failures are logged as warnings.
    /// </remarks>
    public void SetIcon(byte[] iconData)
    {
        if (iconData == null || iconData.Length == 0)
            return;

        _iconData = iconData;
        ApplyIcon();
    }

    private void ApplyIcon()
    {
        if (_iconData == null || _iconData.Length == 0)
            return;

        try
        {
            DecodedImage image = ImageDecoder.DecodeRgba(_iconData);
            if (!_window.TrySetIcon(image.Width, image.Height, image.Pixels))
                Logger.Instance.WarningWithCategory("Window", "SDL could not apply the window icon.");
        }
        catch (Exception ex)
        {
            Logger.Instance.WarningWithCategory("Window", "Could not decode/apply window icon: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Releases the renderer, render surface, presenter, and native window resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        Logger.Instance.InfoWithCategory("Window", "Disposing SDL window and renderer");

        // GPU resources must die while the renderer/context is still alive.
        _presenter.Dispose();
        _mainRenderTarget.Dispose();
        _renderer.Dispose();
        _window.Dispose();

        _isDisposed = true;
        Logger.Instance.InfoWithCategory("Window", "Window disposed");
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
