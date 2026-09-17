using System.Diagnostics;
using Bench.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class MonoGameBenchmarkGame : Game
{
    private const int MaxRecordedFrames = 2_000_000;

    private readonly BenchmarkConfig _config;
    private readonly SpriteInstance[] _sprites;
    private readonly bool _offscreen;
    private readonly BlendState _spriteBlendState;

    private readonly double[] _frameTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _batchTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _commandTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _flushTimes =
        new double[MaxRecordedFrames];

    private readonly GraphicsDeviceManager _graphics;

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _texture = null!;
    private RenderTarget2D? _renderTarget;

    private readonly Stopwatch _warmupTimer = new();

    private long _measurementStart;
    private long _lastFrameTimestamp;
    private long _allocatedStart;

    private int _gen0Start;
    private int _gen1Start;
    private int _gen2Start;

    private int _frameCount;
    private int _drawCalls;

    private bool _measuring;
    private bool _finished;

    public BenchmarkResult? Result { get; private set; }

    public MonoGameBenchmarkGame(
        BenchmarkConfig config,
        bool offscreen,
        BlendState spriteBlendState)
    {
        _config = config;
        _sprites = SpriteDataset.Create(config);
        _offscreen = offscreen;
        _spriteBlendState = spriteBlendState;

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = config.Width,
            PreferredBackBufferHeight = config.Height,
            SynchronizeWithVerticalRetrace = false
        };

        IsFixedTimeStep = false;
        Window.Title = "MonoGame 3.8.5.1 Benchmark";
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _texture = new Texture2D(
            GraphicsDevice,
            _config.SpriteWidth,
            _config.SpriteHeight);

        var pixels = new Color[
            _config.SpriteWidth * _config.SpriteHeight];

        Array.Fill(pixels, Color.White);
        _texture.SetData(pixels);

        if (_offscreen)
        {
            _renderTarget = new RenderTarget2D(
                GraphicsDevice,
                _config.Width,
                _config.Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);
        }

        _warmupTimer.Start();

        base.LoadContent();
    }

    protected override void Update(
        GameTime gameTime)
    {
        if (_finished)
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(
        GameTime gameTime)
    {
        long now = Stopwatch.GetTimestamp();
        int sampleIndex = -1;

        if (!_measuring)
        {
            if (_warmupTimer.Elapsed.TotalSeconds >=
                _config.WarmupSeconds)
            {
                BeginMeasurement();
            }
        }
        else
        {
            if (Stopwatch.GetElapsedTime(
                    _measurementStart,
                    now).TotalSeconds >=
                _config.MeasurementSeconds)
            {
                CompleteMeasurement();
                _finished = true;
                return;
            }

            if (_frameCount >= _frameTimes.Length)
            {
                CompleteMeasurement();
                _finished = true;
                return;
            }

            sampleIndex = _frameCount;

            _frameTimes[sampleIndex] =
                Stopwatch.GetElapsedTime(
                    _lastFrameTimestamp,
                    now).TotalMilliseconds;

            _lastFrameTimestamp = now;
        }

        if (_offscreen)
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            GraphicsDevice.SetRenderTarget(
                _renderTarget);

            GraphicsDevice.Clear(Color.Black);
        }
        else
        {
            GraphicsDevice.Clear(Color.Black);
        }

        long batchStart =
            Stopwatch.GetTimestamp();

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            _spriteBlendState,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        long commandStart =
            Stopwatch.GetTimestamp();

        for (int i = 0; i < _sprites.Length; i++)
        {
            SpriteInstance sprite =
                _sprites[i];

            _spriteBatch.Draw(
                _texture,
                new Vector2(
                    sprite.X,
                    sprite.Y),
                Color.White);
        }

        long commandEnd =
            Stopwatch.GetTimestamp();

        _spriteBatch.End();

        long batchEnd =
            Stopwatch.GetTimestamp();

        if (_offscreen)
        {
            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            _spriteBatch.Draw(
                _renderTarget!,
                new Rectangle(
                    0,
                    0,
                    _config.Width,
                    _config.Height),
                Color.White);

            _spriteBatch.End();
        }

        if (sampleIndex >= 0)
        {
            _batchTimes[sampleIndex] =
                Stopwatch.GetElapsedTime(
                    batchStart,
                    batchEnd).TotalMilliseconds;

            _commandTimes[sampleIndex] =
                Stopwatch.GetElapsedTime(
                    commandStart,
                    commandEnd).TotalMilliseconds;

            _flushTimes[sampleIndex] =
                Stopwatch.GetElapsedTime(
                    commandEnd,
                    batchEnd).TotalMilliseconds;

            _drawCalls =
                checked(
                    (int)GraphicsDevice
                        .Metrics
                        .DrawCount);

            _frameCount++;
        }

        base.Draw(gameTime);
    }

    private void BeginMeasurement()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _frameCount = 0;

        _allocatedStart =
            GC.GetAllocatedBytesForCurrentThread();

        _gen0Start = GC.CollectionCount(0);
        _gen1Start = GC.CollectionCount(1);
        _gen2Start = GC.CollectionCount(2);

        long timestamp =
            Stopwatch.GetTimestamp();

        _measurementStart = timestamp;
        _lastFrameTimestamp = timestamp;
        _measuring = true;
    }

    private void CompleteMeasurement()
    {
        long allocated =
            GC.GetAllocatedBytesForCurrentThread()
            - _allocatedStart;

        string version =
            typeof(Game)
                .Assembly
                .GetName()
                .Version?
                .ToString()
            ?? "Unknown";

        Result = new BenchmarkResult
        {
            Framework = "MonoGame",
            FrameworkVersion = version,
            Scenario = _offscreen
                ? "StaticSprites_OffscreenPresent_BlendAB"
                : "StaticSprites_Direct_BlendAB",
            Config = _config,

            FrameStatistics =
                FrameStatistics.Calculate(
                    _frameTimes.AsSpan(
                        0,
                        _frameCount)),

            BatchStatistics =
                FrameStatistics.Calculate(
                    _batchTimes.AsSpan(
                        0,
                        _frameCount)),

            CommandStatistics =
                FrameStatistics.Calculate(
                    _commandTimes.AsSpan(
                        0,
                        _frameCount)),

            FlushStatistics =
                FrameStatistics.Calculate(
                    _flushTimes.AsSpan(
                        0,
                        _frameCount)),

            FramesMeasured = _frameCount,
            AllocatedBytes = allocated,
            BatchAllocatedBytes = 0,

            Gen0Collections =
                GC.CollectionCount(0)
                - _gen0Start,

            Gen1Collections =
                GC.CollectionCount(1)
                - _gen1Start,

            Gen2Collections =
                GC.CollectionCount(2)
                - _gen2Start,

            DrawCalls = _drawCalls
        };

        _measuring = false;
    }

    protected override void UnloadContent()
    {
        _renderTarget?.Dispose();
        _spriteBatch?.Dispose();
        _texture?.Dispose();

        base.UnloadContent();
    }
}
