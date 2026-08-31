// ============================================================================
//  Animator.cs - Scavengers Demo Animation System
// ============================================================================
//  This file contains the animation system used for all game entities.
//  It supports frame-based sprite animations with support for looping,
//  speed control, and animation callbacks.
//
//  The demo shows how to:
//  - Define animations from spritesheet frames
//  - Play, stop, and loop animations
//  - Handle animation completion events
//  - Draw animated sprites with flips
// ============================================================================

namespace Scavengers;

/// <summary>
/// Represents a single animation definition.
/// </summary>
/// <typeparam name="TEnum">The enum type used to identify animations.</typeparam>
public readonly struct Animation<TEnum>
{
    /// <summary>The animation identifier.</summary>
    public TEnum Type { get; }

    /// <summary>The sprite frames that make up this animation.</summary>
    public Rect2[] Sources { get; }

    /// <summary>The playback speed (frames per second).</summary>
    public float Speed { get; }

    /// <summary>True if the animation loops when it reaches the end.</summary>
    public bool Looped { get; }

    public Animation(TEnum type, Rect2[] sources, float speed, bool looped)
    {
        Type = type;
        Sources = sources;
        Speed = speed;
        Looped = looped;
    }
}

/// <summary>
/// Manages sprite animations for entities.
/// Supports multiple animations per entity with smooth transitions.
/// </summary>
/// <typeparam name="TEnum">The enum type used to identify animations.</typeparam>
/// <remarks>
/// The Animator is designed to work with spritesheets. Each animation is a set
/// of frames (rectangles) from the spritesheet. The animator handles frame
/// timing, looping, and completion callbacks.
/// </remarks>
public sealed class Animator<TEnum> where TEnum : Enum
{
    // All registered animations
    private readonly Dictionary<Enum, Animation<TEnum>> _anims = [];

    // The spritesheet texture used for all frames
    private readonly Texture _texture;

    // Current animation state
    private TEnum _current;
    private bool _playing;
    private float _delta;
    private int _frame;

    /// <summary>
    /// Called when a non-looping animation completes.
    /// </summary>
    public Action<TEnum, Animation<TEnum>> AnimFinished { get; set; }

    /// <summary>
    /// Creates a new Animator using the specified spritesheet texture.
    /// </summary>
    public Animator(Texture texture)
    {
        _texture = texture;
    }

    /// <summary>
    /// Registers an animation with the animator.
    /// </summary>
    /// <param name="name">The animation identifier.</param>
    /// <param name="rects">The sprite frames in order.</param>
    /// <param name="speed">Playback speed (frames per second).</param>
    /// <param name="looped">True if the animation should loop.</param>
    public Animator<TEnum> Add(TEnum name, Rect2[] rects, float speed, bool looped)
    {
        // Don't add duplicate animations
        if (_anims.ContainsKey(name))
            return this;

        // Ignore empty animations
        if (rects.IsEmpty())
            return this;

        // Store the animation definition
        _anims[name] = new Animation<TEnum>(name, rects, speed, looped);

        return this;
    }

    /// <summary>
    /// Starts playing an animation.
    /// </summary>
    /// <param name="name">The animation to play.</param>
    /// <param name="repeat">If true, restarts a looping animation.</param>
    public Animator<TEnum> Play(TEnum name, bool repeat)
    {
        // Check if the animation exists
        if (!_anims.TryGetValue(name, out var result))
            return this;

        // Don't restart if already playing and not forced to repeat
        if (repeat && _current.Equals(name))
            return this;

        // Reset frame counter and timer
        _frame = 0;
        _delta = 0;

        _current = name;
        _playing = true;

        return this;
    }

    /// <summary>
    /// Updates the animation timer and advances frames.
    /// Called every frame while the animation is playing.
    /// </summary>
    public void Update(FrameTime frameTime)
    {
        // Don't update if not playing or animation doesn't exist
        if (!_playing || !_anims.TryGetValue(_current, out var anim))
            return;

        // Accumulate time
        _delta += frameTime.DeltaTime;

        // Check if it's time to advance to the next frame
        if (_delta > (1f / anim.Speed))
        {
            _delta -= 1f / anim.Speed;
            _frame++;

            // Check if we've reached the end of the animation
            if (_frame > anim.Sources.Length - 1)
            {
                if (anim.Looped)
                {
                    // Loop back to the start
                    _frame = 0;
                }
                else
                {
                    // Stay on the last frame and stop playing
                    _frame = anim.Sources.Length - 1;
                    _playing = false;

                    // Notify that the animation finished
                    AnimFinished?.Invoke(_current, anim);
                }
            }
        }
    }

    /// <summary>
    /// Draws the current animation frame.
    /// </summary>
    public void Draw(SpriteBatcher batch, Vect2 position, TextureEffects effects, float depth)
    {
        // Don't draw if animation doesn't exist
        if (!_anims.TryGetValue(_current, out var anim))
            return;

        // Clamp the frame to valid range
        var frame = Math.Clamp(_frame, 0, anim.Sources.Length - 1);
        var rect = anim.Sources[frame];

        // Draw the sprite using the batcher
        batch.Draw(_texture, position, rect, Color.White, 0f, Vect2.One, Vect2.Zero, effects, depth);
    }
}