namespace FlappyBirb;

public sealed class Pipe
{
    private const float Gap = 60;

    private readonly Rect2 _topPipeRect = Global.Sheet.GetBounds("TopPipe");
    private readonly Rect2 _bottomPipeRect = Global.Sheet.GetBounds("BottomPipe");

    public Vect2 Position { get; set; }

    public Pipe(Vect2 position)
    {
        Position = position;
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

        batch.Draw(Global.Texture, top, _topPipeRect, Color.White, 0.8f);
        batch.Draw(Global.Texture, bottom, _bottomPipeRect, Color.White, 0.8f);
    }
}
