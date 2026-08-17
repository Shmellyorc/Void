using Void.Engine.FSM;

namespace Scavengers.Entities;

public sealed class Enemy(LDtkEntityInstance inst) : Entity(inst)
{
    private enum EnemyType { Normal, Enraged, Random }
    private enum AnimType { Idle, Attack }

    private static readonly IReadOnlyList<Rect2> _enemyAIdle
        = Globals.Sheet.GetBounds("EnemyAIdle0", "EnemyAIdle1", "EnemyAIdle2", "EnemyAIdle3", "EnemyAIdle4", "EnemyAIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyAAttack = Globals.Sheet.GetBounds("EnemyAAttack0", "EnemyAAttack1");
    private static readonly IReadOnlyList<Rect2> _enemyBIdle
        = Globals.Sheet.GetBounds("EnemyBIdle0", "EnemyBIdle1", "EnemyBIdle2", "EnemyBIdle3", "EnemyBIdle4", "EnemyBIdle5");
    private static readonly IReadOnlyList<Rect2> _enemyBAttack = Globals.Sheet.GetBounds("EnemyBAttack0", "EnemyBAttack1");

    private EnemyType _type;
    private Animator _anim;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);
        BeaconManager.Instance.Subscribe(GameBecaons.EnemyMoved, OnEnemyMoved);

        _type = LDtkSetting.GetEnumSetting<EnemyType>(Settings, "Type");

        if (_type == EnemyType.Random)
        {
            var types = Enum
                .GetValues<EnemyType>()
                .Where(x => x != EnemyType.Random);

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

        _anim = new Animator(Globals.Texture) { AnimFinished = (anim) => _anim.Play(AnimType.Idle, true) }
            .Add(AnimType.Idle, idle, 8f, true)
            .Add(AnimType.Attack, attack, 8f, false)
            .Play(AnimType.Idle, false)
            ;

        base.OnEnter();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.PlayerMoved, OnPlayerMoved);
        BeaconManager.Instance.Unsubscribe(GameBecaons.EnemyMoved, OnEnemyMoved);

        base.OnExit();
    }

    private void OnEnemyMoved(BeaconHandle handle)
    {
        var enemy = handle.Get<Enemy>(0);

        if (enemy != this) return;
        if (!MapHelper.IsUnitAround(_player.Location, Location, false)) return;

        _anim.Play(AnimType.Attack, false);
    }

    private Player _player;

    private void OnPlayerMoved(BeaconHandle handle)
    {
        if (IsMoving) return;

        _player = handle.Get<Player>(0);
        var path = App.GetPath(Location, _player.Location);

        if (path.IsEmpty())
            return;

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

        _anim.Draw(batch, Position, effects, Globals.DefaultDepth);

        base.OnDraw(batch, frameTime);
    }
}