using Void.Engine.Systems;

namespace Void.Engine;

public class Game : IDisposable
{
    private readonly GameSettings _settings;
    private readonly Window _window;
    private readonly BeaconManager _beacon = new();
    private readonly Coroutine _coroutine = new();
    private readonly AssetManager _asset = new();
    private readonly AtlasManager _atlas = new();
    private readonly FrameTime _timing = new();
    private readonly SFClock _sfClock = new();
    private SFTime _previousTime;
    private bool _isDisposed;

    internal int _scrollWheel;

    public static Game Instance { get; private set; }
    public bool IsActive => _window.IsFocused;
    public FrameTime FrameTime => _timing;
    public Window Window => _window;

    public Game(GameSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings), "Settings is null");
        if (!settings.Initialized)
            throw new InvalidOperationException($"Settings has never been build. Please use .Build() to finalize the build");

        Instance ??= this;
        _settings = settings;

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
        _window.Dispose();

        GC.SuppressFinalize(this);
        _isDisposed = true;
    }
}