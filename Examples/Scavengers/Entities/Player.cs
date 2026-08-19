namespace Scavengers.Entities;

public sealed class Player(LDtkEntityInstance inst) : Entity(inst)
{
    private enum PlayerAnims { None, Idle, Attack, Hit, GameOver }

    private Animator<PlayerAnims> _anim;
    private int _direction = 1;
    private bool _canMove = true;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerHit, OnPlayerHit);
        BeaconManager.Instance.Subscribe(GameBecaons.GameOver, OnGameover);

        var idleAnim = Globals.Sheet.GetBounds(
            "PlayerIdle0", "PlayerIdle1", "PlayerIdle2", "PlayerIdle3", "PlayerIdle4", "PlayerIdle5");
        var attackAnim = Globals.Sheet.GetBounds("PlayerAttack0", "PlayerAttack1");
        var hitAnim = Globals.Sheet.GetBounds("PlayerHit0", "PlayerHit1");
        var gameoverAnim = Globals.Sheet.GetBounds("PlayerHit1", "PlayerHit0");

        _anim = new Animator<PlayerAnims>(Globals.Texture) { AnimFinished = OnAnimFinished }
            .Add(PlayerAnims.Idle, [.. idleAnim], 8f, true)
            .Add(PlayerAnims.Attack, [.. attackAnim], 8f, false)
            .Add(PlayerAnims.Hit, [.. hitAnim], 8f, false)
            .Add(PlayerAnims.GameOver, [.. gameoverAnim], 8f, false)
            .Play(PlayerAnims.Idle, true)
            ;

        base.OnEnter();
    }


    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerHit, OnPlayerHit);

        base.OnExit();
    }


    private void OnGameover(BeaconHandle handle)
        => _anim.Play(PlayerAnims.GameOver, false);
    private void OnPlayerHit(BeaconHandle handle)
    {
        Globals.Die.PlayAndForget(Globals.SoundFxVolume);
        _anim.Play(PlayerAnims.Hit, true);
    }

    private void OnAnimFinished(PlayerAnims current, Animation<PlayerAnims> animation)
    {
        if (current == PlayerAnims.GameOver)
            return;

        _anim.Play(PlayerAnims.Idle, true);
        _canMove = true;
    }

    public override void OnUpdate(FrameTime frameTime)
    {
        var state = InputAction.GetState();
        var vel = Vect2.Zero;

        if (_canMove && !IsLocked && !IsMoving)
        {
            if (state.IsHeld(GameInputs.MoveUp))
            {
                vel.Y = -1;
            }
            else if (state.IsHeld(GameInputs.MoveRight))
            {
                vel.X = 1;
                _direction = 1;
            }
            else if (state.IsHeld(GameInputs.MoveDown))
            {
                vel.Y = 1;
            }
            else if (state.IsHeld(GameInputs.MoveLeft))
            {
                vel.X = -1;
                _direction = -1;
            }
            else if (state.IsPressed(GameInputs.Interact))
            {
                SoundHelper.PlayRandom([Globals.Chop1, Globals.Chop2], Globals.SoundFxVolume);
                _anim.Play(PlayerAnims.Attack, true);

                BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.PlayerAttackFoodReduction);
                BeaconManager.Instance.Publish(GameBecaons.PlayerInteract, this);
                BeaconManager.Instance.Publish(GameBecaons.PlayerMoved, this);
                _canMove = false;
            }
        }

        if (vel != Vect2.Zero)
        {
            if (!App.HasCollded(Location + vel))
            {
                SetPath(vel + Location);
            }
        }

        Globals.Camera.Position = Position + Vect2.One * Globals.TileSize / 2f;

        _anim.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        var effects = _direction > 0 ? TextureEffects.None : TextureEffects.Horizontal;

        _anim.Draw(batch, Position, effects, Globals.PlayerDepth);

        base.OnDraw(batch, frameTime);
    }
}
