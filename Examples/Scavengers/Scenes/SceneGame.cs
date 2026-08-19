namespace Scavengers.Scenes;

public sealed class SceneGame : Scene
{
    private readonly Dictionary<Vect2, bool> _collisions = [];
    private readonly EntityManager _manager = new();
    private SpriteBatcher _batch;
    private bool _isGameOver;
    private LDtkLevel _level;
    private AStar2D _astar;
    private Rect2 _region;
    private bool _gameOver;

    public override void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.UpdateFood, OnUpdateFood);
        BeaconManager.Instance.Subscribe(GameBecaons.GameOver, OnGameOver);

        var level = FastRandom.Shared.Choice(Globals.Levels);

        if (!Globals.Map.TryGetLevelByName(level, out _level))
            throw new InvalidOperationException($"Unable to load LDtk Level: '{level}'.");

        _region = new Rect2(Vect2.Zero, _level.GridSize);
        _batch = new SpriteBatcher();
        _astar = new AStar2D
        {
            DefaultAlgorithm = PathAlgorithm.AStar,
            DefaultDiagonalMode = DiagonalMode.Never,
            DefaultHeuristic = Heuristic.Manhattan
        };

        var paths = new List<Vect2>();
        foreach (var layer in _level.Layers)
        {
            switch (layer.Type)
            {
                case LDtkLayerType.IntGrid:
                    _collisions.EnsureCapacity(layer.Instances.Count);
                    foreach (var inst in layer.InstanceAs<LDtkIntGridInstance>())
                    {
                        _collisions[inst.Location] = inst.IsSolid;

                        if (!inst.IsSolid)
                            paths.Add(inst.Location);
                    }
                    break;
                case LDtkLayerType.Entities:
                    foreach (var inst in layer.InstanceAs<LDtkEntityInstance>())
                    {
                        if (!InstanceHelper.TryCreateInstance<Entity>(inst.Name, true, [inst], out var instance))
                            continue;

                        _manager.Add(instance);
                    }
                    break;
                case LDtkLayerType.Tiles:
                    foreach (var inst in layer.InstanceAs<LDtkTileInstance>())
                        _manager.Add(new Tile(inst.Position, inst.Source, inst.Effects));
                    break;
            }
        }

        _astar.ReserveSpace(paths.Count);
        foreach (var p in paths)
        {
            var id = MapHelper.To1D(p, (int)_level.GridSize.X);
            _astar.AddPoint(id, p);
        }

        foreach (var p in paths)
        {
            var idFrom = MapHelper.To1D(p, (int)_level.GridSize.X);

            var neighbours = new[]
            {
                p + Vect2.Up,
                p + Vect2.Right,
                p + Vect2.Down,
                p + Vect2.Left,
            };

            for (int i = neighbours.Length - 1; i >= 0; i--)
            {
                var n = neighbours[i];
                var idTo = MapHelper.To1D(n, (int)_level.GridSize.X);

                if (!paths.Contains(n)) continue;
                if (!_astar.HasPoint(idFrom)) continue;
                if (!_astar.HasPoint(idTo)) continue;
                if (_astar.ArePointsConnected(idFrom, idTo)) continue;

                _astar.ConnectPoints(idFrom, idTo);
            }
        }

        CoroutineManager.Instance.Run(Globals.FadeInMusic());

        base.OnEnter();
    }

    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.UpdateFood, OnUpdateFood);
        BeaconManager.Instance.Unsubscribe(GameBecaons.GameOver, OnGameOver);

        _manager.Clear();

        base.OnExit();
    }



    private void OnGameOver(BeaconHandle handle) => _gameOver = true;

    private void OnUpdateFood(BeaconHandle handle)
    {
        var amount = handle.Get<int>(0);

        Globals.Data.Food += amount;

        if (Globals.Data.Food <= 0)
        {
            BeaconManager.Instance.Publish(GameBecaons.LockUnits);
            BeaconManager.Instance.Publish(GameBecaons.GameOver);

            if (!_isGameOver)
            {
                CoroutineManager.Instance.Run(new DelayCall(2f, () => SceneManager.Instance.Add(new SceneGameOver())));
                _isGameOver = true;
            }
        }
    }

    public Vect2[] GetPath(Vect2 start, Vect2 end)
    {
        var idFrom = MapHelper.To1D(start, (int)_level.GridSize.X);
        var idTo = MapHelper.To1D(end, (int)_level.GridSize.X);

        if (idFrom == idTo)
            return [];

        var path = _astar.GetPath(idFrom, idTo);

        if (path.IsEmpty())
            return [];

        if (path.Count > 0)
        {
            path.Remove(path[0]);

            if (path.Count > 0)
                path.Remove(path[^1]);
        }

        return [.. path];
    }



    public override void Update(FrameTime frameTime)
    {
        if (!_gameOver && SceneManager.Instance.Scenes[^1] == this)
            Globals.Data.PlayTime += frameTime.DeltaTime;

        _manager.Update(frameTime);

        base.Update(frameTime);
    }

    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(camera: Globals.Camera);
        _manager.Draw(_batch, frameTime);
        _batch.End();

        _batch.Begin(camera: Globals.CameraUi);
        _batch.Draw(Globals.Texture, Vect2.One * 8, Globals.Sheet.GetBound("SodaUi"), Color.White, 0f);
        _batch.DrawText(Globals.Font, $"{Math.Max(Globals.Data.Food, 0)}", new Rect2(8 + 28, 8, 100, 16), Color.White, Vect2.One, 0f, TextAlignment.CenterLeft, TextWrapMode.None);
        _batch.End();

        base.Draw(frameTime);
    }


    public bool InMapRegion(Vect2 location) => _region.Contains(location);

    public bool HasCollded(Vect2 location)
    {
        if (!InMapRegion(location))
            return true;
        if (!_collisions.TryGetValue(location, out var value))
            return true;

        return value;
    }

    public void SetCollision(Vect2 location, bool isSolid)
    {
        if (!InMapRegion(location) || !_collisions.ContainsKey(location))
            return;

        var id = MapHelper.To1D(location, (int)_level.GridSize.X);

        _astar.SetPointDisabled(id, isSolid);

        _collisions[location] = isSolid;
    }
}
