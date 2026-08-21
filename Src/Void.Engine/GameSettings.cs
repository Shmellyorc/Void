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
        _setWindowScaleMode;

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
    public Color ClearColor { get; private set; }

    /// <summary>
    /// Enables half-texel offset for pixel-perfect rendering. Default is false.
    /// </summary>
    public GameSettings SetHalfTexelOffset(bool value)
    {
        UseHalfTexelOffset = value;
        return this;
    }
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
    public IAtlasPacker AtlasPacker { get; private set; }

    #endregion

    #region Asset Management

    /// <summary>
    /// Sets asset eviction timeout in minutes. Default is 30.
    /// </summary>
    public GameSettings SetAssetEviction(uint minutes)
    {
        AssetEvictionMinutes = (int)minutes;
        return this;
    }
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
    public int PrimitiveBatchCapacity { get; private set; }

    /// <summary>
    /// Enables batch sorting. Default is true.
    /// </summary>
    public GameSettings SetEnableBatchSorting(bool value)
    {
        EnableBatchSorting = value;
        return this;
    }
    public bool EnableBatchSorting { get; private set; }

    /// <summary>
    /// Sets the default sort mode. Default is BackToFront.
    /// </summary>
    public GameSettings SetDefaultSortMode(SortMode value)
    {
        DefaultSortMode = value;
        return this;
    }
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
        AssetEvictionMinutes = AssetEvictionMinutes == 0 ? 30 : AssetEvictionMinutes;
        AppTitle = AppTitle.IsEmpty() ? "Game" : AppTitle;
        Window = Window.IsZero ? new Vect2(1280, 720) : Window;
        Viewport = Viewport.IsZero ? new Vect2(320, 180) : Viewport;
        ClearColor = ClearColor.IsEmpty ? new Color(100, 149, 237) : ClearColor;
        SpriteBatchCapacity = SpriteBatchCapacity <= 0 ? 1024 : SpriteBatchCapacity;
        PrimitiveBatchCapacity = PrimitiveBatchCapacity <= 0 ? 4096 : PrimitiveBatchCapacity;
        DefaultSortMode = DefaultSortMode == SortMode.Immediate ? SortMode.BackToFront : DefaultSortMode;
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

        Initialized = true;

        return this;
    }
}