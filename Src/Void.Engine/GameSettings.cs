// ============================================================================
//  GameSettings.cs
// ============================================================================
//  Fluent configuration builder for the game engine. All settings are optional
//  with sensible defaults. Call Build() to finalize before creating a Game 
//  instance.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine;

/// <summary>
/// Fluent configuration builder for the game engine. Use the singleton instance
/// to chain configuration methods and call <see cref="Build"/> to finalize.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var settings = GameSettings.Instance
///     .SetAppCompany("MyStudio")
///     .SetAppName("MyGame")
///     .SetWindow(1920, 1080)
///     .SetFullScreen(true)
///     .Build();
/// 
/// using var game = new Game(settings);
/// game.Run();
/// </code>
/// </remarks>
public sealed class GameSettings
{
    private static readonly Lazy<GameSettings> _instance = new(() => new GameSettings());
    private bool _isFixedTimeStepSet, _ignoreInputSet, _isFullscreenSet,
        _isVSyncSet, _useApplicationDataSet, _setLogMinLevel,
        _setWindowScaleMode, _setDefaultSortMode;

    /// <summary>
    /// Gets the singleton settings instance.
    /// </summary>
    public static GameSettings Instance => _instance.Value;

    /// <summary>
    /// Returns true after <see cref="Build"/> has been called.
    /// </summary>
    public bool Initialized { get; private set; }

    private GameSettings() { }

    #region Application Data

    /// <summary>
    /// Sets whether to use the system's application data folder (e.g., %APPDATA%).
    /// Default is false (uses local folder).
    /// </summary>
    public GameSettings SetUseApplicationData(bool value)
    {
        _useApplicationDataSet = true;
        UseApplicationData = value;
        return this;
    }

    /// <summary>
    /// Gets whether the engine should use the system's application data folder.
    /// </summary>
    public bool UseApplicationData { get; private set; }

    /// <summary>
    /// Sets the application name. Required.
    /// </summary>
    public GameSettings SetAppName(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppName = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string AppName { get; private set; }

    /// <summary>
    /// Sets the company name. Required.
    /// </summary>
    public GameSettings SetAppCompany(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppCompany = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the company name.
    /// </summary>
    public string AppCompany { get; private set; }

    /// <summary>
    /// Sets the window title. Default is "Game".
    /// </summary>
    public GameSettings SetAppTitle(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTitle = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string AppTitle { get; private set; }

    /// <summary>
    /// Sets the log folder name. Default is "Logs".
    /// </summary>
    public GameSettings SetAppLogFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppLogFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the log folder name.
    /// </summary>
    public string AppLogFolder { get; private set; }

    /// <summary>
    /// Sets the save data folder name. Default is "Saves".
    /// </summary>
    public GameSettings SetAppSaveFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppSaveFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the save data folder name.
    /// </summary>
    public string AppSaveFolder { get; private set; }

    /// <summary>
    /// Sets the config folder name. Default is "Config".
    /// </summary>
    public GameSettings SetAppConfigFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppConfigFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the config folder name.
    /// </summary>
    public string AppConfigFolder { get; private set; }

    /// <summary>
    /// Sets the temp folder name. Default is "Temp".
    /// </summary>
    public GameSettings SetAppTempFolder(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTempFolder = name.Trim();
        return this;
    }

    /// <summary>
    /// Gets the temp folder name.
    /// </summary>
    public string AppTempFolder { get; private set; }

    /// <summary>
    /// Sets the content root directory. Defaults to "Content" or "Assets" if found.
    /// </summary>
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
    /// Gets the content root directory.
    /// </summary>
    public string AppContentRoot { get; private set; }

    /// <summary>
    /// Sets the application version. Default is "1.0.0.0".
    /// </summary>
    public GameSettings SetAppVersion(uint major, uint minor = 0, uint rebuild = 0, uint revision = 0)
    {
        if (major == 0)
            throw new ArgumentOutOfRangeException(nameof(major), "Major version must be greater than zero");

        AppVersion = new Version((int)major, (int)minor, (int)rebuild, (int)revision).ToString();
        return this;
    }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string AppVersion { get; private set; }

    /// <summary>
    /// Gets a hash of the version string for build verification.
    /// </summary>
    public string AppVersionHash => $"{HashHelper.Cache64(AppVersion):X8}";

    #endregion

    #region Window & Viewport

    /// <summary>
    /// Sets fullscreen mode. Default is false.
    /// </summary>
    public GameSettings SetFullScreen(bool value)
    {
        _isFullscreenSet = true;
        Fullscreen = value;
        return this;
    }

    /// <summary>
    /// Gets whether the game should run in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; private set; }

    /// <summary>
    /// Sets VSync. Default is true.
    /// </summary>
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
    /// Sets the window resolution. Default is 1280x720.
    /// </summary>
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
    /// Gets the window resolution.
    /// </summary>
    public Vect2 Window { get; private set; }

    /// <summary>
    /// Sets the internal render resolution. Default is 320x180.
    /// </summary>
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
    /// Sets fixed timestep mode. Default is true.
    /// </summary>
    public GameSettings SetFixedTimeStep(bool value)
    {
        _isFixedTimeStepSet = true;
        IsFixedTimeStep = value;
        return this;
    }

    /// <summary>
    /// Gets whether fixed timestep mode is enabled.
    /// </summary>
    public bool IsFixedTimeStep { get; private set; }

    /// <summary>
    /// Sets the target elapsed time in seconds. Default is 1/60.
    /// </summary>
    public GameSettings SetTargetElapsedTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Target elapsed time must be greater than zero");

        TargetElapsedTime = seconds;
        return this;
    }

    /// <summary>
    /// Sets the target FPS. Converts to elapsed time internally.
    /// </summary>
    public GameSettings SetTargetFPS(float fps)
    {
        if (fps <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero");

        TargetElapsedTime = 1f / fps;
        return this;
    }

    /// <summary>
    /// Gets the target elapsed time in seconds.
    /// </summary>
    public float TargetElapsedTime { get; private set; }

    /// <summary>
    /// Sets the maximum delta time to prevent spiral of death. Default is 0.1s.
    /// </summary>
    public GameSettings SetMaxDeltaTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Max delta time must be greater than zero");

        MaxDeltaTime = seconds;
        return this;
    }

    /// <summary>
    /// Gets the maximum delta time in seconds.
    /// </summary>
    public float MaxDeltaTime { get; private set; }

    #endregion

    #region Graphics

    /// <summary>
    /// Sets the clear color. Default is cornflower blue (100,149,237).
    /// </summary>
    public GameSettings SetClearColor(uint red, uint green, uint blue)
    {
        ClearColor = new Color((byte)red, (byte)green, (byte)blue);
        return this;
    }

    /// <summary>
    /// Sets the clear color from a Color object.
    /// </summary>
    public GameSettings SetClearColor(Color color)
        => SetClearColor(color.R, color.G, color.B);

    /// <summary>
    /// Sets the clear color from a hex string (e.g., "#3e3f3e").
    /// </summary>
    public GameSettings SetClearColor(string hex)
    {
        var c = new Color(hex);
        SetClearColor(c.R, c.G, c.B);
        return this;
    }

    /// <summary>
    /// Gets the clear color used to clear the render target each frame.
    /// </summary>
    public Color ClearColor { get; private set; }

    /// <summary>
    /// Enables half-texel offset for pixel-perfect rendering. Default is false.
    /// </summary>
    public GameSettings SetHalfTexelOffset(bool value)
    {
        UseHalfTexelOffset = value;
        return this;
    }

    /// <summary>
    /// Gets whether half-texel offset is enabled for pixel-perfect rendering.
    /// </summary>
    public bool UseHalfTexelOffset { get; private set; }

    /// <summary>
    /// Sets supersampling factor. Default is 4.
    /// </summary>
    public GameSettings SetSuperSample(uint value)
    {
        if (value == 0 || value > 16)
            throw new ArgumentOutOfRangeException(nameof(value), "SuperSample must be between 1 and 16");

        SuperSample = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the supersampling factor (1-16).
    /// </summary>
    public int SuperSample { get; private set; }

    /// <summary>
    /// Sets how the viewport scales to the window. Default is Fit.
    /// </summary>
    public GameSettings SetWindowScaleMode(WindowScaleMode mode)
    {
        _setWindowScaleMode = true;
        WindowScaleMode = mode;
        return this;
    }

    /// <summary>
    /// Gets how the viewport scales to the window.
    /// </summary>
    public WindowScaleMode WindowScaleMode { get; private set; }

    #endregion

    #region Atlas

    /// <summary>
    /// Sets the atlas defragmentation threshold (5-80%). Default is 30%.
    /// </summary>
    public GameSettings SetAtlasDefragThreshold(float value)
    {
        if (value < 0.05f || value > 0.80f)
            throw new ArgumentOutOfRangeException(nameof(value), "Threshold must be between 5% and 80%");

        AtlasDefragThreshold = value;
        return this;
    }

    /// <summary>
    /// Gets the atlas defragmentation threshold.
    /// </summary>
    public float AtlasDefragThreshold { get; private set; }

    /// <summary>
    /// Sets the atlas page size. Default is 2048.
    /// </summary>
    public GameSettings SetAtlasPageSize(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageSize = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the atlas page size in pixels.
    /// </summary>
    public int AtlasPageSize { get; private set; }

    /// <summary>
    /// Sets the number of atlas pages. Default is 4.
    /// </summary>
    public GameSettings SetAtlasPageCount(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageCount = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the number of atlas pages.
    /// </summary>
    public int AtlasPageCount { get; private set; }

    /// <summary>
    /// Sets the atlas packer implementation. Default is SkylinePacker.
    /// </summary>
    public GameSettings SetAtlasPacker(IAtlasPacker value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        AtlasPacker = value;
        return this;
    }

    /// <summary>
    /// Gets the atlas packer implementation.
    /// </summary>
    public IAtlasPacker AtlasPacker { get; private set; }



    /// <summary>
    /// Sets the maximum number of atlas defragmentation moves to process per frame.
    /// </summary>
    /// <param name="value">
    /// The maximum number of moves per frame. Valid range is 1 to 100.
    /// The default value is 10.
    /// </param>
    /// <returns>The current <see cref="GameSettings"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is zero or exceeds 100.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This value controls how many texture moves are processed each frame during
    /// atlas defragmentation. Higher values complete defragmentation faster but
    /// may cause frame rate hitches. Lower values spread the work across more
    /// frames but take longer to complete.
    /// </para>
    /// <para>
    /// <b>Valid Range:</b> 1 to 100
    /// </para>
    /// <para>
    /// <b>Default Value:</b> 10
    /// </para>
    /// <para>
    /// <b>Typical Usage:</b> 10 to 20 moves per frame provides a good balance
    /// between defragmentation speed and performance. Values above 50 are
    /// generally unnecessary and may impact frame rate. Values above 100 are
    /// excessive and will be rejected.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// <code>
    /// // Conservative - minimal frame impact (default)
    /// settings.SetAtlasDefragMovesPerFrame(10);
    /// 
    /// // Aggressive - faster defrag, slight frame impact
    /// settings.SetAtlasDefragMovesPerFrame(30);
    /// 
    /// // Maximum allowed - use only if you know what you're doing
    /// settings.SetAtlasDefragMovesPerFrame(100);
    /// </code>
    /// </para>
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
    /// Gets or sets the maximum number of atlas defragmentation moves to process per frame.
    /// Higher values complete defragmentation faster but may cause frame hitches.
    /// </summary>
    public int AtlasDefragMovesPerFrame { get; private set; }

    #endregion

    #region Asset Management
    /// <summary>
    /// Sets how often the asset manager checks for expired assets.
    /// </summary>
    /// <param name="minutes">
    /// Number of minutes between eviction checks.
    /// Valid range is 1 to 60 minutes.
    /// </param>
    /// <returns>The current <see cref="GameSettings"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minutes"/> is below 1 or exceeds 60.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This value controls how frequently the asset manager checks for stale
    /// assets to evict.
    /// </para>
    /// <para>
    /// <b>Valid Range:</b> 1 to 60 minutes
    /// </para>
    /// <para>
    /// <b>Default Value:</b> 1 minute
    /// </para>
    /// </remarks>
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
    /// Gets how often the asset manager checks for expired assets.
    /// Default is 1 minute.
    /// </summary>
    public int AssetCheckIntervalMinutes { get; private set; }

    /// <summary>
    /// Sets asset eviction timeout in minutes.
    /// </summary>
    /// <param name="minutes">
    /// Number of minutes an asset can remain idle before being evicted.
    /// Valid range is 15 to 240 minutes.
    /// </param>
    /// <returns>The current <see cref="GameSettings"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minutes"/> is below 15 or exceeds 240.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This value controls how long an asset can remain unused before the asset
    /// manager unloads it to free memory.
    /// </para>
    /// <para>
    /// <b>Valid Range:</b> 15 to 240 minutes
    /// </para>
    /// <para>
    /// <b>Default Value:</b> 30 minutes
    /// </para>
    /// <para>
    /// Lower values evict more aggressively (frees memory faster but may cause
    /// frequent reloading). Higher values keep assets in memory longer (better
    /// performance but higher memory usage).
    /// </para>
    /// </remarks>
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
    /// Gets the asset eviction timeout in minutes.
    /// </summary>
    public int AssetEvictionMinutes { get; private set; }

    #endregion

    #region Batch Rendering

    /// <summary>
    /// Sets sprite batch capacity. Default is 1024.
    /// </summary>
    public GameSettings SetSpriteBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        SpriteBatchCapacity = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the sprite batch capacity.
    /// </summary>
    public int SpriteBatchCapacity { get; private set; }

    /// <summary>
    /// Sets primitive batch capacity. Default is 4096.
    /// </summary>
    public GameSettings SetPrimitiveBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        PrimitiveBatchCapacity = (int)value;
        return this;
    }

    /// <summary>
    /// Gets the primitive batch capacity.
    /// </summary>
    public int PrimitiveBatchCapacity { get; private set; }

    /// <summary>
    /// Enables batch sorting. Default is true.
    /// </summary>
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
    /// Sets the default sort mode. Default is BackToFront.
    /// </summary>
    public GameSettings SetDefaultSortMode(SortMode value)
    {
        _setDefaultSortMode = true;
        DefaultSortMode = value;
        return this;
    }

    /// <summary>
    /// Gets the default sort mode.
    /// </summary>
    public SortMode DefaultSortMode { get; private set; }

    /// <summary>
    /// Sets the default blend mode. Default is Alpha.
    /// </summary>
    public GameSettings SetDefaultBlendMode(IBlendMode value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        DefaultBlendMode = value;
        return this;
    }

    /// <summary>
    /// Gets the default blend mode.
    /// </summary>
    public IBlendMode DefaultBlendMode { get; private set; }

    #endregion

    #region Discoverable

    /// <summary>
    /// Sets how assemblies are scanned for discoverable types. Default is ExcludeFramework.
    /// </summary>
    public GameSettings SetDiscoverableScanMode(AssemblyScanMode mode)
    {
        DiscoverableScanMode = mode;
        return this;
    }

    /// <summary>
    /// Gets how assemblies are scanned for discoverable types.
    /// </summary>
    public AssemblyScanMode DiscoverableScanMode { get; private set; }

    /// <summary>
    /// Sets a custom filter for assembly discovery.
    /// </summary>
    public GameSettings SetDiscoverableAssemblyFilter(Func<Assembly, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        DiscoverableAssemblyFilter = filter;
        return this;
    }

    /// <summary>
    /// Gets the custom assembly filter for discovery.
    /// </summary>
    public Func<Assembly, bool> DiscoverableAssemblyFilter { get; private set; }

    /// <summary>
    /// Adds an assembly name to include in discovery.
    /// </summary>
    public GameSettings AddDiscoverableAssembly(string assemblyName)
    {
        if (assemblyName.IsEmpty())
            throw new ArgumentNullException(nameof(assemblyName));

        DiscoverableAssemblies.Add(assemblyName.Trim());
        return this;
    }

    /// <summary>
    /// Gets the set of assembly names to include in discovery.
    /// </summary>
    public HashSet<string> DiscoverableAssemblies { get; } = [];

    /// <summary>
    /// Adds a namespace prefix to exclude from discovery.
    /// </summary>
    public GameSettings AddDiscoverableExcludedPrefix(string prefix)
    {
        if (prefix.IsEmpty())
            throw new ArgumentNullException(nameof(prefix));

        DiscoverableExcludedPrefixes.Add(prefix.Trim());
        return this;
    }

    /// <summary>
    /// Gets the list of namespace prefixes to exclude from discovery.
    /// </summary>
    public List<string> DiscoverableExcludedPrefixes { get; } = [];

    #endregion

    #region Input

    /// <summary>
    /// Sets the gamepad dead zone (0-1). Default is 0.15.
    /// </summary>
    public GameSettings SetDeadZone(float value)
    {
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), "Dead zone must be between 0 and 1.");

        DeadZone = value;
        return this;
    }

    /// <summary>
    /// Gets the gamepad dead zone value (0-1).
    /// </summary>
    public float DeadZone { get; private set; }

    /// <summary>
    /// Sets whether to ignore input when the window is unfocused. Default is true.
    /// </summary>
    public GameSettings SetIgnoreInputWhenUnfocused(bool value)
    {
        _ignoreInputSet = true;
        IgnoreInputWhenUnfocused = value;
        return this;
    }

    /// <summary>
    /// Gets whether input is ignored when the window is unfocused.
    /// </summary>
    public bool IgnoreInputWhenUnfocused { get; private set; }

    #endregion

    #region Logging

    /// <summary>
    /// Sets the minimum log level. Default is Info.
    /// </summary>
    public GameSettings SetLogMinLevel(LogLevel level)
    {
        _setLogMinLevel = true;
        LogMinLevel = level;
        return this;
    }

    /// <summary>
    /// Gets the minimum log level.
    /// </summary>
    public LogLevel LogMinLevel { get; private set; }

    /// <summary>
    /// Sets the maximum log file size in MB. Default is 10.
    /// </summary>
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
    /// Sets the maximum number of log files. Default is 10.
    /// </summary>
    public GameSettings SetLogMaxFiles(uint count)
    {
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Log file count must be greater than zero");

        LogMaxFiles = (int)count;
        return this;
    }

    /// <summary>
    /// Gets the maximum number of log files to keep.
    /// </summary>
    public int LogMaxFiles { get; private set; }

    #endregion

    #region Trace

    /// <summary>
    /// Sets a callback for unhandled exceptions.
    /// </summary>
    public GameSettings SetOnCrash(Action<Exception> onCrash)
    {
        OnCrash = onCrash ?? throw new ArgumentNullException(nameof(onCrash));
        return this;
    }

    /// <summary>
    /// Gets the callback for unhandled exceptions.
    /// </summary>
    public Action<Exception> OnCrash { get; private set; }

    #endregion

    #region Sound

    /// <summary>
    /// Sets the maximum number of concurrent audio instances allowed in the sound pool.
    /// </summary>
    /// <param name="value">The maximum number of audio instances. Must be between 32 and 512.</param>
    /// <returns>The current <see cref="GameSettings"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is below 32 or exceeds 512.</exception>
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
    /// Finalizes the configuration and validates all settings.
    /// Must be called before creating a <see cref="Game"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required settings are missing.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when content root doesn't exist.</exception>
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

        // Apply defaults
        AtlasPageSize = AtlasPageSize <= 0 ? 2048 : AtlasPageSize;
        AtlasPageCount = AtlasPageCount <= 0 ? 4 : AtlasPageCount;
        AtlasPacker ??= new SkylinePacker(AtlasPageSize, AtlasPageSize);
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