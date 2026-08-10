namespace Void.Engine;

public class Game : IDisposable
{
    private readonly GameSettings _settings;
    private readonly BeaconManager _beacon = new();
    private readonly Coroutine _coroutine = new();
    private readonly AssetManager _asset = new();
    private readonly AtlasManager _atlas = new();
    private readonly FrameTime _timing = new();
    private readonly SFClock _sfClock = new();
    private SFTime _previousTime;
    private bool _isDisposed;

    internal SFRenderWindow _window;
    internal SFRenderTexture _renderTexture;
    internal SFSprite _renderSprite;
    internal int _scrollWheel;

    public static Game Instance { get; private set; }
    public bool IsActive { get; private set; }

    public Game(GameSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings), "Settings is null");
        if (!settings.Initialized)
            throw new InvalidOperationException($"Settings has never been build. Please use .Build() to finalize the build");

        Instance ??= this;

        _settings = settings;

        var styles = SFStyles.Close | SFStyles.Titlebar;
        var video = new SFVideoMode((uint)_settings.Window.X, (uint)_settings.Window.Y);
        var context = new SFContextSettings { MajorVersion = 4, MinorVersion = 0 };

        _window = new SFRenderWindow(video, _settings.AppTitle, styles, context);

        _renderTexture = new SFRenderTexture((uint)_settings.Window.X, (uint)_settings.Window.Y);
        _renderSprite = new SFSprite(_renderTexture.Texture);

        _window.Closed += (s, o) => _window.Close();
        _window.GainedFocus += (s, o) => IsActive = true;
        _window.LostFocus += (s, o) => IsActive = false;
        _window.MouseWheelScrolled += (sender, args) => _scrollWheel += (int)args.Delta;
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

                _renderTexture.Clear(_settings.ClearColor);
                OnDraw(_timing);
                _renderTexture.Display();
            }
            else
            {
                _coroutine.Update(_timing);
                OnUpdate(_timing);

                _renderTexture.Clear(_settings.ClearColor);
                OnDraw(_timing);
                _renderTexture.Display();
            }

            _window.Clear();
            _window.Draw(_renderSprite);
            _window.Display();
        }
    }

    protected virtual void OnUpdate(FrameTime timing) { }
    protected virtual void OnDraw(FrameTime timing) { }
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

        _renderSprite?.Dispose();
        _renderTexture?.Dispose();
        _window?.Dispose();

        GC.SuppressFinalize(this);

        _isDisposed = true;
    }
}