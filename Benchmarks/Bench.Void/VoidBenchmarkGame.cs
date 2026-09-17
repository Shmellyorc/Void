using System.Diagnostics;
using Bench.Shared;
using Void.Engine;
using Void.Engine.Assets.Loaders;
using Void.Engine.Graphics;
using Void.Engine.Graphics.Rendering;
using Void.Engine.Systems;

public sealed class VoidBenchmarkGame : Game
{
    private const int MaxRecordedFrames = 2_000_000;

    private readonly BenchmarkConfig _config;
    private readonly SpriteInstance[] _sprites;
    private readonly int _batchCapacity;
    private readonly IBlendMode _blendMode;

    private readonly double[] _frameTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _batchTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _commandTimes =
        new double[MaxRecordedFrames];

    private readonly double[] _flushTimes =
        new double[MaxRecordedFrames];

    private SpriteBatcher _batch = null!;
    private Texture _texture = null!;

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

    public VoidBenchmarkGame(
        GameSettings settings,
        BenchmarkConfig config,
        int batchCapacity,
        IBlendMode blendMode)
        : base(settings)
    {
        _config = config;
        _sprites = SpriteDataset.Create(config);
        _batchCapacity = batchCapacity;
        _blendMode = blendMode;
    }

    protected override void OnEnter()
    {
        _texture = new Texture(
            new Vect2(
                _config.SpriteWidth,
                _config.SpriteHeight),
            Color.White);

        _batch = new SpriteBatcher(_batchCapacity);

        _warmupTimer.Start();

        base.OnEnter();
    }

    protected override void OnUpdate(
        FrameTime frameTime)
    {
        if (_finished)
            Quit();

        base.OnUpdate(frameTime);
    }

    protected override void OnDraw(
        FrameTime frameTime)
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

        long batchStart =
            Stopwatch.GetTimestamp();

        _batch.Begin(
            SortMode.Deferred,
            _blendMode);

        long commandStart =
            Stopwatch.GetTimestamp();

        for (int i = 0; i < _sprites.Length; i++)
        {
            SpriteInstance sprite =
                _sprites[i];

            _batch.Draw(
                _texture,
                new Vect2(
                    sprite.X,
                    sprite.Y),
                Color.White);
        }

        long commandEnd =
            Stopwatch.GetTimestamp();

        _batch.End();

        long batchEnd =
            Stopwatch.GetTimestamp();

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

            _drawCalls = _batch.DrawCallCount;
            _frameCount++;
        }

        base.OnDraw(frameTime);
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

        Result = new BenchmarkResult
        {
            Framework = "VOID",
            FrameworkVersion = Version,
            Scenario = "StaticSprites_BlendAB",
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

    protected override void OnExit()
    {
        _batch?.Dispose();
        _texture?.Dispose();

        base.OnExit();
    }
}
