namespace MiniPac.Entities;

public sealed class PacMan(Vect2 position) : Entity(position)
{
    public override void Update(FrameTime frameTime)
    {
        var input = InputAction.GetState();
        var velocity = Vect2.Zero;

        if (input.IsPressed(GameInputs.MoveUp))
            velocity.Y = -1;
        else if (input.IsPressed(GameInputs.MoveDown))
            velocity.Y = 1;
        else if (input.IsPressed(GameInputs.MoveLeft))
            velocity.X = -1;
        else if (input.IsPressed(GameInputs.MoveRight))
            velocity.X = 1;

        if (velocity != Vect2.Zero)
        {
            if (App.HasCollded(Location + velocity))
                return;

            Position += velocity * Globals.TileSize;
            BeaconManager.Instance.Publish(GameBeacons.PlayerMoved, this);
        }

        Globals.Camera.Position = Position + (Vect2.One * Globals.TileSize / 2);

        base.Update(frameTime);
    }

    public override void Draw(PrimitiveBatcher batch)
    {
        batch.DrawRect(Position, Size, Color.Yellow, 0.5f);

        base.Draw(batch);
    }
}
