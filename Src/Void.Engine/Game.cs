using Void.Engine.Logs;
using Void.Engine.Logs.Sinks;
using Void.Engine.Systems;

namespace Void.Engine;

public class Game : IDisposable
{
    private readonly GameSettings _settings;
    private readonly Window _window;
    private readonly BeaconManager _beacon;
    private readonly CoroutineManager _coroutine;
    private readonly AssetManager _asset;
    private readonly AtlasManager _atlas;
    private readonly FrameTime _timing;
    private readonly SFClock _sfClock;
    private SFTime _previousTime;
    private bool _isDisposed;

    internal int _scrollWheel;

    public static Game Instance { get; private set; }
    public bool IsActive => _window.IsFocused;
    public FrameTime FrameTime => _timing;
    public Window Window => _window;
    public string ApplicationLogFolder => Path.Combine(ApplicationFolder, "Logs");
    public string ApplicationSaveFolder => Path.Combine(ApplicationFolder, "Saves");
    public string ApplicationConfigFolder => Path.Combine(ApplicationFolder, "Config");
    public string ApplicationTempFolder => Path.Combine(ApplicationFolder, "Temp");
    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    public string VersionHash => $"{HashHelper.Cache64(Version):X8}";

    public string ApplicationFolder
    {
        get
        {
            if (_settings.UseApplicationData)
                return FileHelper.GetApplicationData(_settings.AppCompany, _settings.AppName);

            string localPath = Path.Combine(AppContext.BaseDirectory, _settings.AppName);

            // If this path is a file (like the executable), use a subfolder instead
            if (File.Exists(localPath) && !Directory.Exists(localPath))
                return Path.Combine(AppContext.BaseDirectory, _settings.AppName + "Data");

            return localPath;
        }
    }


    public Game(GameSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings), "Settings is null");
        if (!settings.Initialized)
            throw new InvalidOperationException($"Settings has never been build. Please use .Build() to finalize the build");

        Instance ??= this;
        _settings = settings;

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
            _settings.VSync
        )
        {
            OnMouseWheelScrolled = delta => _scrollWheel += delta
        };

        _sfClock = new SFClock();
        _timing = new FrameTime();
        _asset = new AssetManager();
        _atlas = new AtlasManager();
        _beacon = new BeaconManager();
        _coroutine = new CoroutineManager();

        Logger.Instance.Info("VOID setting up Application folders...");
        FileHelper.EnsureDirectoryExists(ApplicationFolder);
        FileHelper.EnsureDirectoryExists(ApplicationLogFolder);
        FileHelper.EnsureDirectoryExists(ApplicationSaveFolder);
        FileHelper.EnsureDirectoryExists(ApplicationConfigFolder);
        FileHelper.EnsureDirectoryExists(ApplicationTempFolder);
        Logger.Instance.Info("Application folders ready");
    }

    ~Game() => Dispose();

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
                    _coroutine.Update(_timing);
                    OnUpdate(_timing);
                    _timing.ConsumeFixedUpdate();
                }

                _window.BeginRender(_settings.ClearColor);
                OnDraw(_timing);
                _window.EndRender();
            }
            else
            {
                _coroutine.Update(_timing);
                OnUpdate(_timing);

                _window.BeginRender(_settings.ClearColor);
                OnDraw(_timing);
                _window.EndRender();
            }
        }
    }

    protected virtual void OnUpdate(FrameTime frameTime) { }
    protected virtual void OnDraw(FrameTime frameTime) { }
    protected virtual void OnEnter() { }
    protected virtual void OnExit() { }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        OnExit();

        _coroutine.StopAll();
        _beacon.Clear();
        _asset.Clear();
        _atlas.Clear();
        _window.Dispose();

        GC.SuppressFinalize(this);
        _isDisposed = true;
    }




    private void CreateFolder(string path, string description)
    {
        try
        {
            if (FileHelper.EnsureDirectoryExists(path))
            {
                Console.WriteLine($"Created {description}: {path}");
            }
            else
            {
                Console.WriteLine($"{description} already exists: {path}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Unable to create {description} at '{path}'. {ex.Message}");
            throw new IOException($"Unable to create {description} at '{path}'.", ex);
        }
    }
}