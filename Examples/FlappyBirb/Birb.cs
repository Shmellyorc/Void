// ============================================================================
//  Birb.cs
// ============================================================================
//  The player character for FlappyBirb.
//
//  This little guy has simple physics:
//    - Gravity pulls him down every frame
//    - Pressing Space or Left Click gives him an upward boost
//    - He rotates up when flapping and noses down when falling
//    - He dies if he hits a pipe, the ground, or flies too high
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================

namespace FlappyBirb;

/// <summary>
/// The bird. Handles its own physics, input, animation, and collision.
/// </summary>
public sealed class Birb
{
    // Animation
    private const float AnimSpeed = 6f;              // Frames per second for wing flap animation


    // Physics
    private const float Gravity = 600f;              // How fast the bird falls (pixels per second squared)
    private const float FlapImpulse = -180f;         // Upward speed when flapping (negative = up)
    private const float MaxFallSpeed = 250f;         // Fastest the bird can fall (terminal velocity)


    // Rotation
    private const float RotationSpeed = 180f;        // How fast the bird tilts (degrees per second)
    private const float MaxUpRotation = -25f;        // How far up the bird can tilt when flapping
    private const float MaxDownRotation = 90f;       // How far down the bird can tilt when diving


    // Animation frames from the spritesheet
    private readonly Rect2[] _anims = [.. Globals.Sheet.GetBounds("Bird0", "Bird1", "Bird2")];


    // Current state
    private float _rotate;          // Current rotation angle in degrees
    private float _delta;           // Time since last animation frame change
    private float _velocity;        // Current vertical speed (negative = moving up)
    private Vect2 _position;        // Top-left position of the bird sprite
    private int _frame;             // Current animation frame index


    // Input state (previous and current for "just pressed" detection)
    private KeyboardState _keyState, _oldKeyState;
    private MouseState _mouseState, _oldMouseState;


    /// <summary>
    /// The bird's hitbox. Slightly smaller than the sprite so it feels fair.
    /// </summary>
    public Rect2 CollisionRect
    {
        get
        {
            // Use 70% of the sprite size for a forgiving hitbox
            var size = _anims[0].Size * 0.7f;
            
            return new Rect2(
                _position.X + (_anims[0].Size.X - size.X) / 2f,
                _position.Y + (_anims[0].Size.Y - size.Y) / 2f,
                size.X,
                size.Y
            );
        }
    }

    /// <summary>
    /// True when the bird has hit something and the game is over.
    /// </summary>
    public bool IsDead { get; private set; }

    /// <summary>
    /// True for one frame after the player flaps. Useful for sound effects or particles.
    /// </summary>
    public bool IsFlapping { get; private set; }

    /// <summary>
    /// The bird's current top-left position.
    /// </summary>
    public Vect2 Position => _position;

    /// <summary>
    /// Creates a new bird at the given position.
    /// </summary>
    public Birb(Vect2 position)
    {
        _position = position;
    }

    /// <summary>
    /// Updates the bird every frame. Call this from your game's OnUpdate.
    /// </summary>
    public void Update(FrameTime frameTime)
    {
        if (IsDead)
            return;

        // Save old input state so we can detect "just pressed"
        _oldKeyState = _keyState;
        _oldMouseState = _mouseState;

        // Get current input state
        _keyState = Keyboard.GetState();
        _mouseState = Mouse.GetState();

        IsFlapping = false;

        // Check if the player pressed Space or Left Click this frame
        bool flapPressed =
            (_oldKeyState.IsKeyUp(KeyboardKey.Space) && _keyState.IsKeyDown(KeyboardKey.Space)) ||
            (_oldMouseState.IsButtonReleased(MouseButton.Left) && _mouseState.IsButtonPressed(MouseButton.Left));

        if (flapPressed)
        {
            Flap();
        }

        // Apply gravity to pull the bird down
        _velocity += Gravity * frameTime.DeltaTime;

        // Don't let the bird fall faster than MaxFallSpeed
        _velocity = MathF.Min(_velocity, MaxFallSpeed);

        // Move the bird based on its velocity
        _position.Y += _velocity * frameTime.DeltaTime;

        // Don't let the bird fly above the top of the screen
        if (_position.Y < 0f)
        {
            _position.Y = 0f;
            _velocity = MathF.Max(_velocity, 0f);  // Stop upward movement
        }

        // Check if the bird hit the ground
        float screenHeight = GameSettings.Instance.Viewport.Y;
        float groundY = screenHeight - Globals.Floor;
        float birdBottom = _position.Y + _anims[0].Size.Y;

        if (birdBottom >= groundY)
        {
            // Snap the bird to the ground and kill it
            _position.Y = groundY - _anims[0].Size.Y;
            Die();
            return;
        }

        // Rotate the bird based on what it's doing
        if (flapPressed || _velocity < 0f)
        {
            // Flapping or moving up = tilt up
            _rotate = MathF.Max(_rotate - RotationSpeed * frameTime.DeltaTime, MaxUpRotation);
        }
        else
        {
            // Falling = tilt down. Rotate faster as fall speed increases.
            float targetRotation = MathHelper.Remap(_velocity, 0f, MaxFallSpeed, 0f, MaxDownRotation);
            _rotate = MathHelper.MoveTowards(_rotate, targetRotation, RotationSpeed * frameTime.DeltaTime);
        }

        // Cycle through the animation frames
        UpdateAnimate(frameTime);
    }

    /// <summary>
    /// Gives the bird an upward boost.
    /// </summary>
    private void Flap()
    {
        _velocity = FlapImpulse;
        IsFlapping = true;
    }

    /// <summary>
    /// Kills the bird. Stops all movement.
    /// </summary>
    public void Die()
    {
        IsDead = true;
        _velocity = 0f;
    }

    /// <summary>
    /// Resets the bird for a new game.
    /// </summary>
    public void Reset(Vect2 position)
    {
        _position = position;
        _velocity = 0f;
        _rotate = 0f;
        _frame = 0;
        _delta = 0f;
        IsDead = false;
    }

    /// <summary>
    /// Checks if the bird's hitbox overlaps a pipe.
    /// </summary>
    public bool CollidesWith(Rect2 pipeRect)
    {
        return CollisionRect.Intersects(pipeRect);
    }

    /// <summary>
    /// Cycles through the wing flap animation frames.
    /// </summary>
    private void UpdateAnimate(FrameTime frameTime)
    {
        _delta += frameTime.DeltaTime;

        // Change frame when enough time has passed
        if (_delta > (1f / AnimSpeed))
        {
            _delta -= 1f / AnimSpeed;
            _frame++;

            // Loop back to the first frame
            if (_frame > _anims.Length - 1)
                _frame = 0;
        }
    }

    /// <summary>
    /// Checks if the bird went out of bounds (above ceiling or below ground).
    /// </summary>
    /// <param name="groundY">The Y position of the ground (top of the floor).</param>
    public bool IsOutOfBounds(float groundY)
    {
        // Above the screen
        if (_position.Y < 0f)
        {
            _position.Y = 0f;
            return true;
        }

        // Below the ground
        float birdBottom = _position.Y + _anims[0].Size.Y;
        if (birdBottom >= groundY)
        {
            _position.Y = groundY - _anims[0].Size.Y;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Draws the bird with its current rotation and animation frame.
    /// </summary>
    public void Draw(SpriteBatcher batch)
    {
        // Get the current animation frame
        var rect = _anims[Math.Min(_frame, _anims.Length - 1)];

        // Convert rotation from degrees to radians for the batcher
        float rotationRadians = _rotate * MathHelper.DegToRad;

        // Draw centered on the bird's position with rotation
        batch.Draw(Globals.Texture, _position, rect, Color.White, rotationRadians, Vect2.One, rect.Size / 2f, TextureEffects.None, 0.3f);
    }
}