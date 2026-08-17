namespace MiniPac;

public enum GameInputs
{
    MoveUp, MoveDown, MoveLeft, MoveRight
}

public sealed class MiniPackGame(GameSettings settings) : Game(settings)
{
    private PrimitiveBatcher _batch;
    private readonly List<Vect2> _collisions = [], _food = [];
    private readonly List<Entity> _entities = [], _removeEntity = [];
    private Rect2 _region;

    private readonly string _map =
        // X = Wall, C = PacMan, F = Food, . = Empty
        "XXXXXXXXX" +
        "X.......X" +
        "X.X.F.X.X" +
        "X.X.F.X.X" +
        "X.......X" +
        "X.X...X.X" +
        "X.XXXXX.X" +
        "X.......X" +
        "X.X.C.X.X" +
        "XXXXXXXXX";

    protected override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBeacons.Wall, OnWall);
        BeaconManager.Instance.Subscribe(GameBeacons.Food, OnFood);

        InputAction.AddAction(GameInputs.MoveUp)
            .AddKey(KeyboardKey.W).AddKey(KeyboardKey.Up)
            .AddGamepad(Void.Engine.Inputs.Gamepads.GamepadButton.DPadUp);
        InputAction.AddAction(GameInputs.MoveDown)
            .AddKey(KeyboardKey.S).AddKey(KeyboardKey.Down)
            .AddGamepad(Void.Engine.Inputs.Gamepads.GamepadButton.DPadDown);
        InputAction.AddAction(GameInputs.MoveLeft)
            .AddKey(KeyboardKey.A).AddKey(KeyboardKey.Left)
            .AddGamepad(Void.Engine.Inputs.Gamepads.GamepadButton.DPadLeft);
        InputAction.AddAction(GameInputs.MoveRight)
            .AddKey(KeyboardKey.D).AddKey(KeyboardKey.Right)
            .AddGamepad(Void.Engine.Inputs.Gamepads.GamepadButton.DPadRight);

        _batch = new PrimitiveBatcher();
        _region = new Rect2(Vect2.Zero, Vect2.One * Globals.MapSoze);

        Globals.Camera = new Camera();

        _entities.EnsureCapacity(_map.Length);

        for (int i = 0; i < _map.Length; i++)
        {
            var loc = MapHelper.To2D(i, Globals.MapSoze);
            var pos = MapHelper.MapToWorld(loc, (int)Globals.TileSize);

            Entity item = _map[i].ToString().ToUpper() switch
            {
                "X" => new Wall(pos),
                "C" => new PacMan(pos),
                "F" => new Food(pos),
                _ => null,
            };

            if (item == null)
                continue;

            _entities.Add(item);
        }

        base.OnEnter();
    }

    private void OnWall(BeaconHandle handle)
        => _collisions.Add(handle.Get<Vect2>(0));
    private void OnFood(BeaconHandle handle)
       => _food.Add(handle.Get<Vect2>(0));

    protected override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBeacons.Wall, OnWall);
        BeaconManager.Instance.Unsubscribe(GameBeacons.Food, OnFood);

        base.OnExit();
    }

    private bool _gameFinihed;

    protected override void OnUpdate(FrameTime frameTime)
    {
        if (_food.Count == 0 && !_gameFinihed)
        {
            System.Console.WriteLine("Game Finished!");

            _gameFinihed = true;
        }

        foreach (var entity in _entities)
        {
            if (entity.IsExiting)
            {
                if (entity is Food food)
                    _food.Remove(food.Location);

                _removeEntity.Add(entity);
                continue;
            }

            entity.Update(frameTime);
        }

        foreach (var entity in _removeEntity)
            _entities.Remove(entity);
        _removeEntity.Clear();

        base.OnUpdate(frameTime);
    }

    protected override void OnDraw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, camera: Globals.Camera);

        foreach (var entity in _entities)
            entity.Draw(_batch);

        _batch.End();

        base.OnDraw(frameTime);
    }

    public bool InRegion(Vect2 location) => _region.Contains(location);

    public bool HasCollded(Vect2 location)
    {
        if (!InRegion(location))
            return true;

        return _collisions.Contains(location);
    }
}
