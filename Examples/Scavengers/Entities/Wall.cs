namespace Scavengers.Entities;

public sealed class Wall(LDtkEntityInstance inst) : Entity(inst)
{
    private enum WallType { Normal, Damaged, Destroyed }

    private static readonly IReadOnlyList<Rect2> _wallA = Globals.Sheet.GetBounds("WallA0", "WallA1");
    private static readonly IReadOnlyList<Rect2> _wallB = Globals.Sheet.GetBounds("WallB0", "WallB1");
    private static readonly IReadOnlyList<Rect2> _wallC = Globals.Sheet.GetBounds("WallC0", "WallC1");
    private static readonly IReadOnlyList<Rect2> _wallD = Globals.Sheet.GetBounds("WallD0", "WallD1");
    private static readonly IReadOnlyList<Rect2> _wallE = Globals.Sheet.GetBounds("WallE0", "WallE1");
    private static readonly IReadOnlyList<Rect2> _wallF = Globals.Sheet.GetBounds("WallF0", "WallF1");

    private Animator _anim;
    private WallType _state;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerInteract, OnPlayerInteract);

        var result = FastRandom.Shared
            .Choice([_wallA, _wallB, _wallC, _wallD, _wallE, _wallF]);

        _anim = new Animator(Globals.Texture)
            .Add(WallType.Normal, [result[0]], 8f, false)
            .Add(WallType.Damaged, [result[1]], 8f, false)
            .Add(WallType.Destroyed, [Globals.Sheet.GetBound("Empty")], 8f, false)
            .Play(_state, false)
            ;

        App.SetCollision(Location, true);

        base.OnEnter();
    }

    private void OnPlayerInteract(BeaconHandle handle)
    {
        var player = handle.Get<Player>(0);

        if (IsDestroyed)
            return;
        if (!MapHelper.IsUnitAround(Location, player.Location, false))
            return;

        var newState = _state switch
        {
            WallType.Normal => WallType.Damaged,
            WallType.Damaged => WallType.Destroyed,
            _ => WallType.Destroyed,
        };

        _state = newState;
        _anim.Play(_state, false);

        if (_state == WallType.Destroyed)
            Destroy();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerInteract, OnPlayerInteract);

        App?.SetCollision(Location, false);

        base.OnExit();
    }

    public override void OnUpdate(FrameTime frameTime)
    {
        _anim.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        _anim.Draw(batch, Position, TextureEffects.None, Globals.DefaultDepth);

        base.OnDraw(batch, frameTime);
    }
}
