// ============================================================================
//  Game.cs
// ============================================================================
//  The core game class. Manages the game loop, window, timing, and application
//  lifecycle. Create an instance with configured settings and call Run() to 
//  start.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine;

/// <summary>
/// The main game class. Create an instance with configured settings and call <see cref="Run"/> to start.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var settings = GameSettings.Instance
///     .SetAppCompany("MyStudio")
///     .SetAppName("MyGame")
///     .Build();
/// 
/// using var game = new Game(settings);
/// game.Run();
/// </code>
/// </remarks>
public class Game : IDisposable
{
    private const string DefaultFontTag = "Void.Engine.Internal.DefaultFont";

    private readonly GameSettings _settings;
    private readonly Window _window;
    private readonly FrameTime _timing;
    private readonly SFClock _sfClock;
    private SFTime _previousTime;
    private bool _isDisposed;

    internal int _scrollWheel;

    /// <summary>
    /// Gets the singleton instance. Set automatically on first construction.
    /// </summary>
    public static Game Instance { get; private set; }

    /// <summary>
    /// Returns true if the game window has focus.
    /// </summary>
    public bool IsActive => _window.IsFocused;

    /// <summary>
    /// Gets timing information for the current frame (delta time, fixed timestep, etc.).
    /// </summary>
    public FrameTime FrameTime => _timing;

    /// <summary>
    /// Gets the underlying window instance.
    /// </summary>
    public Window Window => _window;

    /// <summary>
    /// Full path to the log folder. Created during initialization.
    /// </summary>
    public string ApplicationLogFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppLogFolder);

    /// <summary>
    /// Full path to the save data folder. Created during initialization.
    /// </summary>
    public string ApplicationSaveFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppSaveFolder);

    /// <summary>
    /// Full path to the config folder. Created during initialization.
    /// </summary>
    public string ApplicationConfigFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppConfigFolder);

    /// <summary>
    /// Full path to the temp folder. Created during initialization.
    /// </summary>
    public string ApplicationTempFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppTempFolder);

    /// <summary>
    /// Gets the assembly version.
    /// </summary>
    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    /// <summary>
    /// Gets a hash of the version string, useful for build verification.
    /// </summary>
    public string VersionHash => $"{HashHelper.Cache64(Version):X8}";

    /// <summary>
    /// Gets the root application folder. Uses system app data or local directory based on settings.
    /// </summary>
    public string ApplicationFolder
    {
        get
        {
            if (_settings.UseApplicationData)
                return FileHelper.GetApplicationData(_settings.AppCompany, _settings.AppName);

            string localPath = Path.Combine(AppContext.BaseDirectory, _settings.AppName);

            if (File.Exists(localPath) && !Directory.Exists(localPath))
                return Path.Combine(AppContext.BaseDirectory, _settings.AppName + "Data");

            return localPath;
        }
    }

    /// <summary>
    /// Gets the default engine font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This font is loaded from embedded resources and cached in the AssetManager
    /// under the internal tag "Void.Engine.Internal.DefaultFont". It is always
    /// available and will not be evicted from the cache because each access
    /// updates its last access time.
    /// </para>
    /// <para>
    /// Use this font for UI elements, debug text, and any text rendering where
    /// a custom font is not required.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var font = Game.Instance.Font;
    /// batcher.DrawText(font, "Hello World!", position, Color.White);
    /// </code>
    /// </para>
    /// </remarks>
    public SpriteFont Font
    {
        get
        {
            if (AssetManager.Instance.TryGetAsset<SpriteFont>(DefaultFontTag, out var font))
                return font;

            // Should never happen, but just in case
            LoadDefaultFont();
            return AssetManager.Instance.TryGetAsset<SpriteFont>(DefaultFontTag, out var f) ? f : null;
        }
    }

    /// <summary>
    /// Creates a new game instance.
    /// </summary>
    /// <param name="settings">Configured settings from <see cref="GameSettings.Build"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when settings hasn't been built.</exception>
    /// <remarks>
    /// Example:
    /// <code>
    /// var settings = GameSettings.Instance
    ///     .SetAppCompany("MyStudio")
    ///     .SetAppName("MyGame")
    ///     .Build();
    /// 
    /// using var game = new Game(settings);
    /// game.Run();
    /// </code>
    /// </remarks>
    public Game(GameSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings), "Settings is null");
        if (!settings.Initialized)
            throw new InvalidOperationException($"Settings has never been build. Please use .Build() to finalize the build");

        Instance ??= this;
        _settings = settings;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Logger.Instance.AddSink(new ConsoleSink());
        Logger.Instance.AddSink(new FileSink(ApplicationLogFolder, _settings.LogMaxFileSizeMB, _settings.LogMaxFiles));
        Logger.Instance.SetLevel(_settings.LogMinLevel);

        Logger.Instance.Info("  ██╗   ██╗   ██████╗   ██╗  ██████╗");
        Logger.Instance.Info("  ██║   ██║  ██╔═══██╗  ██║  ██╔══██╗");
        Logger.Instance.Info("  ██║   ██║  ██║   ██║  ██║  ██║  ██║");
        Logger.Instance.Info("  ╚██╗ ██╔╝  ██║   ██║  ██║  ██║  ██║");
        Logger.Instance.Info("   ╚████╔╝   ╚██████╔╝  ██║  ██████╔╝");
        Logger.Instance.Info("    ╚═══╝     ╚═════╝   ╚═╝  ╚═════╝");
        Logger.Instance.Info("Version: {0}  Hash: {1}", Version, VersionHash);
        Logger.Instance.Info();

        LoadDefaultFont();

        _window = new Window(
            (int)_settings.Window.X,
            (int)_settings.Window.Y,
            _settings.AppTitle,
            _settings.Fullscreen ? WindowMode.Fullscreen : WindowMode.Windowed,
            _settings.VSync,
            EmbeddedResources.Exists("Data/Icon.png") ? EmbeddedResources.ReadAllBytes("Data/Icon.png") : null
        )
        {
            OnMouseWheelScrolled = delta => _scrollWheel += delta
        };

        _sfClock = new SFClock();
        _timing = new FrameTime();

        Logger.Instance.Info("VOID setting up Application folders...");
        FileHelper.EnsureDirectoryExists(ApplicationFolder);
        FileHelper.EnsureDirectoryExists(ApplicationLogFolder);
        FileHelper.EnsureDirectoryExists(ApplicationSaveFolder);
        FileHelper.EnsureDirectoryExists(ApplicationConfigFolder);
        FileHelper.EnsureDirectoryExists(ApplicationTempFolder);
        Logger.Instance.Info("Application folders ready");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        if (!string.IsNullOrEmpty(exception?.Message))
            Logger.Instance.FatalWithCategory("Game", $"Message: {exception.Message}");

        if (!string.IsNullOrEmpty(exception?.StackTrace))
        {
            Logger.Instance.FatalWithCategory("Game", "Stack Trace:");
            Logger.Instance.FatalWithCategory("Game", exception.StackTrace);
        }

        if (exception == null || (string.IsNullOrEmpty(exception.Message) && string.IsNullOrEmpty(exception.StackTrace)))
            Logger.Instance.FatalWithCategory("Game", "Unknown crash - no exception details available");

        _settings.OnCrash?.Invoke(exception);
    }

    /// <summary>
    /// Finalizer that ensures resources are cleaned up if <see cref="Dispose"/> wasn't called.
    /// </summary>
    ~Game() => Dispose();

    /// <summary>
    /// Starts the game loop. Blocks until the window closes.
    /// </summary>
    /// <remarks>
    /// Supports both fixed and variable timestep modes. Override <see cref="OnUpdate"/> and <see cref="OnDraw"/> for game logic.
    /// </remarks>
    public void Run()
    {
        OnEnter();

        _sfClock.Restart();
        _previousTime = SFTime.Zero;

        while (_window.IsOpen)
        {
            _window.DispatchEvents();

            var currentTime = _sfClock.ElapsedTime;
            float rawDelta = (currentTime - _previousTime).AsSeconds();
            _previousTime = currentTime;

            _timing.Update(rawDelta);

            if (_timing.IsFixedTimeStep)
            {
                while (_timing.Accumulator >= _timing.TargetElapsed)
                {
                    CoroutineManager.Instance.Update(_timing.TargetElapsed);
                    OnUpdate(_timing);
                    _timing.ConsumeFixedUpdate();
                }

                _window.BeginRender(_settings.ClearColor);
                OnDraw(_timing);
                _window.EndRender();
            }
            else
            {
                CoroutineManager.Instance.Update(_timing.DeltaTime);
                OnUpdate(_timing);

                _window.BeginRender(_settings.ClearColor);
                OnDraw(_timing);
                _window.EndRender();
            }
        }
    }

    /// <summary>
    /// Requests the game to exit gracefully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method closes the game window, which causes the main loop in
    /// <see cref="Run"/> to exit. Cleanup is handled automatically by
    /// <see cref="Dispose"/>.
    /// </para>
    /// </remarks>
    public void Quit()
    {
        if (_isDisposed) return;

        Window.Close();
    }

    /// <summary>
    /// Override to add your update logic. Called once per frame.
    /// </summary>
    /// <param name="frameTime">Timing info for this frame.</param>
    protected virtual void OnUpdate(FrameTime frameTime) { }

    /// <summary>
    /// Override to add your rendering logic. Called once per frame.
    /// </summary>
    /// <param name="frameTime">Timing info for this frame.</param>
    protected virtual void OnDraw(FrameTime frameTime) { }

    /// <summary>
    /// Override for initialization logic before the game loop starts.
    /// </summary>
    protected virtual void OnEnter() { }

    /// <summary>
    /// Override for cleanup logic when the game exits.
    /// </summary>
    protected virtual void OnExit() { }



    private void LoadDefaultFont()
    {
        try
        {
            if (!EmbeddedResources.Exists("Data/Font.png"))
            {
                Logger.Instance.WarningWithCategory("Game", "Default font not found in embedded resources.");
            }

            var fontData = EmbeddedResources.ReadAllBytes("Data/Font.png");
            AssetManager.Instance.LoadFromData<SpriteFont>(fontData, DefaultFontTag);

            Logger.Instance.InfoWithCategory("Game", "Default font loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.Instance.ErrorWithCategory("Game", "Failed to load default font: {0}", ex.Message);
        }
    }



    /// <summary>
    /// Cleans up all resources. Called automatically when disposed.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        OnExit();

        CoroutineManager.Instance.StopAll();
        BeaconManager.Instance.Clear();
        AssetManager.Instance.Clear();
        AtlasManager.Instance.Clear();
        _window.Dispose();

        GC.SuppressFinalize(this);
        _isDisposed = true;

        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }
}