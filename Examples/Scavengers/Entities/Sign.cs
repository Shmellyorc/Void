// ============================================================================
//  Sign.cs - Scavengers Demo Sign Entity (Level Transition Trigger)
// ============================================================================
//  Signs are interactive objects that trigger level transitions when the player
//  steps on them. They are the exit point for each level.
//
//  The demo shows how to:
//  - Create trigger-based interactions
//  - Handle player movement events via beacons
//  - Transition between levels
//  - Lock units during transitions
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// A sign that triggers a level transition when the player walks over it.
/// </summary>
/// <remarks>
/// When the player moves onto the sign's tile, the following happens:
/// 1. All units are locked (prevent movement during transition)
/// 2. A SceneTransition is added to the scene manager
/// 3. The transition handles fading out, loading the next level, and cleaning up
/// </remarks>
public sealed class Sign(LDtkEntityInstance inst) : Entity(inst)
{
    // ============================================================================
    // Lifecycle
    // ============================================================================

    public override void OnEnter()
    {
        // Subscribe to PlayerMoved events to detect when the player steps on this tile
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        base.OnEnter();
    }

    public override void OnExit()
    {
        // Clean up the subscription when the sign is removed
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        base.OnExit();
    }

    // ============================================================================
    // Interaction
    // ============================================================================

    /// <summary>
    /// Called when the player moves. Checks if the player is on the sign's tile.
    /// </summary>
    private void OnPlayerMoved(BeaconHandle handle)
    {
        var player = handle.Get<Player>(0);

        // Only trigger if the player is exactly on this sign's tile
        if (player.Location == Location)
        {
            // Lock all units so they don't move during the transition
            BeaconManager.Instance.Publish(GameBecaons.LockUnits);

            // Add the transition scene (which will load the next level)
            SceneManager.Instance.Add(new SceneTransition());
        }
    }

    // ============================================================================
    // Drawing
    // ============================================================================

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        // Draw the sign sprite at the default depth
        // The sign uses the "Sign" sprite from the spritesheet
        batch.Draw(
            Globals.Texture,
            Position,
            Globals.Sheet.GetBound("Sign"),
            Color.White,
            Globals.DefaultDepth
        );

        base.OnDraw(batch, frameTime);
    }
}