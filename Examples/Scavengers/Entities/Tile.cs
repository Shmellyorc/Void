// ============================================================================
//  Tile.cs - Scavengers Demo Static Tile Entity
// ============================================================================
//  Tiles are static background elements rendered from the LDtk map's tile layer.
//  They are non-interactive and purely visual.
//
//  The demo shows how to:
//  - Create static visual entities from tile data
//  - Render tiles with texture flips
//  - Use primary constructors for simple entities
// ============================================================================

namespace Scavengers.Entities;

/// <summary>
/// A static background tile rendered from the LDtk tile layer.
/// Tiles have no behavior or collision - they are purely visual.
/// </summary>
/// <remarks>
/// Tiles are created from the LDtk tile layer and drawn at their world position.
/// They support texture effects (horizontal flip, vertical flip, or both).
/// </remarks>
public sealed class Tile(Vect2 position, Rect2 source, TextureEffects effects) : Entity(position)
{
    // The source rectangle within the spritesheet
    private readonly Rect2 _source = source;

    // The texture effects to apply (flip horizontally, vertically, or both)
    private readonly TextureEffects _effects = effects;

    /// <summary>
    /// Draws the tile at its position with the specified effects.
    /// </summary>
    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        // Draw the tile using the global spritesheet texture
        // The source rectangle defines which part of the spritesheet to use
        // Effects can flip the tile horizontally or vertically
        batch.Draw(
            Globals.Texture,                // Spritesheet texture
            Position,                       // World position
            _source,                        // Source rectangle in the spritesheet
            Color.White,                    // Full opacity, no tinting
            0f,                             // No rotation
            Vect2.One,                      // No scaling
            Vect2.Zero,                     // No origin offset
            _effects,                       // Texture effects (flips)
            0f                              // Depth (0 = base layer)
        );

        base.OnDraw(batch, frameTime);
    }
}