using Void.Engine.Graphics.Rendering;
using Void.Engine.Systems;

// ============================================================================
//  GameSettings.cs
// ============================================================================
//  Fluent configuration for a VOID application. Configure the singleton, then
//  call Build() before creating the Game instance.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine;

/// <summary>
/// Configures a VOID application before the game is created.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Instance"/> to configure the engine with the fluent setters,
/// then call <see cref="Build"/> to validate required values and apply defaults.
/// A finalized settings instance is required by <see cref="Game.Game(GameSettings)"/>.
/// </para>
/// <example>
/// <code>
/// var settings = GameSettings.Instance
///     .SetAppCompany("MyStudio")
///     .SetAppName("MyGame")
///     .SetWindow(1280, 720)
///     .SetViewport(320, 180)
///     .SetVsync(true)
///     .Build();
///
/// using var game = new Game(settings);
/// game.Run();
/// </code>
/// </example>
/// </remarks>
public sealed class GameSettings
{
    private static readonly Lazy<GameSettings> _instance = new(() => new GameSettings());
    private bool _isFixedTimeStepSet, _ignoreInputSet, _isFullscreenSet,
        _isVSyncSet, _useApplicationDataSet, _setLogMinLevel,
        _setWindowScaleMode, _setDefaultSortMode, _setOpenGLVersion;

    /// <summary>
    /// Gets the process-wide settings instance used to configure VOID.
    /// </summary>
    public static GameSettings Instance => _instance.Value;

    /// <summary>
    /// Gets whether <see cref="Build"/> has finalized the settings.
    /// </summary>
    public bool Initialized { get; private set; }

    private GameSettings() { }

    #region Application Data

    /// <summary>
    /// Chooses whether application files are stored in the platform application-data location.
    /// </summary>
    /// <param name="value"><see langword="true"/> to use application data; otherwise, use a local game directory.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetUseApplicationData(bool value)
    {
        _useApplicationDataSet = true;
        UseApplicationData = value;
        return this;
    }

    /// <summary>
    /// Gets whether application files use the platform application-data location.
    /// </summary>
    public bool UseApplicationData { get; private set; }

    /// <summary>
    /// Sets the application name used by VOID for application data and identification.
    /// </summary>
    /// <param name="name">The non-empty application name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppName(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppName = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the configured application name.
    /// </summary>
    public string AppName { get; private set; }

    /// <summary>
    /// Sets the company or studio name used for application data.
    /// </summary>
    /// <param name="name">The non-empty company or studio name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppCompany(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppCompany = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the configured company or studio name.
    /// </summary>
    public string AppCompany { get; private set; }

    /// <summary>
    /// Sets the title displayed by the game window.
    /// </summary>
    /// <param name="name">The non-empty window title.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppTitle(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTitle = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the configured window title.
    /// </summary>
    public string AppTitle { get; private set; }

    /// <summary>
    /// Sets the application log-directory name.
    /// </summary>
    /// <param name="name">The non-empty directory name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppLogFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppLogFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the application log-directory name.
    /// </summary>
    public string AppLogFolder { get; private set; }

    /// <summary>
    /// Sets the application save-data directory name.
    /// </summary>
    /// <param name="name">The non-empty directory name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppSaveFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppSaveFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the application save-data directory name.
    /// </summary>
    public string AppSaveFolder { get; private set; }

    /// <summary>
    /// Sets the application configuration-directory name.
    /// </summary>
    /// <param name="name">The non-empty directory name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppConfigFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppConfigFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the application configuration-directory name.
    /// </summary>
    public string AppConfigFolder { get; private set; }

    /// <summary>
    /// Sets the application temporary-data directory name.
    /// </summary>
    /// <param name="name">The non-empty directory name.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public GameSettings SetAppTempFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTempFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the application temporary-data directory name.
    /// </summary>
    public string AppTempFolder { get; private set; }

    /// <summary>
    /// Sets the root directory used for game content.
    /// </summary>
    /// <param name="path">An existing content directory.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="path"/> does not exist.</exception>
    public GameSettings SetContentRoot(string path)
    {
        if (path.IsEmpty())
            throw new ArgumentNullException(nameof(path), "path cannot be null or empty");
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"The specified content root directory does not exist: '{path}'.");

        AppContentRoot = path;
        return this;
    }

    /// <summary>
    /// Gets the configured game content root.
    /// </summary>
    public string AppContentRoot { get; private set; }

    /// <summary>
    /// Sets the application version.
    /// </summary>
    /// <param name="major">The major version. Must be greater than zero.</param>
    /// <param name="minor">The minor version.</param>
    /// <param name="rebuild">The build component.</param>
    /// <param name="revision">The revision component.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="major"/> is zero.</exception>
    public GameSettings SetAppVersion(uint major, uint minor = 0, uint rebuild = 0, uint revision = 0)
    {
        if (major == 0)
            throw new ArgumentOutOfRangeException(nameof(major), "Major version must be greater than zero");

        AppVersion = new Version((int)major, (int)minor, (int)rebuild, (int)revision).ToString();
        return this;
    }

    /// <summary>
    /// Gets the configured application version string.
    /// </summary>
    public string AppVersion { get; private set; }

    /// <summary>
    /// Gets a stable hexadecimal hash derived from <see cref="AppVersion"/>.
    /// </summary>
    public string AppVersionHash => $"{HashHelper.Cache64(AppVersion):X8}";

    #endregion

    #region Window & Viewport

    /// <summary>
    /// Enables or disables fullscreen startup.
    /// </summary>
    /// <param name="value"><see langword="true"/> to start fullscreen.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetFullScreen(bool value)
    {
        _isFullscreenSet = true;
        Fullscreen = value;
        return this;
    }

    /// <summary>
    /// Gets whether the game starts in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; private set; }

    /// <summary>
    /// Configures desktop fullscreen and enables fullscreen startup.
    /// </summary>
    /// <returns>This settings instance.</returns>
    /// <remarks>
    /// Desktop fullscreen uses the selected display's current desktop resolution
    /// and refresh rate.
    /// </remarks>
    public GameSettings SetDesktopFullscreen()
    {
        _isFullscreenSet = true;
        Fullscreen = true;
        FullscreenStyle = FullscreenStyle.Desktop;
        FullscreenWidth = 0;
        FullscreenHeight = 0;
        FullscreenRefreshRate = 0f;
        return this;
    }

    /// <summary>
    /// Configures exclusive fullscreen and enables fullscreen startup.
    /// </summary>
    /// <param name="width">The requested fullscreen width in pixels.</param>
    /// <param name="height">The requested fullscreen height in pixels.</param>
    /// <param name="refreshRate">The requested refresh rate in Hz, or zero to let SDL choose.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a zero size or negative refresh rate.</exception>
    public GameSettings SetFullscreenMode(uint width, uint height, float refreshRate = 0f)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Fullscreen width must be greater than zero.");
        if (height == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Fullscreen height must be greater than zero.");
        if (refreshRate < 0f)
            throw new ArgumentOutOfRangeException(nameof(refreshRate), "Refresh rate cannot be negative.");

        _isFullscreenSet = true;
        Fullscreen = true;
        FullscreenStyle = FullscreenStyle.Exclusive;
        FullscreenWidth = checked((int)width);
        FullscreenHeight = checked((int)height);
        FullscreenRefreshRate = refreshRate;
        return this;
    }

    /// <summary>
    /// Gets the configured fullscreen style.
    /// </summary>
    public FullscreenStyle FullscreenStyle { get; private set; }

    /// <summary>
    /// Gets the exclusive fullscreen width, or zero when desktop fullscreen is used.
    /// </summary>
    public int FullscreenWidth { get; private set; }

    /// <summary>
    /// Gets the exclusive fullscreen height, or zero when desktop fullscreen is used.
    /// </summary>
    public int FullscreenHeight { get; private set; }

    /// <summary>
    /// Gets the requested exclusive fullscreen refresh rate in Hz, or zero when SDL should choose.
    /// </summary>
    public float FullscreenRefreshRate { get; private set; }

    /// <summary>
    /// Enables or disables vertical synchronization.
    /// </summary>
    /// <param name="value"><see langword="true"/> to enable VSync.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetVsync(bool value)
    {
        _isVSyncSet = true;
        VSync = value;
        return this;
    }

    /// <summary>
    /// Gets whether VSync is enabled.
    /// </summary>
    public bool VSync { get; private set; }

    /// <summary>
    /// Sets the initial window size.
    /// </summary>
    /// <param name="width">The window width in pixels.</param>
    /// <param name="height">The window height in pixels.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either dimension is zero.</exception>
    public GameSettings SetWindow(uint width, uint height)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width is zero");
        if (height == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height is zero");

        Window = new Vect2(width, height);
        return this;
    }

    /// <summary>
    /// Gets the initial window size in pixels.
    /// </summary>
    public Vect2 Window { get; private set; }

    /// <summary>
    /// Selects the initial display by its zero-based enumeration index.
    /// </summary>
    /// <param name="displayIndex">The display index.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="displayIndex"/> is negative.</exception>
    public GameSettings SetDisplay(int displayIndex)
    {
        if (displayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(displayIndex), "Display index cannot be negative.");

        DisplayIndex = displayIndex;
        return this;
    }

    /// <summary>
    /// Gets the configured initial display index.
    /// </summary>
    public int DisplayIndex { get; private set; }

    /// <summary>
    /// Sets the preferred Linux window-system backend.
    /// </summary>
    /// <param name="backend">The Linux backend preference.</param>
    /// <returns>This settings instance.</returns>
    /// <remarks>
    /// This setting applies only on Linux. Other platforms ignore it.
    /// </remarks>
    public GameSettings SetLinuxWindowBackend(LinuxWindowBackend backend)
    {
        LinuxWindowBackend = backend;
        return this;
    }

    /// <summary>
    /// Gets the configured Linux window-system preference.
    /// </summary>
    public LinuxWindowBackend LinuxWindowBackend { get; private set; }

    /// <summary>
    /// Sets the internal render resolution used by the game.
    /// </summary>
    /// <param name="width">The viewport width in pixels.</param>
    /// <param name="height">The viewport height in pixels.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either dimension is zero.</exception>
    public GameSettings SetViewport(uint width, uint height)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width is zero");
        if (height == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height is zero");

        Viewport = new Vect2(width, height);
        return this;
    }

    /// <summary>
    /// Gets the internal render resolution.
    /// </summary>
    public Vect2 Viewport { get; private set; }

    #endregion

    #region Frame Timing

    /// <summary>
    /// Enables or disables fixed-timestep updates.
    /// </summary>
    /// <param name="value"><see langword="true"/> to use fixed updates.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetFixedTimeStep(bool value)
    {
        _isFixedTimeStepSet = true;
        IsFixedTimeStep = value;
        return this;
    }

    /// <summary>
    /// Gets whether fixed-timestep updates are enabled.
    /// </summary>
    public bool IsFixedTimeStep { get; private set; }

    /// <summary>
    /// Sets the target fixed-update interval in seconds.
    /// </summary>
    /// <param name="seconds">The positive target interval.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="seconds"/> is not positive.</exception>
    public GameSettings SetTargetElapsedTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Target elapsed time must be greater than zero");

        TargetElapsedTime = seconds;
        return this;
    }

    /// <summary>
    /// Sets the target fixed-update rate in frames per second.
    /// </summary>
    /// <param name="fps">The positive target rate.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fps"/> is not positive.</exception>
    public GameSettings SetTargetFPS(float fps)
    {
        if (fps <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero");

        TargetElapsedTime = 1f / fps;
        return this;
    }

    /// <summary>
    /// Gets the target fixed-update interval in seconds.
    /// </summary>
    public float TargetElapsedTime { get; private set; }

    /// <summary>
    /// Sets the maximum raw frame delta accepted by the timing system.
    /// </summary>
    /// <param name="seconds">The positive maximum delta in seconds.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="seconds"/> is not positive.</exception>
    public GameSettings SetMaxDeltaTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Max delta time must be greater than zero");

        MaxDeltaTime = seconds;
        return this;
    }

    /// <summary>
    /// Gets the maximum raw frame delta in seconds.
    /// </summary>
    public float MaxDeltaTime { get; private set; }

    #endregion

    #region Renderer

    /// <summary>
    /// Registers a factory that creates the renderer backend used by VOID.
    /// </summary>
    /// <param name="rendererFactory">A factory that returns a new renderer backend.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rendererFactory"/> is null.</exception>
    /// <remarks>
    /// When no custom renderer is registered, VOID creates its built-in OpenGL backend.
    /// The custom backend must implement the public renderer contracts in
    /// <c>Void.Engine.Graphics.Rendering</c>.
    /// </remarks>
    public GameSettings SetRenderer(Func<IRendererBackend> rendererFactory)
    {
        RendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
        return this;
    }

    /// <summary>
    /// Registers a renderer backend type with a public parameterless constructor.
    /// </summary>
    /// <typeparam name="T">The renderer backend type.</typeparam>
    /// <returns>This settings instance.</returns>
    public GameSettings SetRenderer<T>() where T : IRendererBackend, new()
        => SetRenderer(() => new T());

    /// <summary>
    /// Gets the custom renderer factory, or null to use VOID's built-in OpenGL backend.
    /// </summary>
    public Func<IRendererBackend> RendererFactory { get; private set; }

    /// <summary>
    /// Sets the OpenGL context version requested by VOID's built-in OpenGL backend.
    /// </summary>
    /// <param name="major">The non-zero major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="major"/> is zero.</exception>
    /// <remarks>
    /// Custom renderer backends can ignore this OpenGL-specific setting.
    /// </remarks>
    public GameSettings SetOpenGLVersion(uint major, uint minor)
    {
        if (major == 0)
            throw new ArgumentOutOfRangeException(nameof(major), "OpenGL major version must be greater than zero.");

        _setOpenGLVersion = true;
        OpenGLVersion = new GraphicsVersion(major, minor);
        return this;
    }

    /// <summary>
    /// Gets the OpenGL context version requested for the built-in backend.
    /// </summary>
    public GraphicsVersion OpenGLVersion { get; private set; }

    #endregion

    #region Graphics

    /// <summary>
    /// Sets the frame clear color from RGB components.
    /// </summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetClearColor(uint red, uint green, uint blue)
    {
        ClearColor = new Color((byte)red, (byte)green, (byte)blue);
        return this;
    }

    /// <summary>
    /// Sets the frame clear color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetClearColor(Color color)
        => SetClearColor(color.R, color.G, color.B);

    /// <summary>
    /// Sets the frame clear color from a hexadecimal color string.
    /// </summary>
    /// <param name="hex">A color string accepted by <see cref="Color"/>.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetClearColor(string hex)
    {
        var c = new Color(hex);
        SetClearColor(c.R, c.G, c.B);
        return this;
    }

    /// <summary>
    /// Gets the color used to clear the main game render target each frame.
    /// </summary>
    public Color ClearColor { get; private set; }

    /// <summary>
    /// Enables or disables the half-texel adjustment used by sprite UV generation.
    /// </summary>
    /// <param name="value"><see langword="true"/> to enable the adjustment.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetHalfTexelOffset(bool value)
    {
        UseHalfTexelOffset = value;
        return this;
    }

    /// <summary>
    /// Gets whether the sprite half-texel adjustment is enabled.
    /// </summary>
    public bool UseHalfTexelOffset { get; private set; }

    /// <summary>
    /// Sets the supersampling multiplier used for the internal render target.
    /// </summary>
    /// <param name="value">A value from 1 through 16.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside 1 through 16.</exception>
    public GameSettings SetSuperSample(uint value)
    {
        if (value == 0 || value > 16)
            throw new ArgumentOutOfRangeException(nameof(value), "SuperSample must be between 1 and 16");

        SuperSample = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the supersampling multiplier.
    /// </summary>
    public int SuperSample { get; private set; }

    /// <summary>
    /// Sets how the internal viewport is scaled into the native window.
    /// </summary>
    /// <param name="mode">The presentation scaling mode.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetWindowScaleMode(WindowScaleMode mode)
    {
        _setWindowScaleMode = true;
        WindowScaleMode = mode;
        return this;
    }

    /// <summary>
    /// Gets the configured viewport-to-window scaling mode.
    /// </summary>
    public WindowScaleMode WindowScaleMode { get; private set; }

    #endregion

    #region Atlas

    /// <summary>
    /// Sets the atlas fragmentation threshold that can trigger defragmentation.
    /// </summary>
    /// <param name="value">A value from 0.05 through 0.80.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the supported range.</exception>
    /// <remarks>
    /// The threshold is compared with <see cref="IAtlasPacker.Fragmentation"/> after
    /// a normal packing attempt cannot find room. Higher values tolerate more
    /// fragmented free space before VOID asks the packer to defragment.
    /// </remarks>
    public GameSettings SetAtlasDefragThreshold(float value)
    {
        if (value < 0.05f || value > 0.80f)
            throw new ArgumentOutOfRangeException(nameof(value), "Threshold must be between 5% and 80%");

        AtlasDefragThreshold = value;
        return this;
    }

    /// <summary>
    /// Gets the atlas fragmentation threshold used to trigger defragmentation.
    /// </summary>
    public float AtlasDefragThreshold { get; private set; }

    /// <summary>
    /// Sets the width and height of each square atlas page.
    /// </summary>
    /// <param name="value">The non-zero page size in pixels.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero.</exception>
    public GameSettings SetAtlasPageSize(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageSize = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the size of each square atlas page in pixels.
    /// </summary>
    public int AtlasPageSize { get; private set; }

    /// <summary>
    /// Sets the maximum number of atlas pages managed by the atlas system.
    /// </summary>
    /// <param name="value">The non-zero page count.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero.</exception>
    public GameSettings SetAtlasPageCount(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageCount = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the configured atlas page count.
    /// </summary>
    public int AtlasPageCount { get; private set; }

    /// <summary>
    /// Sets the rectangle-packing implementation used for atlas pages.
    /// </summary>
    /// <param name="packerType">
    /// A concrete <see cref="IAtlasPacker"/> implementation with a public
    /// constructor whose parameters are <c>int</c> width and <c>int</c> height.
    /// </param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="packerType"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="packerType"/> does not implement <see cref="IAtlasPacker"/>,
    /// is abstract or an interface, contains unbound generic parameters, or does not expose
    /// the public constructor required by VOID.
    /// </exception>
    /// <remarks>
    /// VOID creates one independent packer for each atlas page by invoking
    /// <c>new PackerType(pageWidth, pageHeight)</c>. Constructor validation occurs
    /// when this setting is assigned so configuration errors are reported before
    /// atlas initialization.
    /// </remarks>
    public GameSettings SetAtlasPacker(Type packerType)
    {
        ArgumentNullException.ThrowIfNull(packerType);

        if (!typeof(IAtlasPacker).IsAssignableFrom(packerType))
        {
            throw new ArgumentException(
                $"Type '{packerType.FullName ?? packerType.Name}' must implement IAtlasPacker.",
                nameof(packerType));
        }

        if (packerType.IsInterface || packerType.IsAbstract)
        {
            throw new ArgumentException(
                $"Atlas packer type '{packerType.FullName ?? packerType.Name}' must be concrete.",
                nameof(packerType));
        }

        if (packerType.ContainsGenericParameters)
        {
            throw new ArgumentException(
                $"Atlas packer type '{packerType.FullName ?? packerType.Name}' cannot contain unbound generic parameters.",
                nameof(packerType));
        }

        if (packerType.GetConstructor([typeof(int), typeof(int)]) == null)
        {
            throw new ArgumentException(
                $"Atlas packer type '{packerType.FullName ?? packerType.Name}' must expose a public constructor with signature (int width, int height).",
                nameof(packerType));
        }

        AtlasPacker = packerType;
        return this;
    }

    /// <summary>
    /// Gets the atlas packer type.
    /// </summary>
    public Type AtlasPacker { get; private set; }

    /// <summary>
    /// Sets the maximum number of queued atlas defragmentation moves processed per frame.
    /// </summary>
    /// <param name="value">A value from 1 through 100.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero or greater than 100.</exception>
    /// <remarks>
    /// Lower values spread defragmentation work over more frames. Higher values
    /// finish the operation sooner but can increase frame-time spikes.
    /// </remarks>
    public GameSettings SetAtlasDefragMovesPerFrame(uint value)
    {
        const uint MaxDefragMovesPerFrame = 100;

        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Value must be greater than zero. Default is 10.");

        if (value > MaxDefragMovesPerFrame)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Value cannot exceed {MaxDefragMovesPerFrame}. Recommended range is 1-50.");

        AtlasDefragMovesPerFrame = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the maximum number of atlas defragmentation moves processed per frame.
    /// </summary>
    public int AtlasDefragMovesPerFrame { get; private set; }

    #endregion

    #region Asset Management

    /// <summary>
    /// Sets how often the asset manager checks for expired assets.
    /// </summary>
    /// <param name="minutes">A value from 1 through 60 minutes.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minutes"/> is outside the supported range.</exception>
    public GameSettings SetAssetCheckIntervalMinutes(uint minutes)
    {
        const uint MinCheckInterval = 1;
        const uint MaxCheckInterval = 60;

        if (minutes < MinCheckInterval)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes,
                $"Check interval must be at least {MinCheckInterval} minute.");

        if (minutes > MaxCheckInterval)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes,
                $"Check interval cannot exceed {MaxCheckInterval} minutes.");

        AssetCheckIntervalMinutes = (int)minutes;
        return this;
    }

    /// <summary>
    /// Gets the interval, in minutes, between asset-expiration checks.
    /// </summary>
    public int AssetCheckIntervalMinutes { get; private set; }

    /// <summary>
    /// Sets how long an unused asset can remain idle before eviction.
    /// </summary>
    /// <param name="minutes">A value from 15 through 240 minutes.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minutes"/> is outside the supported range.</exception>
    public GameSettings SetAssetEviction(uint minutes)
    {
        const uint MinEvictionMinutes = 15;
        const uint MaxEvictionMinutes = 240;

        if (minutes < MinEvictionMinutes)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes,
                $"Eviction minutes must be at least {MinEvictionMinutes} to avoid aggressive eviction.");

        if (minutes > MaxEvictionMinutes)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes,
                $"Eviction minutes cannot exceed {MaxEvictionMinutes}. Values above this are excessive and may cause memory bloat.");

        AssetEvictionMinutes = (int)minutes;
        return this;
    }

    /// <summary>
    /// Gets the idle time, in minutes, before an asset can be evicted.
    /// </summary>
    public int AssetEvictionMinutes { get; private set; }

    #endregion

    #region Batch Rendering

    /// <summary>
    /// Sets the initial command capacity of newly created sprite batchers.
    /// </summary>
    /// <param name="value">The non-zero command capacity.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero.</exception>
    public GameSettings SetSpriteBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        SpriteBatchCapacity = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the initial sprite-batcher command capacity.
    /// </summary>
    public int SpriteBatchCapacity { get; private set; }

    /// <summary>
    /// Sets the initial command capacity of newly created primitive batchers.
    /// </summary>
    /// <param name="value">The non-zero command capacity.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero.</exception>
    public GameSettings SetPrimitiveBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        PrimitiveBatchCapacity = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the initial primitive-batcher command capacity.
    /// </summary>
    public int PrimitiveBatchCapacity { get; private set; }

    /// <summary>
    /// Enables or disables batch sorting.
    /// </summary>
    /// <param name="value"><see langword="true"/> to enable sorting.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetEnableBatchSorting(bool value)
    {
        EnableBatchSorting = value;
        return this;
    }

    /// <summary>
    /// Gets whether batch sorting is enabled.
    /// </summary>
    public bool EnableBatchSorting { get; private set; }

    /// <summary>
    /// Sets the default sort mode used by batchers when none is supplied to Begin.
    /// </summary>
    /// <param name="value">The default sort mode.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetDefaultSortMode(SortMode value)
    {
        _setDefaultSortMode = true;
        DefaultSortMode = value;
        return this;
    }

    /// <summary>
    /// Gets the default batch sort mode.
    /// </summary>
    public SortMode DefaultSortMode { get; private set; }

    /// <summary>
    /// Sets the default blend mode used by batchers when none is supplied to Begin.
    /// </summary>
    /// <param name="value">The default blend mode.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public GameSettings SetDefaultBlendMode(IBlendMode value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        DefaultBlendMode = value;
        return this;
    }

    /// <summary>
    /// Gets the default batch blend mode.
    /// </summary>
    public IBlendMode DefaultBlendMode { get; private set; }

    #endregion

    #region Discoverable

    /// <summary>
    /// Sets the assembly scanning policy used by the discoverable-type system.
    /// </summary>
    /// <param name="mode">The assembly scanning mode.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetDiscoverableScanMode(AssemblyScanMode mode)
    {
        DiscoverableScanMode = mode;
        return this;
    }

    /// <summary>
    /// Gets the assembly scanning policy used by the discoverable-type system.
    /// </summary>
    public AssemblyScanMode DiscoverableScanMode { get; private set; }

    /// <summary>
    /// Sets an additional predicate used to accept or reject assemblies during discovery.
    /// </summary>
    /// <param name="filter">The assembly filter.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
    public GameSettings SetDiscoverableAssemblyFilter(Func<Assembly, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        DiscoverableAssemblyFilter = filter;
        return this;
    }

    /// <summary>
    /// Gets the custom assembly filter used during discoverable-type scanning.
    /// </summary>
    public Func<Assembly, bool> DiscoverableAssemblyFilter { get; private set; }

    /// <summary>
    /// Adds an assembly name to the discoverable-type include set.
    /// </summary>
    /// <param name="assemblyName">The assembly name to include.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblyName"/> is null or empty.</exception>
    public GameSettings AddDiscoverableAssembly(string assemblyName)
    {
        if (assemblyName.IsEmpty())
            throw new ArgumentNullException(nameof(assemblyName));

        DiscoverableAssemblies.Add(assemblyName.Trim());
        return this;
    }

    /// <summary>
    /// Gets the assembly names explicitly included in discoverable-type scanning.
    /// </summary>
    public HashSet<string> DiscoverableAssemblies { get; } = [];

    /// <summary>
    /// Adds a namespace prefix that should be excluded from discoverable-type scanning.
    /// </summary>
    /// <param name="prefix">The namespace prefix to exclude.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is null or empty.</exception>
    public GameSettings AddDiscoverableExcludedPrefix(string prefix)
    {
        if (prefix.IsEmpty())
            throw new ArgumentNullException(nameof(prefix));

        DiscoverableExcludedPrefixes.Add(prefix.Trim());
        return this;
    }

    /// <summary>
    /// Gets the namespace prefixes excluded from discoverable-type scanning.
    /// </summary>
    public List<string> DiscoverableExcludedPrefixes { get; } = [];

    #endregion

    #region Input

    /// <summary>
    /// Sets the normalized gamepad axis dead zone.
    /// </summary>
    /// <param name="value">A value from 0 through 1.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside 0 through 1.</exception>
    public GameSettings SetDeadZone(float value)
    {
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), "Dead zone must be between 0 and 1.");

        DeadZone = value;
        return this;
    }

    /// <summary>
    /// Gets the normalized gamepad axis dead zone.
    /// </summary>
    public float DeadZone { get; private set; }

    /// <summary>
    /// Chooses whether game input is ignored while the window is unfocused.
    /// </summary>
    /// <param name="value"><see langword="true"/> to ignore input while unfocused.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetIgnoreInputWhenUnfocused(bool value)
    {
        _ignoreInputSet = true;
        IgnoreInputWhenUnfocused = value;
        return this;
    }

    /// <summary>
    /// Gets whether game input is ignored while the window is unfocused.
    /// </summary>
    public bool IgnoreInputWhenUnfocused { get; private set; }

    #endregion

    #region Logging

    /// <summary>
    /// Sets the minimum severity written by the logger.
    /// </summary>
    /// <param name="level">The minimum log level.</param>
    /// <returns>This settings instance.</returns>
    public GameSettings SetLogMinLevel(LogLevel level)
    {
        _setLogMinLevel = true;
        LogMinLevel = level;
        return this;
    }

    /// <summary>
    /// Gets the minimum configured log level.
    /// </summary>
    public LogLevel LogMinLevel { get; private set; }

    /// <summary>
    /// Sets the maximum size of one log file before rotation.
    /// </summary>
    /// <param name="size">The non-zero size in megabytes.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is zero.</exception>
    public GameSettings SetLogMaxFileSizeMB(uint size)
    {
        if (size == 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Log file size must be greater than zero");

        LogMaxFileSizeMB = size;
        return this;
    }

    /// <summary>
    /// Gets the maximum log file size in megabytes.
    /// </summary>
    public uint LogMaxFileSizeMB { get; private set; }

    /// <summary>
    /// Sets the maximum number of rotated log files retained by VOID.
    /// </summary>
    /// <param name="count">The non-zero file count.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is zero.</exception>
    public GameSettings SetLogMaxFiles(uint count)
    {
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Log file count must be greater than zero");

        LogMaxFiles = (int)count;
        return this;
    }

    /// <summary>
    /// Gets the maximum number of log files retained by VOID.
    /// </summary>
    public int LogMaxFiles { get; private set; }

    #endregion

    #region Trace

    /// <summary>
    /// Sets a callback invoked when VOID observes an unhandled exception.
    /// </summary>
    /// <param name="onCrash">The crash callback.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onCrash"/> is null.</exception>
    public GameSettings SetOnCrash(Action<Exception> onCrash)
    {
        OnCrash = onCrash ?? throw new ArgumentNullException(nameof(onCrash));
        return this;
    }

    /// <summary>
    /// Gets the callback invoked for unhandled exceptions.
    /// </summary>
    public Action<Exception> OnCrash { get; private set; }

    #endregion

    #region Sound

    /// <summary>
    /// Sets the maximum number of concurrent audio instances in the sound pool.
    /// </summary>
    /// <param name="value">A value from 32 through 512.</param>
    /// <returns>This settings instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside 32 through 512.</exception>
    public GameSettings SetAudioLimit(uint value)
    {
        if (value < 32)
            throw new ArgumentOutOfRangeException(nameof(value), "Audio limit must be at least 32.");
        if (value > 512)
            throw new ArgumentOutOfRangeException(nameof(value), "Audio limit cannot exceed 512.");

        AudioLimit = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the maximum number of concurrent audio instances.
    /// </summary>
    public int AudioLimit { get; private set; }

    #endregion

    /// <summary>
    /// Validates the configuration, applies defaults, and finalizes the settings.
    /// </summary>
    /// <returns>This finalized settings instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required application identity values have not been configured.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when no configured or conventional content directory can be found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Calling <see cref="Build"/> more than once returns the already finalized
    /// instance without applying the configuration a second time.
    /// </para>
    /// <para>
    /// When no content root is explicitly configured, VOID looks for a
    /// <c>Content</c> directory first and then an <c>Assets</c> directory.
    /// </para>
    /// </remarks>
    public GameSettings Build()
    {
        if (Initialized)
            return this;

        if (AppName.IsEmpty())
            throw new InvalidOperationException(
                "'SetAppName()' name not set. Required to start up the engine for application data."
            );

        if (AppCompany.IsEmpty())
            throw new InvalidOperationException(
                "SetAppCompany() name not set. Required to start up engine for application data."
            );

        if (AppContentRoot.IsEmpty())
        {
            if (Directory.Exists("Content"))
                AppContentRoot = "Content";
            else if (Directory.Exists("Assets"))
                AppContentRoot = "Assets";
            else
                throw new DirectoryNotFoundException(
                    "No content directory found. Expected to find either a 'Content' or 'Assets' folder."
                );
        }

        // Apply defaults that were not set explicitly by the game.
        AtlasPageSize = AtlasPageSize <= 0 ? 2048 : AtlasPageSize;
        AtlasPageCount = AtlasPageCount <= 0 ? 4 : AtlasPageCount;
        AtlasPacker ??= typeof(SkylinePacker);
        AppTitle = AppTitle.IsEmpty() ? "Game" : AppTitle;
        Window = Window.IsZero ? new Vect2(1280, 720) : Window;
        Viewport = Viewport.IsZero ? new Vect2(320, 180) : Viewport;
        ClearColor = ClearColor.IsEmpty ? new Color(100, 149, 237) : ClearColor;
        SpriteBatchCapacity = SpriteBatchCapacity <= 0 ? 1024 : SpriteBatchCapacity;
        PrimitiveBatchCapacity = PrimitiveBatchCapacity <= 0 ? 4096 : PrimitiveBatchCapacity;
        DefaultSortMode = !_setDefaultSortMode ? SortMode.BackToFront : DefaultSortMode;
        DefaultBlendMode ??= BlendMode.Alpha;
        IsFixedTimeStep = !_isFixedTimeStepSet || IsFixedTimeStep;
        TargetElapsedTime = TargetElapsedTime <= 0 ? 1f / 60f : TargetElapsedTime;
        MaxDeltaTime = MaxDeltaTime <= 0 ? 0.1f : MaxDeltaTime;
        DeadZone = DeadZone <= 0f ? 0.15f : DeadZone;
        IgnoreInputWhenUnfocused = !_ignoreInputSet || IgnoreInputWhenUnfocused;
        Fullscreen = _isFullscreenSet && Fullscreen;
        if (FullscreenStyle == FullscreenStyle.Exclusive &&
            (FullscreenWidth <= 0 || FullscreenHeight <= 0))
        {
            // Exclusive mode requires an explicit resolution. Public setters
            // normally guarantee this, so this is a final defensive fallback.
            FullscreenStyle = FullscreenStyle.Desktop;
            FullscreenWidth = 0;
            FullscreenHeight = 0;
            FullscreenRefreshRate = 0f;
        }
        VSync = !_isVSyncSet || VSync;
        UseApplicationData = _useApplicationDataSet && UseApplicationData;
        AppVersion = AppVersion.IsEmpty() ? "1.0.0.0" : AppVersion;
        AtlasDefragThreshold = AtlasDefragThreshold <= 0 ? 0.3f : AtlasDefragThreshold;
        DiscoverableScanMode = DiscoverableScanMode == default ? AssemblyScanMode.ExcludeFramework : DiscoverableScanMode;
        LogMinLevel = !_setLogMinLevel ? LogLevel.Info : LogMinLevel;
        LogMaxFileSizeMB = LogMaxFileSizeMB == 0 ? 10 : LogMaxFileSizeMB;
        LogMaxFiles = LogMaxFiles == 0 ? 10 : LogMaxFiles;
        SuperSample = SuperSample <= 0 ? 4 : SuperSample;
        WindowScaleMode = !_setWindowScaleMode ? WindowScaleMode.Fit : WindowScaleMode;
        OpenGLVersion = !_setOpenGLVersion ? new GraphicsVersion(3, 3) : OpenGLVersion;
        AppLogFolder = AppLogFolder.IsEmpty() ? "Logs" : AppLogFolder;
        AppSaveFolder = AppSaveFolder.IsEmpty() ? "Saves" : AppSaveFolder;
        AppConfigFolder = AppConfigFolder.IsEmpty() ? "Config" : AppConfigFolder;
        AppTempFolder = AppTempFolder.IsEmpty() ? "Temp" : AppTempFolder;
        AudioLimit = AudioLimit <= 0 ? 128 : AudioLimit;
        AtlasDefragMovesPerFrame = AtlasDefragMovesPerFrame <= 0 ? 10 : AtlasDefragMovesPerFrame;
        AssetEvictionMinutes = AssetEvictionMinutes <= 0 ? 30 : AssetEvictionMinutes;
        AssetCheckIntervalMinutes = AssetCheckIntervalMinutes <= 0 ? 1 : AssetCheckIntervalMinutes;

        Initialized = true;

        return this;
    }
}
