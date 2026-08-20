// ============================================================================
//  Window.cs
// ============================================================================
//  Game window management with support for multiple display modes, scaling,
//  supersampling, and deferred settings changes.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Defines how the viewport is scaled to fit the window.
/// </summary>
public enum WindowScaleMode
{
    /// <summary>
    /// Scales the viewport to fill the window exactly, which may distort the image if aspect ratios differ.
    /// </summary>
    Stretch,

    /// <summary>
    /// Scales the viewport up by the largest integer factor that fits, adding borders if needed for pixel-perfect rendering.
    /// </summary>
    PixelPerfect,

    /// <summary>
    /// Scales the viewport to fit entirely within the window while maintaining aspect ratio, adding black bars on the sides.
    /// </summary>
    Fit,

    /// <summary>
    /// Scales the viewport to fill the entire window while maintaining aspect ratio, cropping any overflow.
    /// </summary>
    Fill,

    /// <summary>
    /// Displays the viewport at its native resolution centered in the window without any scaling.
    /// </summary>
    None
}

/// <summary>
/// Represents a display mode with width, height, and bits per pixel.
/// </summary>
public readonly struct DisplayMode
{
    /// <summary>
    /// Gets the width of the display mode in pixels.
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Gets the height of the display mode in pixels.
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Gets the bits per pixel of the display mode.
    /// </summary>
    public uint BitsPerPixel { get; }

    internal DisplayMode(uint width, uint height, uint bitsPerPixel)
    {
        Width = width;
        Height = height;
        BitsPerPixel = bitsPerPixel;
    }

    /// <summary>
    /// Returns a string representation of the display mode.
    /// </summary>
    public override string ToString() => $"{Width}x{Height} @ {BitsPerPixel}bpp";
}

/// <summary>
/// Defines the window display mode.
/// </summary>
public enum WindowMode
{
    /// <summary>
    /// Standard window with title bar and borders.
    /// </summary>
    Windowed,

    /// <summary>
    /// Window without borders or title bar.
    /// </summary>
    Borderless,

    /// <summary>
    /// Fullscreen mode that takes over the entire display.
    /// </summary>
    Fullscreen
}

/// <summary>
/// Manages the game window, including creation, resizing, display modes, and rendering.
/// </summary>
/// <remarks>
/// <para>
/// <list type="bullet">
///   <item><description>Windowed, borderless, and fullscreen modes</description></item>
///   <item><description>Deferred settings changes with <see cref="ApplyChanges"/></description></item>
///   <item><description>Supersampling for high-quality rendering</description></item>
///   <item><description>Multiple viewport scaling modes</description></item>
///   <item><description>Window resizing and event handling</description></item>
/// </list>
/// </para>
/// <para>
/// The window uses a deferred settings pattern where changes to size, mode,
/// VSync, or title are stored as pending changes until <see cref="ApplyChanges"/>
/// is called. This allows multiple settings to be changed atomically.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var window = new Window(1280, 720, "My Game", WindowMode.Windowed, true);
/// 
/// // Defer settings changes
/// window.SetSize(1920, 1080)
///       .SetMode(WindowMode.Fullscreen)
///       .ApplyChanges();
/// 
/// // Render loop
/// while (window.IsOpen)
/// {
///     window.DispatchEvents();
///     window.BeginRender(Color.CornflowerBlue);
///     // Draw content here
///     window.EndRender();
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class Window : IDisposable
{
    internal SFRenderWindow _window;
    internal SFRenderTexture _renderTexture;
    internal SFSprite _renderSprite;

    private int _pendingWidth;
    private int _pendingHeight;
    private WindowMode _pendingMode;
    private bool _pendingVSync;
    private string _pendingTitle;
    private bool _hasPendingChanges;

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

    /// <summary>
    /// Gets the current window size in pixels.
    /// </summary>
    public Vect2 WindowSize => _windowSize;

    /// <summary>
    /// Gets the render target size in pixels (viewport size multiplied by supersampling).
    /// </summary>
    public Vect2 RenderSize => _renderSize;

    /// <summary>
    /// Gets the current window mode.
    /// </summary>
    public WindowMode Mode => _appliedMode;

    /// <summary>
    /// Gets whether VSync is currently enabled.
    /// </summary>
    public bool VSyncEnabled => _appliedVSync;

    /// <summary>
    /// Gets whether the window currently has focus.
    /// </summary>
    public bool IsFocused { get; private set; }

    /// <summary>
    /// Gets whether the window is open.
    /// </summary>
    public bool IsOpen => _window?.IsOpen ?? false;

    /// <summary>
    /// Gets whether there are pending changes that need to be applied.
    /// </summary>
    public bool HasPendingChanges => _hasPendingChanges;

    /// <summary>
    /// Called when the window is resized.
    /// </summary>
    public Action<Vect2> OnWindowResized { get; set; }

    /// <summary>
    /// Called when the window gains focus.
    /// </summary>
    public Action OnFocusGained { get; set; }

    /// <summary>
    /// Called when the window loses focus.
    /// </summary>
    public Action OnFocusLost { get; set; }

    /// <summary>
    /// Called when the window is closed.
    /// </summary>
    public Action OnWindowClosed { get; set; }

    /// <summary>
    /// Called when the mouse wheel is scrolled.
    /// </summary>
    public Action<int> OnMouseWheelScrolled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="width">The initial width of the window in pixels.</param>
    /// <param name="height">The initial height of the window in pixels.</param>
    /// <param name="title">The initial window title.</param>
    /// <param name="mode">The initial window mode.</param>
    /// <param name="vsync">Whether VSync should be enabled initially.</param>
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

    /// <summary>
    /// Sets the pending window width.
    /// </summary>
    public Window SetWidth(int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
        _pendingWidth = width;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets the pending window height.
    /// </summary>
    public Window SetHeight(int height)
    {
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");
        _pendingHeight = height;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets the pending window size.
    /// </summary>
    public Window SetSize(int width, int height)
    {
        SetWidth(width);
        SetHeight(height);
        return this;
    }

    /// <summary>
    /// Sets the pending window mode.
    /// </summary>
    public Window SetMode(WindowMode mode)
    {
        _pendingMode = mode;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets the pending VSync state.
    /// </summary>
    public Window SetVSync(bool enabled)
    {
        _pendingVSync = enabled;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets the pending window title.
    /// </summary>
    public Window SetTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            throw new ArgumentNullException(nameof(title));
        _pendingTitle = title;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Applies all pending changes to the window.
    /// </summary>
    /// <returns>This window instance for method chaining.</returns>
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

    /// <summary>
    /// Toggles between fullscreen and windowed mode.
    /// </summary>
    public Window ToggleFullscreen()
    {
        SetMode(_appliedMode == WindowMode.Fullscreen ? WindowMode.Windowed : WindowMode.Fullscreen);
        ApplyChanges();
        return this;
    }

    internal void DispatchEvents()
    {
        _window?.DispatchEvents();
    }

    internal void BeginRender(Color clearColor)
    {
        _renderTexture.Clear(clearColor);
    }

    internal void EndRender()
    {
        _renderTexture.Display();

        var windowView = new SFView(new SFFloatRect(Vect2.Zero, _windowSize));
        _window.SetView(windowView);

        Vect2 viewportSize = GameSettings.Instance.Viewport;
        float scaleX = _windowSize.X / viewportSize.X;
        float scaleY = _windowSize.Y / viewportSize.Y;

        float baseScale;
        switch (_scaleMode)
        {
            case WindowScaleMode.Stretch:
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

        Vect2 scaledSize = viewportSize * _renderSprite.Scale * _superSample;
        _renderSprite.Position = (_windowSize - scaledSize) / 2f;

        _window.Clear(SFColor.Black);
        _window.Draw(_renderSprite);
        _window.Display();
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
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

        Vect2 renderSize = GameSettings.Instance.Viewport * _superSample;
        _renderTexture = new SFRenderTexture(new((uint)renderSize.X, (uint)renderSize.Y));
        _renderSprite = new SFSprite(_renderTexture.Texture);

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

    /// <summary>
    /// Gets the desktop resolution.
    /// </summary>
    public static Vect2 GetDesktopResolution()
    {
        var mode = SFVideoMode.DesktopMode;
        return mode.Size;
    }

    /// <summary>
    /// Gets a list of supported resolutions for the current display.
    /// </summary>
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

    /// <summary>
    /// Gets a list of supported resolutions that match the specified aspect ratio.
    /// </summary>
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
    /// Gets the closest supported resolution to the specified dimensions.
    /// </summary>
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
    /// Determines whether the specified resolution is supported.
    /// </summary>
    public static bool IsResolutionSupported(int width, int height)
    {
        var mode = new SFVideoMode(new((uint)width, (uint)height));
        return mode.IsValid();
    }

    /// <summary>
    /// Implicitly converts a Window to an SFML render window.
    /// </summary>
    public static implicit operator SFRenderWindow(Window v) => v._window;

    /// <summary>
    /// Disposes of the window and all associated resources.
    /// </summary>
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