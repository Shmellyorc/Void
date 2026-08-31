// ============================================================================
//  Food.cs - Scavengers Demo Food Entity
// ============================================================================
//  Food items are collectibles that restore the player's food count.
//  There are two types: Fruit (small amount) and Soda (large amount).
//
//  The demo shows how to:
//  - Create collectible items with different types
//  - Handle player collection via beacon events
//  - Play different sounds for different food types
//  - Use LDtk settings to configure entity properties
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// A collectible food item. The player collects food by walking over it.
/// Different food types provide different amounts of food points.
/// </summary>
/// <remarks>
/// Food types:
/// - Fruit: +4 food points
/// - Soda: +12 food points
/// - Random: Randomly selects between Fruit and Soda
/// </remarks>
public sealed class Food(LDtkEntityInstance inst) : Entity(inst)
{
    /// <summary>
    /// The type of food this entity represents.
    /// </summary>
    private enum FoodType
    {
        Soda,
        Fruit,
        Random
    }

    private FoodType _type;
    private Animator<FoodType> _anim;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    public override void OnEnter()
    {
        // Subscribe to PlayerMoved events to detect when the player walks onto this tile
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        // ========================================================================
        // Determine the food type from LDtk settings
        // ========================================================================
        _type = LDtkSetting.GetEnumSetting<FoodType>(Settings, "Type");

        // If Random, pick a random type (excluding Random itself)
        if (_type == FoodType.Random)
        {
            var types = Enum
                .GetValues<FoodType>()
                .Where(x => x != FoodType.Random);
            _type = FastRandom.Shared.Choice(types);
        }

        // ========================================================================
        // Setup the animator with the correct sprite
        // ========================================================================
        _anim = new Animator<FoodType>(Globals.Texture)
            .Add(FoodType.Fruit, [Globals.Sheet.GetBound("Fruit")], 8f, false)
            .Add(FoodType.Soda, [Globals.Sheet.GetBound("Soda")], 8f, false)
            .Play(_type, false);

        base.OnEnter();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        base.OnExit();
    }

    // ============================================================================
    // Collection
    // ============================================================================

    /// <summary>
    /// Called when the player moves. Checks if the player is on this tile.
    /// </summary>
    private void OnPlayerMoved(BeaconHandle handle)
    {
        var player = handle.Get<Player>(0);

        // Only collect if the player is exactly on this tile
        if (Location != player.Location)
            return;

        // ========================================================================
        // Determine the food points and play the appropriate sound
        // ========================================================================
        int points;
        switch (_type)
        {
            case FoodType.Soda:
                points = Globals.SodaPoints;
                SoundHelper.PlayRandom([Globals.Soda1, Globals.Soda2], Globals.SoundFxVolume);
                break;

            case FoodType.Fruit:
                points = Globals.FruitPoints;
                SoundHelper.PlayRandom([Globals.Fruit1, Globals.Fruit2], Globals.SoundFxVolume);
                break;

            default:
                throw new InvalidOperationException($"Unable to detect food type of: '{_type}'.");
        }

        // Publish the food update with the points earned
        BeaconManager.Instance.Publish(GameBecaons.UpdateFood, points);

        // Destroy the food entity (it disappears)
        Destroy();
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
        // Draw the food at the default depth
        _anim.Draw(batch, Position, TextureEffects.None, Globals.DefaultDepth);

        base.OnDraw(batch, frameTime);
    }
}