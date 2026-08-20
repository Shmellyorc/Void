namespace Void.Engine.Systems;

public enum WindowScaleMode
{
    /// <summary>
    /// Scales the viewport to fill the window exactly (may distort if aspect ratios differ).
    /// </summary>
    Stretch,

    /// <summary>
    /// Scales the viewport up by the largest integer factor that fits, adding borders if needed.
    /// </summary>
    PixelPerfect,

    /// <summary>
    /// Scales the viewport to fit entirely within the window while maintaining aspect ratio.
    /// Adds black bars on sides (letterboxing/pillarboxing).
    /// </summary>
    Fit,

    /// <summary>
    /// Scales the viewport to fill the entire window while maintaining aspect ratio.
    /// Crops overflow (no black bars, but content may be cut off).
    /// </summary>
    Fill,

    /// <summary>
    /// No scaling - viewport is displayed at its native resolution centered in the window.
    /// </summary>
    None
}

/// <summary>
/// Represents a display mode/resolution.
/// </summary>
public readonly struct DisplayMode
{
    public uint Width { get; }
    public uint Height { get; }
    public uint BitsPerPixel { get; }

    internal DisplayMode(uint width, uint height, uint bitsPerPixel)
    {
        Width = width;
        Height = height;
        BitsPerPixel = bitsPerPixel;
    }

    public override string ToString() => $"{Width}x{Height} @ {BitsPerPixel}bpp";
}

/// <summary>
/// Defines the window display mode.
/// </summary>
public enum WindowMode
{
    Windowed,
    Borderless,
    Fullscreen
}

/// <summary>
/// Manages the game window, including creation, resizing, and display modes.
/// Wraps SFML window operations without exposing SFML types.
/// Uses deferred settings pattern - call ApplyChanges() to apply pending changes.
/// </summary>
public sealed class Window : IDisposable
{
    internal SFRenderWindow _window;
    internal SFRenderTexture _renderTexture;
    internal SFSprite _renderSprite;

    // Pending settings
    private int _pendingWidth;
    private int _pendingHeight;
    private WindowMode _pendingMode;
    private bool _pendingVSync;
    private string _pendingTitle;
    private bool _hasPendingChanges;

    // Applied settings
    private int _appliedWidth;
    private int _appliedHeight;
    private WindowMode _appliedMode;
    private bool _appliedVSync;
    private string _appliedTitle;

    private Vect2 _windowSize;
    private Vect2 _renderSize;
    private bool _isDisposed;
    private readonly int _superSample;
    private readonly WindowScaleMode _scaleMode;

    public Vect2 WindowSize => _windowSize;
    public Vect2 RenderSize => _renderSize;
    public WindowMode Mode => _appliedMode;
    public bool VSyncEnabled => _appliedVSync;
    public bool IsFocused { get; private set; }
    public bool IsOpen => _window?.IsOpen ?? false;
    public bool HasPendingChanges => _hasPendingChanges;

    public Action<Vect2> OnWindowResized { get; set; }
    public Action OnFocusGained { get; set; }
    public Action OnFocusLost { get; set; }
    public Action OnWindowClosed { get; set; }
    public Action<int> OnMouseWheelScrolled { get; set; }

    public Window(int width, int height, string title, WindowMode mode = WindowMode.Windowed, bool vsync = true)
    {
        Logger.Instance.InfoWithCategory("Window", "Creating window: {0}x{1} '{2}' Mode={3} VSync={4}",
            width, height, title, mode, vsync);

        _pendingWidth = width;
        _pendingHeight = height;
        _pendingMode = mode;
        _pendingVSync = vsync;
        _pendingTitle = title;
        _hasPendingChanges = false;

        _appliedWidth = width;
        _appliedHeight = height;
        _appliedMode = mode;
        _appliedVSync = vsync;
        _appliedTitle = title;

        _superSample = GameSettings.Instance.SuperSample;
        _scaleMode = GameSettings.Instance.WindowScaleMode;

        _windowSize = new Vect2(width, height);
        _renderSize = GameSettings.Instance.Viewport * _superSample;

        CreateWindow(width, height, title, mode);
        _window.SetVerticalSyncEnabled(vsync);

        var windowView = new SFView(new SFFloatRect(Vect2.Zero, new(width, height)));
        _window.SetView(windowView);

        // Create supersampled render texture
        RecreateRenderTarget();

        _window.Closed += (s, o) =>
        {
            OnWindowClosed?.Invoke();
            _window.Close();
        };

        _window.GainedFocus += (s, o) =>
        {
            IsFocused = true;
            OnFocusGained?.Invoke();
        };

        _window.LostFocus += (s, o) =>
        {
            IsFocused = false;
            OnFocusLost?.Invoke();
        };

        _window.Resized += (s, e) =>
        {
            HandleResize((int)e.Size.X, (int)e.Size.Y);
        };

        _window.MouseWheelScrolled += (sender, args) =>
        {
            OnMouseWheelScrolled?.Invoke((int)args.Delta);
        };

        Logger.Instance.InfoWithCategory("Window", "Window created successfully");
    }

    public Window SetWidth(int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
        _pendingWidth = width;
        _hasPendingChanges = true;
        return this;
    }

    public Window SetHeight(int height)
    {
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
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
        if (string.IsNullOrEmpty(title))
            throw new ArgumentNullException(nameof(title));
        _pendingTitle = title;
        _hasPendingChanges = true;
        return this;
    }

    public Window ApplyChanges()
    {
        if (!_hasPendingChanges)
            return this;

        Logger.Instance.InfoWithCategory("Window", "Applying window changes...");

        bool modeChanged = _pendingMode != _appliedMode;
        bool sizeChanged = _pendingWidth != _appliedWidth || _pendingHeight != _appliedHeight;

        if (modeChanged)
        {
            Logger.Instance.InfoWithCategory("Window", "Mode changed from {0} to {1}, recreating window",
                _appliedMode, _pendingMode);
            RecreateWindow(_pendingWidth, _pendingHeight, _pendingTitle, _pendingMode);
        }
        else
        {
            if (sizeChanged)
            {
                Logger.Instance.InfoWithCategory("Window", "Size changed to {0}x{1}",
                    _pendingWidth, _pendingHeight);

                _window.Size = new SFVector2u((uint)_pendingWidth, (uint)_pendingHeight);

                var windowView = new SFView(new SFFloatRect(Vect2.Zero, new(_pendingWidth, _pendingHeight)));
                _window.SetView(windowView);
            }

            _window.SetTitle(_pendingTitle);
        }

        _window.SetVerticalSyncEnabled(_pendingVSync);

        _appliedWidth = _pendingWidth;
        _appliedHeight = _pendingHeight;
        _appliedMode = _pendingMode;
        _appliedVSync = _pendingVSync;
        _appliedTitle = _pendingTitle;

        _windowSize = new Vect2(_appliedWidth, _appliedHeight);

        Logger.Instance.InfoWithCategory("Window", "Window changes applied: {0}x{1} {2} VSync={3}",
            _appliedWidth, _appliedHeight, _appliedMode, _appliedVSync);

        _hasPendingChanges = false;
        OnWindowResized?.Invoke(_windowSize);
        return this;
    }

    public Window ToggleFullscreen()
    {
        SetMode(_appliedMode == WindowMode.Fullscreen ? WindowMode.Windowed : WindowMode.Fullscreen);
        ApplyChanges();
        return this;
    }

    public void DispatchEvents()
    {
        _window?.DispatchEvents();
    }

    public void BeginRender(Color clearColor)
    {
        _renderTexture.Clear(clearColor);
    }

    public void EndRender()
    {
        _renderTexture.Display();

        var windowView = new SFView(new SFFloatRect(Vect2.Zero, _windowSize));
        _window.SetView(windowView);

        Vect2 viewportSize = GameSettings.Instance.Viewport;
        float scaleX = _windowSize.X / viewportSize.X;
        float scaleY = _windowSize.Y / viewportSize.Y;

        // Calculate the base scale for each mode
        float baseScale;
        switch (_scaleMode)
        {
            case WindowScaleMode.Stretch:
                // For stretch, use independent X/Y scaling
                _renderSprite.Scale = new Vect2(scaleX / _superSample, scaleY / _superSample);
                break;

            case WindowScaleMode.PixelPerfect:
                baseScale = MathF.Max(1f, MathF.Floor(MathF.Min(scaleX, scaleY)));
                _renderSprite.Scale = new Vect2(baseScale / _superSample, baseScale / _superSample);
                break;

            case WindowScaleMode.Fit:
                baseScale = MathF.Min(scaleX, scaleY);
                _renderSprite.Scale = new Vect2(baseScale / _superSample, baseScale / _superSample);
                break;

            case WindowScaleMode.Fill:
                baseScale = MathF.Max(scaleX, scaleY);
                _renderSprite.Scale = new Vect2(baseScale / _superSample, baseScale / _superSample);
                break;

            case WindowScaleMode.None:
                _renderSprite.Scale = new Vect2(1f / _superSample, 1f / _superSample);
                break;
        }

        // Calculate position to center the scaled sprite
        Vect2 scaledSize = viewportSize * _renderSprite.Scale * _superSample;
        _renderSprite.Position = (_windowSize - scaledSize) / 2f;

        _window.Clear(SFColor.Black);
        _window.Draw(_renderSprite);
        _window.Display();
    }

    public void Close()
    {
        _window?.Close();
    }

    private void CreateWindow(int width, int height, string title, WindowMode mode)
    {
        Logger.Instance.DebugWithCategory("Window", "Creating SFML window with styles for {0}", mode);

        var styles = mode switch
        {
            WindowMode.Windowed => SFStyles.Close | SFStyles.Titlebar | SFStyles.Resize,
            WindowMode.Borderless => SFStyles.Close,
            WindowMode.Fullscreen => SFStyles.Close,
            _ => SFStyles.Close | SFStyles.Titlebar | SFStyles.Resize
        };
        var state = mode switch
        {
            WindowMode.Fullscreen => SFState.Fullscreen,
            _ => SFState.Windowed
        };

        var video = new SFVideoMode(new((uint)width, (uint)height));
        var context = new SFContextSettings { MajorVersion = 4, MinorVersion = 0 };

        _window = new SFRenderWindow(video, title, styles, state, context);

        Logger.Instance.DebugWithCategory("Window", "SFML window created");
    }

    private void RecreateWindow(int width, int height, string title, WindowMode mode)
    {
        Logger.Instance.InfoWithCategory("Window", "Recreating window: {0}x{1} '{2}' Mode={3}",
            width, height, title, mode);

        _window.Closed -= OnWindowClosedHandler;
        _window.GainedFocus -= OnFocusGainedHandler;
        _window.LostFocus -= OnFocusLostHandler;
        _window.Resized -= OnResizedHandler;
        _window.MouseWheelScrolled -= OnMouseWheelHandler;

        _window.Dispose();
        CreateWindow(width, height, title, mode);

        _window.Closed += OnWindowClosedHandler;
        _window.GainedFocus += OnFocusGainedHandler;
        _window.LostFocus += OnFocusLostHandler;
        _window.Resized += OnResizedHandler;
        _window.MouseWheelScrolled += OnMouseWheelHandler;

        RecreateRenderTarget();

        Logger.Instance.InfoWithCategory("Window", "Window recreated");
    }

    private void RecreateRenderTarget()
    {
        Logger.Instance.DebugWithCategory("Window", "Recreating render target with supersampling");

        _renderTexture?.Dispose();
        _renderSprite?.Dispose();

        // Use supersampled resolution for smooth rendering
        Vect2 renderSize = GameSettings.Instance.Viewport * _superSample;
        _renderTexture = new SFRenderTexture(new((uint)renderSize.X, (uint)renderSize.Y));
        _renderSprite = new SFSprite(_renderTexture.Texture);

        // Set the view to viewport coordinates (logical)
        var renderView = new SFView(new SFFloatRect(Vect2.Zero, GameSettings.Instance.Viewport));
        _renderTexture.SetView(renderView);

        _renderSize = renderSize;
    }

    private void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        Logger.Instance.InfoWithCategory("Window", "Window resized to {0}x{1}", width, height);

        _appliedWidth = width;
        _appliedHeight = height;
        _pendingWidth = width;
        _pendingHeight = height;

        _windowSize = new Vect2(width, height);

        var windowView = new SFView(new SFFloatRect(Vect2.Zero, _windowSize));
        _window.SetView(windowView);

        OnWindowResized?.Invoke(_windowSize);
    }

    private void OnWindowClosedHandler(object sender, EventArgs e)
    {
        OnWindowClosed?.Invoke();
        _window.Close();
    }

    private void OnFocusGainedHandler(object sender, EventArgs e)
    {
        IsFocused = true;
        OnFocusGained?.Invoke();
    }

    private void OnFocusLostHandler(object sender, EventArgs e)
    {
        IsFocused = false;
        OnFocusLost?.Invoke();
    }

    private void OnResizedHandler(object sender, SFSizeEventArgs e)
    {
        HandleResize((int)e.Size.X, (int)e.Size.Y);
    }

    private void OnMouseWheelHandler(object sender, SFMouseWheelScrollEventArgs e)
    {
        OnMouseWheelScrolled?.Invoke((int)e.Delta);
    }

    public static Vect2 GetDesktopResolution()
    {
        var mode = SFVideoMode.DesktopMode;
        return mode.Size;
    }

    public static List<Vect2> GetSupportedResolutions()
    {
        var result = new List<Vect2>();
        var modes = SFVideoMode.FullscreenModes;

        foreach (var mode in modes)
        {
            var size = mode.Size;
            if (!result.Contains(size))
                result.Add(size);
        }

        return result;
    }

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
    {
        var mode = new SFVideoMode(new((uint)width, (uint)height));
        return mode.IsValid();
    }

    public static implicit operator SFRenderWindow(Window v) => v._window;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        Logger.Instance.InfoWithCategory("Window", "Disposing window");

        _renderSprite?.Dispose();
        _renderTexture?.Dispose();
        _window?.Dispose();

        Logger.Instance.InfoWithCategory("Window", "Window disposed");
        _isDisposed = true;
    }
}