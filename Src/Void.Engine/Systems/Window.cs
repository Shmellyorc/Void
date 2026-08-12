

namespace Void.Engine.Systems;



/// <summary>
/// Represents a display mode/resolution.
/// </summary>
public readonly struct DisplayMode
{
    /// <summary>
    /// Gets the resolution width in pixels.
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Gets the resolution height in pixels.
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Gets the bits per pixel.
    /// </summary>
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
    /// <summary>
    /// Standard window with title bar and borders.
    /// </summary>
    Windowed,

    /// <summary>
    /// Borderless window that fills the screen.
    /// </summary>
    Borderless,

    /// <summary>
    /// Exclusive fullscreen mode.
    /// </summary>
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

    /// <summary>
    /// Gets the current window size in pixels.
    /// </summary>
    public Vect2 WindowSize => _windowSize;

    /// <summary>
    /// Gets the render resolution (may differ from window size if scaling is used).
    /// </summary>
    public Vect2 RenderSize => _renderSize;

    /// <summary>
    /// Gets the current applied window display mode.
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
    /// Gets whether the window is currently open.
    /// </summary>
    public bool IsOpen => _window?.IsOpen ?? false;

    /// <summary>
    /// Gets whether there are pending changes that have not been applied yet.
    /// </summary>
    public bool HasPendingChanges => _hasPendingChanges;

    /// <summary>
    /// Called when the window is resized. Args: new window size.
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
    /// Called when the window is closed by the user (X button).
    /// </summary>
    public Action OnWindowClosed { get; set; }

    /// <summary>
    /// Called when the mouse wheel is scrolled. Args: scroll delta.
    /// </summary>
    public Action<int> OnMouseWheelScrolled { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    public Window(int width, int height, string title, WindowMode mode = WindowMode.Windowed, bool vsync = true)
    {
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

        _windowSize = new Vect2(width, height);
        _renderSize = new Vect2(width, height);

        CreateWindow(width, height, title, mode);
        _window.SetVerticalSyncEnabled(vsync);

        _renderTexture = new SFRenderTexture((uint)width, (uint)height);
        _renderSprite = new SFSprite(_renderTexture.Texture);

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
            HandleResize((int)e.Width, (int)e.Height);
        };

        _window.MouseWheelScrolled += (sender, args) =>
        {
            OnMouseWheelScrolled?.Invoke((int)args.Delta);
        };
    }

    /// <summary>
    /// Sets the window width. Call <see cref="ApplyChanges"/> to apply.
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
    /// Sets the window height. Call <see cref="ApplyChanges"/> to apply.
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
    /// Sets the window size. Call <see cref="ApplyChanges"/> to apply.
    /// </summary>
    public Window SetSize(int width, int height)
    {
        SetWidth(width);
        SetHeight(height);
        return this;
    }

    /// <summary>
    /// Sets the window display mode. Call <see cref="ApplyChanges"/> to apply.
    /// </summary>
    public Window SetMode(WindowMode mode)
    {
        _pendingMode = mode;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets VSync. Call <see cref="ApplyChanges"/> to apply.
    /// </summary>
    public Window SetVSync(bool enabled)
    {
        _pendingVSync = enabled;
        _hasPendingChanges = true;
        return this;
    }

    /// <summary>
    /// Sets the window title. Call <see cref="ApplyChanges"/> to apply.
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
    /// Applies all pending window changes at once.
    /// Recreates the window if mode changes (borderless/fullscreen cannot be toggled at runtime).
    /// </summary>
    public Window ApplyChanges()
    {
        if (!_hasPendingChanges)
            return this;

        bool modeChanged = _pendingMode != _appliedMode;
        bool sizeChanged = _pendingWidth != _appliedWidth || _pendingHeight != _appliedHeight;

        // Mode changes require recreating the window
        if (modeChanged)
        {
            RecreateWindow(_pendingWidth, _pendingHeight, _pendingTitle, _pendingMode);
        }
        else
        {
            // Just apply size/title/vsync
            if (sizeChanged)
            {
                _window.Size = new SFVector2u((uint)_pendingWidth, (uint)_pendingHeight);
                RecreateRenderTarget(_pendingWidth, _pendingHeight);
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
        _renderSize = new Vect2(_appliedWidth, _appliedHeight);

        _hasPendingChanges = false;
        OnWindowResized?.Invoke(_windowSize);
        return this;
    }

    /// <summary>
    /// Convenience method to toggle fullscreen and apply immediately.
    /// </summary>
    public Window ToggleFullscreen()
    {
        SetMode(_appliedMode == WindowMode.Fullscreen ? WindowMode.Windowed : WindowMode.Fullscreen);
        ApplyChanges();
        return this;
    }

    /// <summary>
    /// Dispatches pending window events. Call once per frame.
    /// </summary>
    public void DispatchEvents()
    {
        _window?.DispatchEvents();
    }

    /// <summary>
    /// Begins rendering to the internal render texture.
    /// </summary>
    public void BeginRender(Color clearColor)
    {
        _renderTexture.Clear(clearColor);
    }

    /// <summary>
    /// Ends rendering and displays the render texture to the window.
    /// </summary>
    public void EndRender()
    {
        _renderTexture.Display();
        _window.Clear();
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
        var styles = mode switch
        {
            WindowMode.Windowed => SFStyles.Close | SFStyles.Titlebar | SFStyles.Resize,
            WindowMode.Borderless => SFStyles.Close,
            WindowMode.Fullscreen => SFStyles.Close,
            _ => SFStyles.Close | SFStyles.Titlebar | SFStyles.Resize
        };

        var video = new SFVideoMode((uint)width, (uint)height);
        var context = new SFContextSettings { MajorVersion = 4, MinorVersion = 0 };

        _window = new SFRenderWindow(video, title, styles, context);
    }

    private void RecreateWindow(int width, int height, string title, WindowMode mode)
    {
        // Unsubscribe events
        _window.Closed -= OnWindowClosedHandler;
        _window.GainedFocus -= OnFocusGainedHandler;
        _window.LostFocus -= OnFocusLostHandler;
        _window.Resized -= OnResizedHandler;
        _window.MouseWheelScrolled -= OnMouseWheelHandler;

        _window.Dispose();
        CreateWindow(width, height, title, mode);

        // Resubscribe events
        _window.Closed += OnWindowClosedHandler;
        _window.GainedFocus += OnFocusGainedHandler;
        _window.LostFocus += OnFocusLostHandler;
        _window.Resized += OnResizedHandler;
        _window.MouseWheelScrolled += OnMouseWheelHandler;

        RecreateRenderTarget(width, height);
    }

    private void RecreateRenderTarget(int width, int height)
    {
        _renderTexture?.Dispose();
        _renderSprite?.Dispose();
        _renderTexture = new SFRenderTexture((uint)width, (uint)height);
        _renderSprite = new SFSprite(_renderTexture.Texture);
    }

    private void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _appliedWidth = width;
        _appliedHeight = height;
        _pendingWidth = width;
        _pendingHeight = height;

        RecreateRenderTarget(width, height);

        _windowSize = new Vect2(width, height);
        _renderSize = new Vect2(width, height);

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
        HandleResize((int)e.Width, (int)e.Height);
    }

    private void OnMouseWheelHandler(object sender, SFMouseWheelScrollEventArgs e)
    {
        OnMouseWheelScrolled?.Invoke((int)e.Delta);
    }











    /// <summary>
    /// Gets the current desktop resolution.
    /// </summary>
    /// <returns>The desktop resolution as a <see cref="Vect2"/>.</returns>
    public static Vect2 GetDesktopResolution()
    {
        var mode = SFVideoMode.DesktopMode;
        return new Vect2(mode.Width, mode.Height);
    }

    /// <summary>
    /// Gets all supported fullscreen resolutions.
    /// </summary>
    /// <returns>A list of unique resolutions (width, height pairs).</returns>
    public static List<Vect2> GetSupportedResolutions()
    {
        var result = new List<Vect2>();
        var modes = SFVideoMode.FullscreenModes;

        foreach (var mode in modes)
        {
            var size = new Vect2(mode.Width, mode.Height);
            if (!result.Contains(size))
                result.Add(size);
        }

        return result;
    }

    /// <summary>
    /// Gets all supported fullscreen resolutions with the specified aspect ratio.
    /// </summary>
    /// <param name="ratioWidth">The width component of the aspect ratio (e.g., 16 for 16:9).</param>
    /// <param name="ratioHeight">The height component of the aspect ratio (e.g., 9 for 16:9).</param>
    /// <param name="tolerance">Tolerance for floating point comparison. Default is 0.01f.</param>
    /// <returns>A list of resolutions matching the aspect ratio.</returns>
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
    /// Gets the closest supported fullscreen resolution to the specified size.
    /// </summary>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    /// <returns>The closest supported resolution, or the target if none found.</returns>
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
    /// Checks if the specified resolution is supported for fullscreen mode.
    /// </summary>
    /// <param name="width">The width to check.</param>
    /// <param name="height">The height to check.</param>
    /// <returns>True if the resolution is supported.</returns>
    public static bool IsResolutionSupported(int width, int height)
    {
        var mode = new SFVideoMode((uint)width, (uint)height);
        return mode.IsValid();
    }











    public static implicit operator SFRenderWindow(Window v) => v._window;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _renderSprite?.Dispose();
        _renderTexture?.Dispose();
        _window?.Dispose();

        _isDisposed = true;
    }
}

