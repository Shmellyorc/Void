// ============================================================================
//  Player.cs - Scavengers Demo Player Entity
// ============================================================================
//  The player is the main character controlled by the player. They can move in
//  four directions, attack enemies and walls, and collect food.
//
//  The demo shows how to:
//  - Handle player input with the InputAction system
//  - Animate the player with different states (idle, attack, hit, game over)
//  - Publish beacon events for game systems
//  - Move the camera to follow the player
//  - Handle player death and game over
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// The player character. Controlled by the player via keyboard or gamepad.
/// </summary>
/// <remarks>
/// Player actions:
/// - Movement: WASD or arrow keys (4-directional grid-based)
/// - Attack: E key or gamepad A button (attacks adjacent enemies/walls)
/// 
/// Each move and attack costs food. The player dies when food reaches zero.
/// </remarks>
public sealed class Player(LDtkEntityInstance inst) : Entity(inst)
{
    /// <summary>
    /// Animation states for the player.
    /// </summary>
    private enum PlayerAnims
    {
        None,
        Idle,
        Attack,
        Hit,
        GameOver
    }

    private Animator<PlayerAnims> _anim;
    private int _direction = 1;
    private bool _canMove = true;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    public override void OnEnter()
    {
        // Subscribe to game events
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerHit, OnPlayerHit);
        BeaconManager.Instance.Subscribe(GameBecaons.GameOver, OnGameover);

        // ========================================================================
        // Setup player animations
        // ========================================================================
        var idleAnim = Globals.Sheet.GetBounds(
            "PlayerIdle0", "PlayerIdle1", "PlayerIdle2", "PlayerIdle3", "PlayerIdle4", "PlayerIdle5");
        var attackAnim = Globals.Sheet.GetBounds("PlayerAttack0", "PlayerAttack1");
        var hitAnim = Globals.Sheet.GetBounds("PlayerHit0", "PlayerHit1");
        var gameoverAnim = Globals.Sheet.GetBounds("PlayerHit1", "PlayerHit0");

        _anim = new Animator<PlayerAnims>(Globals.Texture)
        {
            AnimFinished = OnAnimFinished  // Called when non-looping animations complete
        }
        .Add(PlayerAnims.Idle, [.. idleAnim], 8f, true)        // Looping idle animation
        .Add(PlayerAnims.Attack, [.. attackAnim], 8f, false)   // One-shot attack
        .Add(PlayerAnims.Hit, [.. hitAnim], 8f, false)         // One-shot hit reaction
        .Add(PlayerAnims.GameOver, [.. gameoverAnim], 8f, false) // One-shot game over
        .Play(PlayerAnims.Idle, true);

        base.OnEnter();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerHit, OnPlayerHit);

        base.OnExit();
    }

    // ============================================================================
    // Event Handlers
    // ============================================================================

    private void OnGameover(BeaconHandle handle)
        => _anim.Play(PlayerAnims.GameOver, false);

    private void OnPlayerHit(BeaconHandle handle)
    {
        Globals.Die.PlayAndForget(Globals.SoundFxVolume);  // Play death sound
        _anim.Play(PlayerAnims.Hit, true);                 // Play hit animation
    }

    /// <summary>
    /// Called when a non-looping animation completes.
    /// Returns the player to idle state after attack or hit.
    /// </summary>
    private void OnAnimFinished(PlayerAnims current, Animation<PlayerAnims> animation)
    {
        // Don't interrupt the game over animation
        if (current == PlayerAnims.GameOver)
            return;

        // Return to idle and allow movement again
        _anim.Play(PlayerAnims.Idle, true);
        _canMove = true;
    }

    // ============================================================================
    // Update
    // ============================================================================

    public override void OnUpdate(FrameTime frameTime)
    {
        var state = InputAction.GetState();
        var vel = Vect2.Zero;

        // Only process input if the player can move and isn't locked
        if (_canMove && !IsLocked && !IsMoving)
        {
            // ====================================================================
            // Movement Input
            // ====================================================================
            if (state.IsHeld(GameInputs.MoveUp))
            {
                vel.Y = -1;
            }
            else if (state.IsHeld(GameInputs.MoveRight))
            {
                vel.X = 1;
                _direction = 1;
            }
            else if (state.IsHeld(GameInputs.MoveDown))
            {
                vel.Y = 1;
            }
            else if (state.IsHeld(GameInputs.MoveLeft))
            {
                vel.X = -1;
                _direction = -1;
            }
            // ====================================================================
            // Attack Input
            // ====================================================================
            else if (state.IsPressed(GameInputs.Interact))
            {
                // Play attack sound
                SoundHelper.PlayRandom([Globals.Chop1, Globals.Chop2], Globals.SoundFxVolume);

                // Play attack animation
                _anim.Play(PlayerAnims.Attack, true);

                // Publish events for the attack
                BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.PlayerAttackFoodReduction);
                BeaconManager.Instance.Publish(GameBecaons.PlayerInteract, this);
                BeaconManager.Instance.Publish(GameBecaons.PlayerMoved, this);

                // Prevent movement during the attack animation
                _canMove = false;
            }
        }

        // ========================================================================
        // Movement
        // ========================================================================
        // Move the player if there is input and the destination is walkable
        if (vel != Vect2.Zero)
        {
            if (!App.HasCollded(Location + vel))
            {
                SetPath(vel + Location);
            }
        }

        // ========================================================================
        // Camera Follow
        // ========================================================================
        // Smoothly follow the player with the camera
        // Center the camera on the player's tile
        Globals.Camera.Position = Position + Vect2.One * Globals.TileSize / 2f;

        // Update the animation
        _anim.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    // ============================================================================
    // Drawing
    // ============================================================================

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        // Flip the sprite horizontally if the player is facing left
        var effects = _direction > 0 ? TextureEffects.None : TextureEffects.Horizontal;

        // Draw the player at the player depth (on top of most things)
        _anim.Draw(batch, Position, effects, Globals.PlayerDepth);

        base.OnDraw(batch, frameTime);
    }
}