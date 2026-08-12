namespace Void.Engine;

public sealed class GameSettings
{
    private bool _isFixedTimeStepSet, _ignoreInputSet, _isFullscreenSet, _isVSyncSet;

    public static GameSettings Instance { get; private set; }
    public bool Initialized { get; private set; }


    public GameSettings()
    {
        Instance ??= this;
    }





    public GameSettings SetAppName(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppName = name.Trim();

        return this;
    }
    internal string AppName { get; private set; }



    public GameSettings SetAppCompany(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppCompany = name.Trim();

        return this;
    }
    internal string AppCompany { get; private set; }



    public GameSettings SetAppTitle(string name)
    {
        if (name.IsEmpty())
            throw new ArgumentNullException(nameof(name), "name cannot be null or empty");

        AppTitle = name.Trim();

        return this;
    }
    internal string AppTitle { get; private set; }



    public GameSettings SetContentRoot(string path)
    {
        if (path.IsEmpty())
            throw new ArgumentNullException(nameof(path), "path cannot be null or empty");
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("The specified content root directory does not exist: '{path}'.");

        AppContentRoot = path;

        return this;
    }
    internal string AppContentRoot { get; private set; }



    public GameSettings SetFullScreen(bool value)
    {
        _isFullscreenSet = true;
        Fullscreen = value;
        return this;
    }
    internal bool Fullscreen { get; private set; }


    public GameSettings SetVsync(bool value)
    {
        _isVSyncSet = true;
        VSync = value;
        return this;
    }
    internal bool VSync { get; private set; }


    #region Frame Timing

    public GameSettings SetFixedTimeStep(bool value)
    {
        _isFixedTimeStepSet = true;
        IsFixedTimeStep = value;
        return this;
    }



    internal bool IsFixedTimeStep { get; private set; }

    public GameSettings SetTargetElapsedTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Target elapsed time must be greater than zero");

        TargetElapsedTime = seconds;
        return this;
    }
    internal float TargetElapsedTime { get; private set; }

    public GameSettings SetTargetFPS(float fps)
    {
        if (fps <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero");

        TargetElapsedTime = 1f / fps;
        return this;
    }

    public GameSettings SetMaxDeltaTime(float seconds)
    {
        if (seconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Max delta time must be greater than zero");

        MaxDeltaTime = seconds;
        return this;
    }
    internal float MaxDeltaTime { get; private set; }

    #endregion






    public GameSettings SetWindow(uint width, uint height)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width is zero");
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height is zero");

        Window = new Vect2(width, height);

        return this;
    }
    internal Vect2 Window { get; private set; }



    public GameSettings SetViewport(uint width, uint height)
    {
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width is zero");
        if (width == 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height is zero");

        Viewport = new Vect2(width, height);

        return this;
    }
    internal Vect2 Viewport { get; private set; }












    public GameSettings SetClearColor(uint red, uint green, uint blue)
    {
        ClearColor = new Color(red, green, blue);

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
    internal Color ClearColor { get; private set; }





    public GameSettings SetHalfTexelOffset(bool value)
    {
        UseHalfTexelOffset = value;
        return this;
    }
    internal bool UseHalfTexelOffset { get; private set; }



    public GameSettings SetAssetEviction(uint minutes)
    {
        AssetEvictionMinutes = (int)minutes;

        return this;
    }
    internal int AssetEvictionMinutes { get; private set; }





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
    internal IAtlasPacker AtlasPacker { get; private set; }




    public GameSettings SetSpriteBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        SpriteBatchCapacity = (int)value;

        return this;
    }
    internal int SpriteBatchCapacity { get; private set; }

    public GameSettings SetPrimitiveBatchCapacity(uint value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero");

        PrimitiveBatchCapacity = (int)value;

        return this;
    }
    internal int PrimitiveBatchCapacity { get; private set; }

    public GameSettings SetEnableBatchSorting(bool value)
    {
        EnableBatchSorting = value;
        return this;
    }
    internal bool EnableBatchSorting { get; private set; }

    public GameSettings SetDefaultSortMode(SortMode value)
    {
        DefaultSortMode = value;
        return this;
    }
    internal SortMode DefaultSortMode { get; private set; }

    public GameSettings SetDefaultBlendMode(IBlendMode value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        DefaultBlendMode = value;

        return this;
    }
    internal IBlendMode DefaultBlendMode { get; private set; }



    public GameSettings SetDeadZone(float value)
    {
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), "Dead zone must be between 0 and 1.");

        DeadZone = value;
        return this;
    }
    internal float DeadZone { get; private set; }



    public GameSettings SetIgnoreInputWhenUnfocused(bool value)
    {
        _ignoreInputSet = true;
        IgnoreInputWhenUnfocused = value;
        return this;
    }
    internal bool IgnoreInputWhenUnfocused { get; private set; }



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

        Initialized = true;

        return this;
    }
}