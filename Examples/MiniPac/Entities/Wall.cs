namespace MiniPac.Entities;

public sealed class Wall : Entity
{
    public Wall(Vect2 position) : base(position)
    {
        BeaconManager.Instance.Publish(GameBeacons.Wall, Location);
    }

    public override void Draw(PrimitiveBatcher batch)
    {
        batch.DrawRect(Position, Size, Color.Blue, 0.2f);

        base.Draw(batch);
    }
}
