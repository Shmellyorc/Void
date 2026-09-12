// ============================================================================
//  Game.cs
// ============================================================================
//  Owns the main VOID application lifecycle, game loop, window, and frame timing.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Diagnostics;

namespace Void.Engine;

/// <summary>
/// Provides the main VOID game lifecycle and application loop.
/// </summary>
/// <remarks>
/// <para>
/// Create one game instance from finalized <see cref="GameSettings"/>, then call
/// <see cref="Run"/> to start the application. Derive from <see cref="Game"/> and
/// override the protected lifecycle methods to provide game-specific behavior.
/// </para>
/// <example>
/// <code>
/// public sealed class MyGame : Game
/// {
///     public MyGame(GameSettings settings) : base(settings) { }
///
///     protected override void OnUpdate(FrameTime frameTime)
///     {
///         // Update game logic.
///     }
///
///     protected override void OnDraw(FrameTime frameTime)
///     {
///         // Submit rendering work.
///     }
/// }
///
/// var settings = GameSettings.Instance
///     .SetAppCompany("MyStudio")
///     .SetAppName("MyGame")
///     .Build();
///
/// using var game = new MyGame(settings);
/// game.Run();
/// </code>
/// </example>
/// </remarks>
public class Game : IDisposable
{
    private const string DefaultFontTag = "Void.Engine.Internal.DefaultFont";

    private readonly GameSettings _settings;
    private readonly Window _window;
    private readonly FrameTime _timing;
    private readonly Stopwatch _clock;
    private double _previousTimeSeconds;
    private bool _isDisposed;

    internal int _scrollWheel;

    /// <summary>
    /// Gets the first game instance created in the current process.
    /// </summary>
    public static Game Instance { get; private set; }

    /// <summary>
    /// Gets whether the game window currently has input focus.
    /// </summary>
    public bool IsActive => _window.IsFocused;

    /// <summary>
    /// Gets timing information for the current frame.
    /// </summary>
    public FrameTime FrameTime => _timing;

    /// <summary>
    /// Gets the game window.
    /// </summary>
    public Window Window => _window;

    /// <summary>
    /// Gets the full path to the application's log directory.
    /// </summary>
    public string ApplicationLogFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppLogFolder);

    /// <summary>
    /// Gets the full path to the application's save-data directory.
    /// </summary>
    public string ApplicationSaveFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppSaveFolder);

    /// <summary>
    /// Gets the full path to the application's configuration directory.
    /// </summary>
    public string ApplicationConfigFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppConfigFolder);

    /// <summary>
    /// Gets the full path to the application's temporary-data directory.
    /// </summary>
    public string ApplicationTempFolder => Path.Combine(ApplicationFolder, GameSettings.Instance.AppTempFolder);

    /// <summary>
    /// Gets the version of the running VOID Engine assembly.
    /// </summary>
    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    /// <summary>
    /// Gets a stable hexadecimal hash derived from <see cref="Version"/>.
    /// </summary>
    public string VersionHash => $"{HashHelper.Cache64(Version):X8}";

    /// <summary>
    /// Gets the root directory used for application-specific data.
    /// </summary>
    /// <remarks>
    /// When <see cref="GameSettings.UseApplicationData"/> is enabled, VOID uses the
    /// platform application-data location. Otherwise, a directory beside the game
    /// executable is used.
    /// </remarks>
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
    /// Gets VOID's built-in sprite font for simple text and debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The font is loaded from embedded engine resources and cached by the asset
    /// manager. Games can use their own fonts whenever a custom typeface is needed.
    /// </para>
    /// <code>
    /// spriteBatch.DrawText(
    ///     Game.Instance.Font,
    ///     "Hello VOID",
    ///     new Vect2(8, 8),
    ///     Color.White);
    /// </code>
    /// </remarks>
    public SpriteFont Font
    {
        get
        {
            if (AssetManager.Instance.TryGetAsset<SpriteFont>(DefaultFontTag, out var font))
                return font;

            LoadDefaultFont();
            return AssetManager.Instance.TryGetAsset<SpriteFont>(DefaultFontTag, out var f) ? f : null;
        }
    }

    /// <summary>
    /// Initializes a game using finalized engine settings.
    /// </summary>
    /// <param name="settings">The settings returned by <see cref="GameSettings.Build"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="settings"/> has not been finalized with
    /// <see cref="GameSettings.Build"/>.
    /// </exception>
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

        // The renderer is initialized before the default font so the font can
        // create any renderer-owned resources through VOID's graphics contracts.
        LoadDefaultFont();

        _clock = new Stopwatch();
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
    /// Releases game resources if the instance was not disposed explicitly.
    /// </summary>
    ~Game() => Dispose();

    /// <summary>
    /// Runs the game loop until the window closes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="OnEnter"/> is called once before the loop. Depending on the
    /// configured timing mode, <see cref="OnUpdate"/> runs at the fixed update rate
    /// or once per rendered frame. <see cref="OnDraw"/> runs once for each frame
    /// that is presented.
    /// </para>
    /// <para>
    /// This method blocks the calling thread until the game exits.
    /// </para>
    /// </remarks>
    public void Run()
    {
        OnEnter();

        _clock.Restart();
        _previousTimeSeconds = 0d;

        while (_window.IsOpen)
        {
            _window.DispatchEvents();

            double currentTime = _clock.Elapsed.TotalSeconds;
            float rawDelta = (float)(currentTime - _previousTimeSeconds);
            _previousTimeSeconds = currentTime;

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
    /// Requests a graceful exit from the game loop.
    /// </summary>
    /// <remarks>
    /// Closing the window causes <see cref="Run"/> to finish. Resource cleanup
    /// occurs when the game is disposed.
    /// </remarks>
    public void Quit()
    {
        if (_isDisposed) return;

        Window.Close();
    }

    /// <summary>
    /// Called for game logic updates.
    /// </summary>
    /// <param name="frameTime">Timing information for the current update.</param>
    /// <remarks>
    /// In fixed-timestep mode this method can run multiple times before a rendered
    /// frame when the game needs to catch up.
    /// </remarks>
    protected virtual void OnUpdate(FrameTime frameTime) { }

    /// <summary>
    /// Called once for each rendered frame.
    /// </summary>
    /// <param name="frameTime">Timing information for the current frame.</param>
    protected virtual void OnDraw(FrameTime frameTime) { }

    /// <summary>
    /// Called once immediately before the game loop begins.
    /// </summary>
    protected virtual void OnEnter() { }

    /// <summary>
    /// Called during disposal before engine-owned runtime systems are cleared.
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
    /// Releases the game window and engine-owned runtime resources.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent. <see cref="OnExit"/> is called before the engine
    /// clears coroutines, beacons, assets, atlas resources, input devices, and the
    /// window.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        OnExit();

        CoroutineManager.Instance.StopAll();
        BeaconManager.Instance.Clear();
        AssetManager.Instance.Clear();
        AtlasManager.Instance.Clear();
        Inputs.Gamepads.Gamepad.Shutdown();
        _window.Dispose();

        GC.SuppressFinalize(this);
        _isDisposed = true;

        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }
}
