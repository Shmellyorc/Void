namespace MiniPac.Entities;

public sealed class Food : Entity
{
    public Food(Vect2 position) : base(position)
    {
        BeaconManager.Instance.Subscribe(GameBeacons.PlayerMoved, OnPlayerMoved);

        BeaconManager.Instance.Publish(GameBeacons.Food, Location);
    }

    private void OnPlayerMoved(BeaconHandle handle)
    {
        if (handle.Get<PacMan>(0).Location != Location)
            return;

        Destroy();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBeacons.PlayerMoved, OnPlayerMoved);

        base.OnExit();
    }

    public override void Draw(PrimitiveBatcher batch)
    {
        batch.DrawRect(Position, Size, Color.Green, 0.1f);

        base.Draw(batch);
    }
}
