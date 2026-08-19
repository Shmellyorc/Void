namespace Scavengers.Entities;

public sealed class Food(LDtkEntityInstance inst) : Entity(inst)
{
    private enum FoodType { Soda, Fruit, Random }

    private FoodType _type;
    private Animator<FoodType> _anim;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.PlayerMoved, OnPlayerMoved);

        _type = LDtkSetting.GetEnumSetting<FoodType>(Settings, "Type");

        if (_type == FoodType.Random)
        {
            var types = Enum
                .GetValues<FoodType>()
                .Where(x => x != FoodType.Random);
            _type = FastRandom.Shared.Choice(types);
        }

        _anim = new Animator<FoodType>(Globals.Texture)
            .Add(FoodType.Fruit, [Globals.Sheet.GetBound("Fruit")], 8f, false)
            .Add(FoodType.Soda, [Globals.Sheet.GetBound("Soda")], 8f, false)
            .Play(_type, false)
            ;

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

        if (Location != player.Location)
            return;

        var looted = _type switch
        {
            FoodType.Soda => Globals.SodaPoints,
            FoodType.Fruit => Globals.FruitPoints,
            _ => throw new InvalidOperationException($"Unable to detect fruit type of: '{_type}'.")
        };

        Sound instance = _type switch
        {
            FoodType.Soda => FastRandom.Shared.Choice([Globals.Soda1, Globals.Soda2]),
            FoodType.Fruit => FastRandom.Shared.Choice([Globals.Fruit1, Globals.Fruit2]),
            _ => throw new InvalidOperationException($"Unable to detect fruit type of: '{_type}'.")
        };

        instance.PlayAndForget(Globals.SoundFxVolume);

        Globals.Data.Food += looted;
        Globals.Data.Looted += looted;

        Destroy();
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
