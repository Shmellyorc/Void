// ============================================================================
//  Wall.cs - Scavengers Demo Wall Entity
// ============================================================================
//  Walls are obstacles that block the player's movement. The player can attack
//  walls to destroy them, which removes the collision and opens the path.
//
//  The demo shows how to:
//  - Create interactive obstacles with multiple states
//  - Handle player interaction via beacons
//  - Use animations for different states
//  - Dynamically update the collision map
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// A destructible wall that blocks the player's path.
/// Walls have three states: Normal, Damaged, and Destroyed.
/// </summary>
/// <remarks>
/// Wall states:
/// - Normal: Fully intact, blocks movement
/// - Damaged: Visually damaged, still blocks movement
/// - Destroyed: Completely destroyed, no longer blocks movement
/// </remarks>
public sealed class Wall(LDtkEntityInstance inst) : Entity(inst)
{
    /// <summary>
    /// The current state of the wall.
    /// </summary>
    private enum WallType
    {
        Normal,
        Damaged,
        Destroyed
    }

    // ============================================================================
    // Wall Variations
    // Each wall has two sprites (Normal and Damaged)
    // We randomly select one of six variations when the wall is created
    // ============================================================================

    private static readonly IReadOnlyList<Rect2> _wallA = Globals.Sheet.GetBounds("WallA0", "WallA1");
    private static readonly IReadOnlyList<Rect2> _wallB = Globals.Sheet.GetBounds("WallB0", "WallB1");
    private static readonly IReadOnlyList<Rect2> _wallC = Globals.Sheet.GetBounds("WallC0", "WallC1");
    private static readonly IReadOnlyList<Rect2> _wallD = Globals.Sheet.GetBounds("WallD0", "WallD1");
    private static readonly IReadOnlyList<Rect2> _wallE = Globals.Sheet.GetBounds("WallE0", "WallE1");
    private static readonly IReadOnlyList<Rect2> _wallF = Globals.Sheet.GetBounds("WallF0", "WallF1");

    private Animator<WallType> _anim;
    private WallType _state;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    public override void OnEnter()
    {
        // Subscribe to the PlayerInteract beacon
        // This is published when the player attacks
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerInteract, OnPlayerInteract);

        // ========================================================================
        // Select a random wall variation
        // ========================================================================
        var result = FastRandom.Shared.Choice([_wallA, _wallB, _wallC, _wallD, _wallE, _wallF]);

        // ========================================================================
        // Setup the animator
        // ========================================================================
        _anim = new Animator<WallType>(Globals.Texture)
            .Add(WallType.Normal, [result[0]], 8f, false)      // Normal state sprite
            .Add(WallType.Damaged, [result[1]], 8f, false)     // Damaged state sprite
            .Add(WallType.Destroyed, [Globals.Sheet.GetBound("Empty")], 8f, false) // Destroyed (empty)
            .Play(_state, false);

        // ========================================================================
        // Block movement on this tile
        // ========================================================================
        App.SetCollision(Location, true);

        base.OnEnter();
    }

    /// <summary>
    /// Called when the scene is removed.
    /// Removes the collision from this tile.
    /// </summary>
    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerInteract, OnPlayerInteract);

        // Remove the collision when the wall is destroyed or the scene ends
        App?.SetCollision(Location, false);

        base.OnExit();
    }

    // ============================================================================
    // Interaction
    // ============================================================================

    /// <summary>
    /// Handles the PlayerInteract beacon.
    /// When the player interacts with this wall, it transitions to the next state.
    /// </summary>
    private void OnPlayerInteract(BeaconHandle handle)
    {
        var player = handle.Get<Player>(0);

        // Don't interact if the wall is already destroyed
        if (IsDestroyed)
            return;

        // The player must be adjacent to the wall (orthogonally)
        if (!MapHelper.IsUnitAround(Location, player.Location, false))
            return;

        // ========================================================================
        // Transition to the next state
        // ========================================================================
        var newState = _state switch
        {
            WallType.Normal => WallType.Damaged,
            WallType.Damaged => WallType.Destroyed,
            _ => WallType.Destroyed,
        };

        // Apply the new state
        _state = newState;
        _anim.Play(_state, false);

        // If destroyed, remove the wall and clear the collision
        if (_state == WallType.Destroyed)
        {
            Destroy();
        }
    }

    // ============================================================================
    // Update and Draw
    // ============================================================================

    public override void OnUpdate(FrameTime frameTime)
    {
        // Update the animation
        _anim.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        // Draw the wall at the default depth
        _anim.Draw(batch, Position, TextureEffects.None, Globals.DefaultDepth);

        base.OnDraw(batch, frameTime);
    }
}