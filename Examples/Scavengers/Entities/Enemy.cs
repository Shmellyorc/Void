// ============================================================================
//  Enemy.cs - Scavengers Demo Enemy Entity
// ============================================================================
//  Enemies are hostile entities that chase the player and attack when adjacent.
//  They use AStar pathfinding to navigate toward the player.
//
//  The demo shows how to:
//  - Implement enemy AI with pathfinding
//  - Use different enemy types (Normal, Enraged) with different visuals
//  - Handle combat with the player
//  - Play different sounds for different enemy types
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// A hostile enemy that chases the player and attacks when adjacent.
/// </summary>
/// <remarks>
/// Enemy types:
/// - Normal: Standard enemy with normal animations
/// - Enraged: More aggressive, different visual appearance
/// - Random: Randomly picks between Normal and Enraged
/// 
/// Enemy behavior:
/// 1. When the player moves, the enemy calculates a path to the player
/// 2. If adjacent, the enemy attacks instead of moving
/// 3. The enemy moves one tile at a time along the path
/// </remarks>
public sealed class Enemy(LDtkEntityInstance inst) : Entity(inst)
{
    /// <summary>
    /// Enemy types that affect visual appearance.
    /// </summary>
    private enum EnemyType
    {
        None,
        Normal,
        Enraged,
        Random
    }

    /// <summary>
    /// Animation states for the enemy.
    /// </summary>
    private enum AnimType
    {
        Idle,
        Attack
    }

    // ============================================================================
    // Enemy Animations
    // Normal enemy (Type A) and Enraged enemy (Type B) have different sprites
    // ============================================================================

    // Normal enemy (Type A) animations
    private static readonly IReadOnlyList<Rect2> _enemyAIdle
        = Globals.Sheet.GetBounds("EnemyAIdle0", "EnemyAIdle1", "EnemyAIdle2", "EnemyAIdle3", "EnemyAIdle4", "EnemyAIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyAAttack = Globals.Sheet.GetBounds("EnemyAAttack0", "EnemyAAttack1");

    // Enraged enemy (Type B) animations
    private static readonly IReadOnlyList<Rect2> _enemyBIdle
        = Globals.Sheet.GetBounds("EnemyBIdle0", "EnemyBIdle1", "EnemyBIdle2", "EnemyBIdle3", "EnemyBIdle4", "EnemyBIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyBAttack = Globals.Sheet.GetBounds("EnemyBAttack0", "EnemyBAttack1");

    private EnemyType _type;
    private Animator<AnimType> _anim;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    public override void OnEnter()
    {
        // Subscribe to PlayerMoved events to react to player movement
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        // ========================================================================
        // Determine the enemy type from LDtk settings
        // ========================================================================
        _type = LDtkSetting.GetEnumSetting<EnemyType>(Settings, "Type");

        if (_type == EnemyType.Random)
        {
            var types = Enum
                .GetValues<EnemyType>()
                .Where(x => x != EnemyType.Random && x != EnemyType.None);

            _type = FastRandom.Shared.Choice(types);
        }

        // ========================================================================
        // Select the appropriate animations based on the enemy type
        // ========================================================================
        var idle = _type switch
        {
            EnemyType.Normal => _enemyAIdle.ToArray(),
            EnemyType.Enraged => _enemyBIdle.ToArray(),
            _ => throw new InvalidOperationException($"Unable to detect idle animation for: '{_type}'.")
        };

        var attack = _type switch
        {
            EnemyType.Normal => _enemyAAttack.ToArray(),
            EnemyType.Enraged => _enemyBAttack.ToArray(),
            _ => throw new InvalidOperationException($"Unable to detect attack animation for: '{_type}'.")
        };

        // ========================================================================
        // Setup the animator
        // ========================================================================
        _anim = new Animator<AnimType>(Globals.Texture)
        {
            // When attack animation finishes, return to idle
            AnimFinished = (_, _) => _anim.Play(AnimType.Idle, true)
        }
        .Add(AnimType.Idle, idle, 8f, true)        // Looping idle animation
        .Add(AnimType.Attack, attack, 8f, false)   // One-shot attack animation
        .Play(AnimType.Idle, false);

        base.OnEnter();
    }

    // ============================================================================
    // AI Behavior
    // ============================================================================

    /// <summary>
    /// Called when the player moves. Updates the enemy's path or attacks.
    /// </summary>
    private void OnPlayerMoved(BeaconHandle handle)
    {
        // Don't recalculate path if already moving
        if (IsMoving) return;

        var player = handle.Get<Player>(0);

        // Calculate the path from the enemy to the player
        // The path excludes the start and end positions
        var path = App.GetPath(Location, player.Location);

        // ========================================================================
        // Attack if adjacent to the player
        // ========================================================================
        if (MapHelper.IsUnitAround(player.Location, Location, false))
        {
            // Play the attack animation
            _anim.Play(AnimType.Attack, false);

            // Publish events for the attack
            BeaconManager.Instance.Publish(GameBecaons.PlayerHit);
            BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.EnemyFoodReduction);
            return;
        }

        // ========================================================================
        // No path, or already at the target
        // ========================================================================
        if (path.IsEmpty())
            return;

        // ========================================================================
        // Face the direction of the player
        // ========================================================================
        var dir = player.Location - Location;

        if (dir.X != 0)
            Direction = (int)dir.X;

        // ========================================================================
        // Move to the next tile on the path
        // ========================================================================
        // The enemy only moves one tile at a time (the first tile in the path)
        SetPath(path[0]);
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
        // Flip the sprite horizontally if the enemy is facing left
        var effects = Direction < 0 ? TextureEffects.Horizontal : TextureEffects.None;

        // Draw the enemy at the enemy depth (between tiles and player)
        _anim.Draw(batch, Position, effects, Globals.EnemyDepth);

        base.OnDraw(batch, frameTime);
    }
}