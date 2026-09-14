// ============================================================================
//  FrameTime.cs
// ============================================================================
//  Manages frame timing information including delta time, fixed timestep
//  accumulation, and interpolation alpha for smooth rendering.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Diagnostics;

namespace Void.Engine.Systems;

/// <summary>
/// Provides comprehensive timing information for the game loop, including
/// elapsed time, total time, fixed timestep management, interpolation,
/// time scaling, and frame-rate information.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FrameTime"/> owns the timing state used by VOID. Engine systems
/// receive the complete <see cref="FrameTime"/> instance rather than standalone
/// delta-time values.
/// </para>
/// <para>
/// In fixed timestep mode, <see cref="DeltaTime"/> represents the configured
/// fixed interval while game updates are running. During rendering it represents
/// the actual elapsed time of the current rendered frame.
/// </para>
/// <para>
/// In variable timestep mode, update and render phases both use the actual
/// elapsed frame time.
/// </para>
/// </remarks>
public sealed class FrameTime
{
    private readonly Stopwatch _clock = new();

    private TimeSpan _totalTime;
    private TimeSpan _elapsedTime;
    private TimeSpan _frameElapsedTime;

    private double _previousTimeSeconds;

    private float _accumulator;
    private bool _isRunningSlowly;

    private int _frameCount;
    private float _fpsTimer;
    private float _fps;

    /// <summary>
    /// Gets the total elapsed game time accumulated by the game loop.
    /// </summary>
    public TimeSpan TotalTime => _totalTime;

    /// <summary>
    /// Gets the unscaled elapsed time for the current timing phase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// During a fixed update this is the configured fixed timestep.
    /// </para>
    /// <para>
    /// During rendering this is the actual elapsed time of the current frame.
    /// </para>
    /// <para>
    /// In variable timestep mode both update and rendering use the actual
    /// elapsed frame time.
    /// </para>
    /// </remarks>
    public TimeSpan ElapsedTime => _elapsedTime;

    /// <summary>
    /// Gets whether the current frame exceeded the configured maximum delta time
    /// and had to be clamped.
    /// </summary>
    public bool IsRunningSlowly => _isRunningSlowly;

    /// <summary>
    /// Gets the current rendered frames per second.
    /// </summary>
    public float FPS => _fps;

    /// <summary>
    /// Gets the amount of unconsumed time available for fixed updates.
    /// </summary>
    public float Accumulator => _accumulator;

    /// <summary>
    /// Gets the interpolation factor between fixed updates.
    /// </summary>
    /// <remarks>
    /// Returns a value between 0 and 1 in fixed timestep mode.
    /// Variable timestep mode always returns 1.
    /// </remarks>
    public float Alpha
    {
        get
        {
            if (!IsFixedTimeStep || TargetElapsed <= 0f)
                return 1f;

            return Math.Clamp(
                _accumulator / TargetElapsed,
                0f,
                1f);
        }
    }

    /// <summary>
    /// Gets whether VOID is using a fixed timestep.
    /// </summary>
    public bool IsFixedTimeStep { get; }

    /// <summary>
    /// Gets the configured fixed update interval in seconds.
    /// </summary>
    public float TargetElapsed { get; }

    /// <summary>
    /// Gets the maximum allowed frame delta in seconds.
    /// </summary>
    public float MaxDeltaTime { get; }

    /// <summary>
    /// Gets or sets the global time-scale multiplier.
    /// </summary>
    /// <remarks>
    /// A value of 1 represents normal speed, 0 pauses scaled time,
    /// values below 1 slow time, and values above 1 speed time up.
    /// </remarks>
    public float TimeScale { get; set; }

    /// <summary>
    /// Gets the scaled elapsed time for the current timing phase in seconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// During fixed updates this is the configured fixed timestep multiplied
    /// by <see cref="TimeScale"/>.
    /// </para>
    /// <para>
    /// During rendering this is the actual elapsed frame time multiplied by
    /// <see cref="TimeScale"/>.
    /// </para>
    /// </remarks>
    public float DeltaTime
        => (float)_elapsedTime.TotalSeconds * TimeScale;

    /// <summary>
    /// Gets the unscaled elapsed time for the current timing phase in seconds.
    /// </summary>
    public float UnscaledDeltaTime
        => (float)_elapsedTime.TotalSeconds;

    internal FrameTime()
    {
        var settings = GameSettings.Instance;

        IsFixedTimeStep = settings.IsFixedTimeStep;
        TargetElapsed = settings.TargetElapsedTime;
        MaxDeltaTime = settings.MaxDeltaTime;

        _totalTime = TimeSpan.Zero;
        _elapsedTime = TimeSpan.Zero;
        _frameElapsedTime = TimeSpan.Zero;

        _previousTimeSeconds = 0d;

        _accumulator = 0f;
        _isRunningSlowly = false;

        _frameCount = 0;
        _fpsTimer = 0f;
        _fps = 0f;

        TimeScale = 1f;
    }

    /// <summary>
    /// Starts or restarts frame timing.
    /// </summary>
    internal void Start()
    {
        _clock.Restart();

        _previousTimeSeconds = 0d;

        _totalTime = TimeSpan.Zero;
        _elapsedTime = TimeSpan.Zero;
        _frameElapsedTime = TimeSpan.Zero;

        _accumulator = 0f;
        _isRunningSlowly = false;

        _frameCount = 0;
        _fpsTimer = 0f;
        _fps = 0f;
    }

    /// <summary>
    /// Begins a new outer game frame and records its elapsed time.
    /// </summary>
    internal void BeginFrame()
    {
        double currentTimeSeconds = _clock.Elapsed.TotalSeconds;

        float elapsed = (float)(
            currentTimeSeconds - _previousTimeSeconds);

        _previousTimeSeconds = currentTimeSeconds;

        if (elapsed > MaxDeltaTime)
        {
            _isRunningSlowly = true;
            elapsed = MaxDeltaTime;
        }
        else
        {
            _isRunningSlowly = false;
        }

        _frameElapsedTime =
            TimeSpan.FromSeconds(elapsed);

        _totalTime += _frameElapsedTime;

        if (IsFixedTimeStep)
        {
            _accumulator += elapsed;

            _elapsedTime =
                TimeSpan.FromSeconds(TargetElapsed);
        }
        else
        {
            _accumulator = 0f;
            _elapsedTime = _frameElapsedTime;
        }

        _fpsTimer += elapsed;
        _frameCount++;

        if (_fpsTimer >= 1f)
        {
            _fps = _frameCount;

            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    /// <summary>
    /// Selects fixed-update timing for the next game update.
    /// </summary>
    internal void BeginFixedUpdate()
    {
        if (!IsFixedTimeStep)
            return;

        _elapsedTime =
            TimeSpan.FromSeconds(TargetElapsed);
    }

    /// <summary>
    /// Selects actual frame timing for the rendering phase.
    /// </summary>
    internal void BeginRender()
    {
        _elapsedTime = _frameElapsedTime;
    }

    /// <summary>
    /// Consumes one fixed update interval from the accumulator.
    /// </summary>
    internal void ConsumeFixedUpdate()
    {
        if (!IsFixedTimeStep)
            return;

        _accumulator -= TargetElapsed;

        if (_accumulator < 0f)
            _accumulator = 0f;
    }
}
