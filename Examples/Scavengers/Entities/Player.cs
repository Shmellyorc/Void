namespace Scavengers.Entities;

public sealed class Player(LDtkEntityInstance inst) : Entity(inst)
{
    private enum PlayerAnims { Idle, Attack, Hit }

    private Animator _anim;
    private int _direction = 1;

    public override void OnEnter()
    {
        var idleAnim = Globals.Sheet.GetBounds(
            "PlayerIdle0", "PlayerIdle1", "PlayerIdle2", "PlayerIdle3", "PlayerIdle4", "PlayerIdle5");
        var attackAnim = Globals.Sheet.GetBounds("PlayerAttack0", "PlayerAttack1");
        var hitAnim = Globals.Sheet.GetBounds("PlayerHit0", "PlayerHit1");

        _anim = new Animator(Globals.Texture)
            .Add(PlayerAnims.Idle, [.. idleAnim], 8f, true)
            .Add(PlayerAnims.Attack, [.. attackAnim], 8f, false)
            .Add(PlayerAnims.Hit, [.. hitAnim], 8f, false)
            .Play(PlayerAnims.Idle, true)
            ;

        base.OnEnter();
    }

    public override void OnUpdate(FrameTime frameTime)
    {
        var state = InputAction.GetState();
        var vel = Vect2.Zero;

        if (!IsLocked && !IsMoving)
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
                BeaconManager.Instance.Publish(GameBecaons.PlayerInteract, this);
        }

        if (vel != Vect2.Zero)
        {
            // if (!App.HasCollded(vel + Location))
            // {
            //     // Position += vel * Globals.TileSize;
            //     // BeaconManager.Instance.Publish(GameBecaons.PlayerMoved, this);
            // }
            SetPath(vel + Location);
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
