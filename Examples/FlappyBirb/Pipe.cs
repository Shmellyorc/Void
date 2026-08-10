namespace FlappyBirb;

public sealed class Pipe
{
    private const float Gap = 60;

    private readonly Rect2 _topPipeRect = new(302, 0, 26, 135);
    private readonly Rect2 _bottomPipeRect = new(330, 0, 26, 135);
    private readonly Texture _texture;

    public Vect2 Position { get; set; }

    public Pipe(Vect2 position)
    {
        Position = position;

        _texture = AssetManager.Instance.Load<Texture>("Spritesheet.png");
    }

    public void Draw(SpriteBatcher batch)
    {
        var top = new Vect2(
            Position.X - _topPipeRect.Width / 2,
            Position.Y - (_topPipeRect.Height + (Gap / 2))
        );
        var bottom = new Vect2(
            Position.X - _bottomPipeRect.Width / 2,
            Position.Y + Gap / 2
        );

        batch.Draw(_texture, top, _topPipeRect, Color.White, 0.8f);
        batch.Draw(_texture, bottom, _bottomPipeRect, Color.White, 0.8f);
    }
}
