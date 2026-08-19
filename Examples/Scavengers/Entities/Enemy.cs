namespace Scavengers.Entities;

public sealed class Enemy(LDtkEntityInstance inst) : Entity(inst)
{
    private enum EnemyType { None, Normal, Enraged, Random }
    private enum AnimType { Idle, Attack }

    private static readonly IReadOnlyList<Rect2> _enemyAIdle
        = Globals.Sheet.GetBounds("EnemyAIdle0", "EnemyAIdle1", "EnemyAIdle2", "EnemyAIdle3", "EnemyAIdle4", "EnemyAIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyAAttack = Globals.Sheet.GetBounds("EnemyAAttack0", "EnemyAAttack1");
    private static readonly IReadOnlyList<Rect2> _enemyBIdle
        = Globals.Sheet.GetBounds("EnemyBIdle0", "EnemyBIdle1", "EnemyBIdle2", "EnemyBIdle3", "EnemyBIdle4", "EnemyBIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyBAttack = Globals.Sheet.GetBounds("EnemyBAttack0", "EnemyBAttack1");

    private EnemyType _type;
    private Animator<AnimType> _anim;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        _type = LDtkSetting.GetEnumSetting<EnemyType>(Settings, "Type");

        if (_type == EnemyType.Random)
        {
            var types = Enum
                .GetValues<EnemyType>()
                .Where(x => x != EnemyType.Random && x != EnemyType.None);

            _type = FastRandom.Shared.Choice(types);
        }

        var idle = _type switch
        {
            EnemyType.Normal => _enemyAIdle.ToArray(),
            EnemyType.Enraged => _enemyBIdle.ToArray(),
            _ => throw new InvalidOperationException($"Unable to detect idle animation for: '{_type}'.")
        };
        var attack = _type switch
        {
            EnemyType.Normal => _enemyAAttack.ToArray(),
            EnemyType.Enraged => _enemyBAttack.ToArray(),
            _ => throw new InvalidOperationException($"Unable to detect attack animation for: '{_type}'.")
        };

        _anim = new Animator<AnimType>(Globals.Texture) { AnimFinished = (_, _) => _anim.Play(AnimType.Idle, true) }
            .Add(AnimType.Idle, idle, 8f, true)
            .Add(AnimType.Attack, attack, 8f, false)
            .Play(AnimType.Idle, false)
            ;

        base.OnEnter();
    }

    private void OnPlayerMoved(BeaconHandle handle)
    {
        if (IsMoving) return;

        var player = handle.Get<Player>(0);
        var path = App.GetPath(Location, player.Location);

        if (MapHelper.IsUnitAround(player.Location, Location, false))
        {
            BeaconManager.Instance.Publish(GameBecaons.PlayerHit);
            BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.EnemyFoodReduction);

            _anim.Play(AnimType.Attack, false);
            return;
        }

        if (path.IsEmpty())
            return;

        var dir = player.Location - Location;

        if (dir.X != 0)
            Direction = (int)dir.X;

        SetPath(path[0]);
    }

    public override void OnUpdate(FrameTime frameTime)
    {
        _anim.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    public override void OnDraw(SpriteBatcher batch, FrameTime frameTime)
    {
        var effects = Direction < 0 ? TextureEffects.Horizontal : TextureEffects.None;

        _anim.Draw(batch, Position, effects, Globals.EnemyDepth);

        base.OnDraw(batch, frameTime);
    }
}