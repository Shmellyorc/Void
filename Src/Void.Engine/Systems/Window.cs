// ============================================================================
//  Window.cs
// ============================================================================
//  SDL-backed game window. Native ownership is SDL; rendering is performed by
//  the selected VOID renderer backend and presented from a renderer-owned FBO.
// ============================================================================

using Void.Engine.Graphics.Rendering;
using Void.Engine.Graphics.RenderTargets;
using Void.Engine.Platform.SDL;

namespace Void.Engine.Systems;

public enum WindowScaleMode
{
    Stretch,
    PixelPerfect,
    Fit,
    Fill,
    None
}

public enum WindowMode
{
    Windowed,
    Borderless,
    Fullscreen
}

/// <summary>
/// Manages the native SDL window and the renderer-owned main game surface.
/// SDL remains an internal implementation detail.
/// </summary>
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

    public Vect2 WindowSize => _windowSize;
    public Vect2 RenderSize => _renderSize;
    public WindowMode Mode => _appliedMode;
    public bool VSyncEnabled => _appliedVSync;
    public bool IsFocused { get; private set; }
    public bool IsOpen => !_isDisposed && _window.IsOpen;
    public bool HasPendingChanges => _hasPendingChanges;
    public FullscreenStyle FullscreenStyle => _appliedFullscreenStyle;
    public int FullscreenWidth => _appliedFullscreenWidth;
    public int FullscreenHeight => _appliedFullscreenHeight;
    public float FullscreenRefreshRate => _appliedFullscreenRefreshRate;

    /// <summary>
    /// Gets the capabilities of the native window backend that VOID actually
    /// initialized for this window.
    /// </summary>
    public WindowCapabilities Capabilities => _window.Capabilities;

    /// <summary>
    /// Gets the active native window-system backend.
    /// </summary>
    public NativeWindowBackend PlatformBackend => Capabilities.Backend;

    /// <summary>Gets the display the native window currently occupies.</summary>
    public DisplayId CurrentDisplayId => _window.Display;

    /// <summary>Gets a fresh snapshot of the display the native window currently occupies.</summary>
    public DisplayInfo CurrentDisplay => DisplayManager.GetDisplay(CurrentDisplayId);

    /// <summary>Gets the current enumeration index of the window's display, or -1 if disconnected.</summary>
    public int DisplayIndex => DisplayManager.GetIndex(CurrentDisplayId);

    public Action<Vect2> OnWindowResized { get; set; }
    public Action OnFocusGained { get; set; }
    public Action OnFocusLost { get; set; }
    public Action OnWindowClosed { get; set; }
    public Action<int> OnMouseWheelScrolled { get; set; }
    public Action<DisplayChangedEvent> OnDisplayChanged { get; set; }

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

    public Window SetWidth(int width)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
        _pendingWidth = width;
        _hasPendingChanges = true;
        return this;
    }

    public Window SetHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
        _pendingHeight = height;
        _hasPendingChanges = true;
        return this;
    }

    public Window SetSize(int width, int height)
    {
        SetWidth(width);
        SetHeight(height);
        return this;
    }

    public Window SetMode(WindowMode mode)
    {
        _pendingMode = mode;
        _hasPendingChanges = true;
        return this;
    }

    public Window SetVSync(bool enabled)
    {
        _pendingVSync = enabled;
        _hasPendingChanges = true;
        return this;
    }

    public Window SetTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
        _pendingTitle = title;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues a move to a connected display by its current enumeration index.
    /// Call <see cref="ApplyChanges"/> to apply it.
    /// </summary>
    public Window SetDisplay(int displayIndex)
        => SetDisplay(DisplayManager.GetDisplay(displayIndex).Id);

    /// <summary>
    /// Queues a move to a connected display by stable VOID display handle.
    /// Call <see cref="ApplyChanges"/> to apply it.
    /// </summary>
    public Window SetDisplay(DisplayId display)
    {
        if (!display.IsValid || !DisplayManager.TryGetDisplay(display, out _))
            throw new InvalidOperationException("The requested display is not connected.");

        _pendingDisplay = display;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Queues desktop/borderless fullscreen. The selected display keeps its
    /// current desktop resolution and refresh rate.
    /// </summary>
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
    /// Queues exclusive fullscreen at the requested resolution and optional
    /// refresh rate. A refresh rate of zero lets SDL choose the closest mode.
    /// </summary>
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

    /// <summary>Queues exclusive fullscreen using a mode returned by DisplayManager.</summary>
    public Window SetFullscreenMode(DisplayMode mode)
        => SetFullscreenMode(
            checked((int)mode.Width),
            checked((int)mode.Height),
            mode.RefreshRate);

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
    public static Vect2 GetDesktopResolution()
        => SdlPlatform.GetPrimaryDesktopResolution();

    public static List<Vect2> GetSupportedResolutions()
        => SdlPlatform.GetPrimarySupportedResolutions();

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

    public static bool IsResolutionSupported(int width, int height)
        => SdlPlatform.IsPrimaryResolutionSupported(width, height);

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
