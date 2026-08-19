using Void.Engine.Logs;

namespace Void.Engine;

public sealed class GameSettings
{
    private bool _isFixedTimeStepSet, _ignoreInputSet, _isFullscreenSet, _isVSyncSet, _useApplicationDataSet, _setLogMinLevel;

    public static GameSettings Instance { get; private set; }
    public bool Initialized { get; private set; }

    public GameSettings()
    {
        Instance ??= this;
    }

    #region Application Data

    public GameSettings SetUseApplicationData(bool value)
    {
        _useApplicationDataSet = true;
        UseApplicationData = value;
        return this;
    }
    public bool UseApplicationData { get; private set; }


    public GameSettings SetAppName(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppName = name.Trim();

        return this;
    }
    public string AppName { get; private set; }



    public GameSettings SetAppCompany(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppCompany = name.Trim();

        return this;
    }
    public string AppCompany { get; private set; }



    public GameSettings SetAppTitle(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTitle = name.Trim();

        return this;
    }
    public string AppTitle { get; private set; }



    public GameSettings SetContentRoot(string path)
    {
        if (path.IsEmpty())
            throw new ArgumentNullException(nameof(path), "path cannot be null or empty");
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("The specified content root directory does not exist: '{path}'.");

        AppContentRoot = path;

        return this;
    }
    public string AppContentRoot { get; private set; }



    public GameSettings SetAppVersion(uint major, uint minor = 0, uint rebuild = 0, uint revision = 0)
    {
        if (major == 0)
            throw new ArgumentOutOfRangeException(nameof(major), "Major version must be greater than zero");

        AppVersion = new Version((int)major, (int)minor, (int)rebuild, (int)revision).ToString();
        return this;
    }
    public string AppVersion { get; private set; }
    public string AppVersionHash => $"{HashHelper.Cache64(AppVersion):X8}";

    #endregion

    #region Window & Viewport

    public GameSettings SetFullScreen(bool value)
    {
        _isFullscreenSet = true;
        Fullscreen = value;
        return this;
    }
    public bool Fullscreen { get; private set; }


    public GameSettings SetVsync(bool value)
    {
        _isVSyncSet = true;
        VSync = value;
        return this;
    }
    public bool VSync { get; private set; }


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

    public GameSettings SetFixedTimeStep(bool value)
    {
        _isFixedTimeStepSet = true;
        IsFixedTimeStep = value;
        return this;
    }
    public bool IsFixedTimeStep { get; private set; }




    public GameSettings SetTargetElapsedTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Target elapsed time must be greater than zero");

        TargetElapsedTime = seconds;
        return this;
    }
    public GameSettings SetTargetFPS(float fps)
    {
        if (fps <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero");

        TargetElapsedTime = 1f / fps;
        return this;
    }
    public float TargetElapsedTime { get; private set; }

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

    public GameSettings SetClearColor(uint red, uint green, uint blue)
    {
        ClearColor = new Color((byte)red, (byte)green, (byte)blue);

        return this;
    }
    public GameSettings SetClearColor(Color color)
        => SetClearColor(color.R, color.G, color.B);
    public GameSettings SetClearColor(string hex)
    {
        var c = new Color(hex);

        SetClearColor(c.R, c.G, c.B);

        return this;
    }
    public Color ClearColor { get; private set; }





    public GameSettings SetHalfTexelOffset(bool value)
    {
        UseHalfTexelOffset = value;
        return this;
    }
    public bool UseHalfTexelOffset { get; private set; }

    #endregion

    #region Atlas

    public GameSettings SetAtlasDefragThreshold(float value)
    {
        if (value < 0.05f || value > 0.80f)
            throw new ArgumentOutOfRangeException(nameof(value), "Threshold must be between 5% and 80%");

        AtlasDefragThreshold = value;
        return this;
    }
    public float AtlasDefragThreshold { get; private set; }



    public GameSettings SetAtlasPageSize(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageSize = (int)value;

        return this;
    }
    public int AtlasPageSize { get; private set; }


    public GameSettings SetAtlasPageCount(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        AtlasPageCount = (int)value;

        return this;
    }
    public int AtlasPageCount { get; private set; }


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

    public GameSettings SetAssetEviction(uint minutes)
    {
        AssetEvictionMinutes = (int)minutes;

        return this;
    }
    public int AssetEvictionMinutes { get; private set; }

    #endregion

    #region Batch Rendering

    public GameSettings SetSpriteBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        SpriteBatchCapacity = (int)value;

        return this;
    }
    public int SpriteBatchCapacity { get; private set; }

    public GameSettings SetPrimitiveBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        PrimitiveBatchCapacity = (int)value;

        return this;
    }
    public int PrimitiveBatchCapacity { get; private set; }

    public GameSettings SetEnableBatchSorting(bool value)
    {
        EnableBatchSorting = value;
        return this;
    }
    public bool EnableBatchSorting { get; private set; }

    public GameSettings SetDefaultSortMode(SortMode value)
    {
        DefaultSortMode = value;
        return this;
    }
    public SortMode DefaultSortMode { get; private set; }

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

    public GameSettings SetDiscoverableScanMode(AssemblyScanMode mode)
    {
        DiscoverableScanMode = mode;
        return this;
    }
    public AssemblyScanMode DiscoverableScanMode { get; private set; }



    public GameSettings SetDiscoverableAssemblyFilter(Func<Assembly, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        DiscoverableAssemblyFilter = filter;
        return this;
    }
    public Func<Assembly, bool> DiscoverableAssemblyFilter { get; private set; }



    public GameSettings AddDiscoverableAssembly(string assemblyName)
    {
        if (assemblyName.IsEmpty())
            throw new ArgumentNullException(nameof(assemblyName));

        DiscoverableAssemblies.Add(assemblyName.Trim());
        return this;
    }
    public HashSet<string> DiscoverableAssemblies { get; } = [];



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

    public GameSettings SetDeadZone(float value)
    {
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), "Dead zone must be between 0 and 1.");

        DeadZone = value;
        return this;
    }
    public float DeadZone { get; private set; }



    public GameSettings SetIgnoreInputWhenUnfocused(bool value)
    {
        _ignoreInputSet = true;
        IgnoreInputWhenUnfocused = value;
        return this;
    }
    public bool IgnoreInputWhenUnfocused { get; private set; }

    #endregion

    #region Logging

    public GameSettings SetLogMinLevel(LogLevel level)
    {
        _setLogMinLevel = true;
        LogMinLevel = level;

        return this;
    }
    public LogLevel LogMinLevel { get; private set; }



    public GameSettings SetLogMaxFileSizeMB(uint size)
    {
        if (size == 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Log file size must be greater than zero");

        LogMaxFileSizeMB = size;
        return this;
    }
    public uint LogMaxFileSizeMB { get; private set; }



    public GameSettings SetLogMaxFiles(uint count)
    {
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Log file count must be greater than zero");

        LogMaxFiles = (int)count;
        return this;
    }
    public int LogMaxFiles { get; private set; }

    #endregion

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
                    "No content directory found. Excepted to find either a 'Content' or 'Asset' folder."
                );
        }


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

        Initialized = true;

        return this;
    }
}