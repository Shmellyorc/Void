namespace Void.Engine.Systems;

public sealed class FrameTime
{
    private TimeSpan _totalTime;
    private TimeSpan _elapsedTime;
    private float _accumulator;
    private bool _isRunningSlowly;
    private int _frameCount;
    private float _fpsTimer;
    private float _fps;

    public TimeSpan TotalTime => _totalTime;
    public TimeSpan ElapsedTime => _elapsedTime;
    public bool IsRunningSlowly => _isRunningSlowly;
    public float FPS => _fps;
    public float Accumulator => _accumulator;

    /// <summary>
    /// Gets the interpolation alpha factor for smooth rendering between fixed timestep updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Alpha represents how far between two fixed updates the current frame falls, 
    /// ranging from 0.0 to 1.0. This allows for smooth visual interpolation of 
    /// positions, rotations, and other visual properties.
    /// </para>
    /// <para>
    /// <b>When to use:</b>
    /// <list type="bullet">
    ///   <item>Rendering positions (sprites, UI, entities)</item>
    ///   <item>Animation blending</item>
    ///   <item>Camera smoothing</item>
    ///   <item>Particle interpolation</item>
    ///   <item>NOT for physics or game logic (use ElapsedTime or DeltaTime)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// <code>
    /// protected override void OnDraw()
    /// {
    ///     // Interpolate between previous and current position
    ///     float renderPos = _position + (_velocity * timing.DeltaTime * timing.Alpha);
    ///     spriteBatch.Draw(texture, new Vect2(renderPos, 100), Color.White);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <b>Alpha Values:</b>
    /// <list type="bullet">
    ///   <item><c>0.0</c> - Beginning of the fixed timestep</item>
    ///   <item><c>0.5</c> - Halfway between two fixed updates</item>
    ///   <item><c>1.0</c> - End of the fixed timestep (next update is due)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> When using variable timestep (<c>IsFixedTimeStep = false</c>), 
    /// alpha will always return <c>1.0</c> as interpolation is not needed.
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

    public bool IsFixedTimeStep { get; }
    public float TargetElapsed { get; }
    public float MaxDeltaTime { get; }

    /// <summary>
    /// Gets or sets the time scale multiplier. 1f = normal speed, 0.5f = half speed, 2f = double speed.
    /// </summary>
    public float TimeScale { get; set; }

    /// <summary>
    /// Gets the scaled delta time (affected by TimeScale).
    /// </summary>
    public float DeltaTime => (float)_elapsedTime.TotalSeconds * TimeScale;

    /// <summary>
    /// Gets the unscaled delta time (always real time, ignores TimeScale).
    /// </summary>
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
