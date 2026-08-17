namespace Scavengers.Entities;

public sealed class Sign(LDtkEntityInstance inst) : Entity(inst)
{
    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        base.OnEnter();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        base.OnExit();
    }

    private void OnPlayerMoved(BeaconHandle handle)
    {
        var player = handle.Get<Player>(0);

        if (player.Location == Location)
        {
            BeaconManager.Instance.Publish(GameBecaons.LockUnits);

            SceneManager.Instance.Add(new SceneTransition());
        }
    }

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        batch.Draw(Globals.Texture, Position, Globals.Sheet.GetBound("Sign"), Color.White, Globals.DefaultDepth);

        base.OnDraw(batch, frameTime);
    }
}
