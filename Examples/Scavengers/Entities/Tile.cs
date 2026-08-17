namespace Scavengers.Entities;

public sealed class Tile(Vect2 position, Rect2 source, TextureEffects effects) : Entity(position)
{
    private readonly Rect2 _source = source;
    private readonly TextureEffects _effects = effects;

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        batch.Draw(Globals.Texture, Position, _source, Color.White, 0f, Vect2.One, Vect2.Zero, _effects, 0f);

        base.OnDraw(batch, frameTime);
    }
}
