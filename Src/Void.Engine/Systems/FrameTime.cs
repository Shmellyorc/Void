// ============================================================================
//  FrameTime.cs
// ============================================================================
//  Manages frame timing information including delta time, fixed timestep
//  accumulation, and interpolation alpha for smooth rendering.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Provides comprehensive frame timing information for the game loop, including
/// delta time, total elapsed time, fixed timestep management, and interpolation
/// alpha for smooth rendering.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FrameTime"/> class manages all timing aspects of the game loop,
/// supporting both fixed and variable timestep modes. It tracks accumulated time,
/// handles delta time capping to prevent the "spiral of death," and provides an
/// interpolation alpha value for smooth visual rendering between fixed updates.
/// </para>
/// <para>
/// Key features include:
/// <list type="bullet">
///   <item><description>Fixed and variable timestep support</description></item>
///   <item><description>Delta time capping to prevent performance spirals</description></item>
///   <item><description>Interpolation alpha for smooth rendering</description></item>
///   <item><description>Time scaling for slow-motion or fast-forward effects</description></item>
///   <item><description>Frames per second (FPS) calculation</description></item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// protected override void OnUpdate(FrameTime time)
/// {
///     // Move at a consistent speed regardless of frame rate
///     float speed = 100f * time.DeltaTime;
///     position += new Vect2(speed, 0);
/// }
/// 
/// protected override void OnDraw(FrameTime time)
/// {
///     // Interpolate position for smooth rendering
///     float renderX = MathHelper.Lerp(prevX, currentX, time.Alpha);
///     spriteBatch.Draw(texture, new Vect2(renderX, 100), Color.White);
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class FrameTime
{
    private TimeSpan _totalTime;
    private TimeSpan _elapsedTime;
    private float _accumulator;
    private bool _isRunningSlowly;
    private int _frameCount;
    private float _fpsTimer;
    private float _fps;

    /// <summary>
    /// Gets the total elapsed time since the game started.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the total duration the game has been running.
    /// This value includes all accumulated frame times and is unaffected by time scaling.
    /// </value>
    public TimeSpan TotalTime => _totalTime;

    /// <summary>
    /// Gets the elapsed time for the current frame.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the duration of the current frame.
    /// In fixed timestep mode, this returns the fixed interval. In variable timestep
    /// mode, this returns the actual frame duration.
    /// </value>
    public TimeSpan ElapsedTime => _elapsedTime;

    /// <summary>
    /// Gets a value indicating whether the game is running slowly due to performance issues.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the raw delta time exceeded <see cref="MaxDeltaTime"/>
    /// and was clamped; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This property is useful for detecting performance problems and adjusting
    /// game behavior accordingly, such as reducing visual quality or skipping
    /// non-critical updates.
    /// </remarks>
    public bool IsRunningSlowly => _isRunningSlowly;

    /// <summary>
    /// Gets the current frames per second (FPS) of the game.
    /// </summary>
    /// <value>
    /// A floating-point value representing the number of frames rendered per second,
    /// calculated as a moving average over one-second intervals.
    /// </value>
    public float FPS => _fps;

    /// <summary>
    /// Gets the current accumulation of time for fixed timestep updates.
    /// </summary>
    /// <value>
    /// The total accumulated time in seconds that has not yet been consumed by fixed updates.
    /// This value is used internally to determine how many fixed updates to run.
    /// </value>
    public float Accumulator => _accumulator;

    /// <summary>
    /// Gets the interpolation alpha factor for smooth rendering between fixed timestep updates.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0 representing how far between two fixed updates the current frame falls.
    /// Returns 1.0 when using variable timestep.
    /// </value>
    /// <remarks>
    /// <para>
    /// Alpha represents the interpolation factor between the previous and current
    /// fixed update states. This allows for smooth visual interpolation of positions,
    /// rotations, and other visual properties.
    /// </para>
    /// <para>
    /// <b>When to use:</b>
    /// <list type="bullet">
    ///   <item><description>Rendering positions for sprites, UI elements, and entities</description></item>
    ///   <item><description>Animation blending between frames</description></item>
    ///   <item><description>Camera smoothing and following</description></item>
    ///   <item><description>Particle system interpolation</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>When NOT to use:</b>
    /// <list type="bullet">
    ///   <item><description>Physics calculations (use <see cref="DeltaTime"/> instead)</description></item>
    ///   <item><description>Game logic updates (use <see cref="DeltaTime"/> instead)</description></item>
    ///   <item><description>Input processing (use raw input values)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Alpha values:</b>
    /// <list type="bullet">
    ///   <item><description><c>0.0</c> - At the beginning of the fixed timestep</description></item>
    ///   <item><description><c>0.5</c> - Halfway between two fixed updates</description></item>
    ///   <item><description><c>1.0</c> - At the end of the fixed timestep (next update is due)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// <code>
    /// protected override void OnDraw(FrameTime time)
    /// {
    ///     // Interpolate between previous and current position
    ///     float renderX = MathHelper.Lerp(_prevPosition.X, _currentPosition.X, time.Alpha);
    ///     spriteBatch.Draw(texture, new Vect2(renderX, 100), Color.White);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <b>Note:</b> When using variable timestep (<c>IsFixedTimeStep = false</c>),
    /// alpha will always return <c>1.0</c> as interpolation is not needed because
    /// there is a 1:1 relationship between updates and renders.
    /// </para>
    /// </remarks>
    public float Alpha
    {
        get
        {
            if (!IsFixedTimeStep || TargetElapsed <= 0f)
                return 1f;

            return Math.Clamp(_accumulator / TargetElapsed, 0f, 1f);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the game is using a fixed timestep.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if updates occur at a fixed interval independent of frame rate;
    /// <see langword="false"/> if updates occur once per rendered frame using actual elapsed time.
    /// </value>
    /// <remarks>
    /// Fixed timestep mode provides deterministic behavior and is recommended for
    /// physics simulations and networked games. Variable timestep mode provides
    /// smoother rendering but can lead to inconsistent behavior at different frame rates.
    /// </remarks>
    public bool IsFixedTimeStep { get; }

    /// <summary>
    /// Gets the target elapsed time per update in seconds.
    /// </summary>
    /// <value>
    /// The desired duration of each fixed update in seconds. Default is typically 1/60.
    /// </value>
    public float TargetElapsed { get; }

    /// <summary>
    /// Gets the maximum allowed delta time in seconds.
    /// </summary>
    /// <value>
    /// The cap on delta time to prevent the "spiral of death" when the game
    /// runs slowly. Default is typically 0.1 seconds.
    /// </value>
    public float MaxDeltaTime { get; }

    /// <summary>
    /// Gets or sets the time scale multiplier for the game.
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///   <item><description><c>1.0</c> - Normal speed (default)</description></item>
    ///   <item><description><c>0.5</c> - Half speed (slow motion)</description></item>
    ///   <item><description><c>2.0</c> - Double speed (fast-forward)</description></item>
    ///   <item><description><c>0.0</c> - Paused (no updates)</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// The time scale affects <see cref="DeltaTime"/> but does not affect
    /// <see cref="UnscaledDeltaTime"/>. This allows for global speed control
    /// of the game while maintaining access to real time for systems that should
    /// not be affected by time scaling.
    /// </para>
    /// <para>
    /// Examples of when to use:
    /// <list type="bullet">
    ///   <item><description>Pause menus (set to 0.0)</description></item>
    ///   <item><description>Slow-motion effects (set to 0.1 - 0.5)</description></item>
    ///   <item><description>Fast-forward (set to 2.0+)</description></item>
    ///   <item><description>Visual feedback for game events</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Systems that should NOT be affected by time scale:
    /// <list type="bullet">
    ///   <item><description>Audio playback</description></item>
    ///   <item><description>Network synchronization</description></item>
    ///   <item><description>Debug timers</description></item>
    ///   <item><description>UI animations that should always run at normal speed</description></item>
    /// </list>
    /// For these systems, use <see cref="UnscaledDeltaTime"/> instead.
    /// </para>
    /// </remarks>
    public float TimeScale { get; set; }

    /// <summary>
    /// Gets the scaled delta time for the current frame.
    /// </summary>
    /// <value>
    /// The elapsed time in seconds multiplied by <see cref="TimeScale"/>.
    /// This value should be used for most game logic and updates.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is the primary delta time value that should be used for:
    /// <list type="bullet">
    ///   <item><description>Movement calculations</description></item>
    ///   <item><description>Physics updates</description></item>
    ///   <item><description>Animation progression</description></item>
    ///   <item><description>Timed events</description></item>
    ///   <item><description>Game logic updates</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The value is affected by <see cref="TimeScale"/>, allowing for global
    /// speed control of the game.
    /// </para>
    /// </remarks>
    public float DeltaTime => (float)_elapsedTime.TotalSeconds * TimeScale;

    /// <summary>
    /// Gets the unscaled delta time for the current frame.
    /// </summary>
    /// <value>
    /// The actual elapsed time in seconds without any time scaling applied.
    /// This value always reflects real time.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this value for systems that should run independently of time scale:
    /// <list type="bullet">
    ///   <item><description>Audio engine updates</description></item>
    ///   <item><description>Network synchronization</description></item>
    ///   <item><description>Debugging and profiling</description></item>
    ///   <item><description>Real-time counters</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Most game logic should use <see cref="DeltaTime"/> instead to respect
    /// time scaling.
    /// </para>
    /// </remarks>
    public float UnscaledDeltaTime => (float)_elapsedTime.TotalSeconds;

    internal FrameTime()
    {
        var settings = GameSettings.Instance;

        IsFixedTimeStep = settings.IsFixedTimeStep;
        TargetElapsed = settings.TargetElapsedTime;
        MaxDeltaTime = settings.MaxDeltaTime;

        _totalTime = TimeSpan.Zero;
        _elapsedTime = TimeSpan.Zero;
        _accumulator = 0f;
        TimeScale = 1f;
    }

    internal void Update(float rawDelta)
    {
        if (rawDelta > MaxDeltaTime)
        {
            _isRunningSlowly = true;
            rawDelta = MaxDeltaTime;
        }
        else
        {
            _isRunningSlowly = false;
        }

        if (IsFixedTimeStep)
        {
            _elapsedTime = TimeSpan.FromSeconds(TargetElapsed);
            _accumulator += rawDelta;
        }
        else
        {
            _elapsedTime = TimeSpan.FromSeconds(rawDelta);
            _accumulator = 0f;
        }

        _totalTime += _elapsedTime;

        _fpsTimer += rawDelta;
        _frameCount++;
        if (_fpsTimer >= 1f)
        {
            _fps = _frameCount;
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    internal void ConsumeFixedUpdate()
    {
        if (IsFixedTimeStep)
            _accumulator -= TargetElapsed;
    }
}