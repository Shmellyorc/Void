namespace Scavengers;

public class Entity
{
    private readonly LDtkEntityInstance _inst;
    private readonly Queue<Vect2> _path = [];

    public Vect2 Position { get; set; }
    public Vect2 Size { get; }
    public Vect2 Location => MapHelper.WorldToMap(Position, Globals.TileSize);
    public IReadOnlyDictionary<uint, LDtkSetting> Settings => _inst.Settings;
    public bool IsDestroyed { get; private set; }
    public SceneManager Scene => SceneManager.Instance;
    public SceneGame App => Scene.GetScene<SceneGame>();
    public bool IsLocked { get; private set; }
    public EntityManager Manager { get; set; }
    public bool Initialized { get; set; }
    public bool IsMoving => _path.IsNotEmpty();
    public int Direction = 1;


    public Entity(LDtkEntityInstance inst)
    {
        _inst = inst;
        Position = inst.Position;
        Size = inst.Size;
    }

    public Entity(Vect2 position)
    {
        Position = position;
        Size = new Vect2(Globals.TileSize);
    }

    public virtual void OnUpdate(FrameTime frameTime)
    {
        if (_path.IsEmpty())
            return;

        var current = _path.Peek();
        var dist = Position.DistanceSquared(current);

        if (MathF.Floor(dist) > 0)
            Position = Position.MoveTowards(current, frameTime.DeltaTime * Globals.MoveSpeed);
        else
        {
            Position = _path.Dequeue();

            if (this is Player player)
            {
                SoundHelper.PlayRandom([Globals.FootStep1, Globals.FootStep2], Globals.SoundFxVolume);
                BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.PlayerMoveFoodReduction);
                BeaconManager.Instance.Publish(GameBecaons.PlayerMoved, player);

            }
        }
    }

    public virtual void OnDraw(SpriteBatcher batch, FrameTime frameTime) { }

    public virtual void OnEnter()
    {
        BeaconManager.Instance.Subscribe(GameBecaons.LockUnits, (h) => IsLocked = true);
    }

    public virtual void OnExit()
    {
        if (IsDestroyed)
            return;

        BeaconManager.Instance.Unsubscribe(GameBecaons.LockUnits, (h) => IsLocked = true);

        IsDestroyed = true;
    }

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        Manager.Remove(this);
    }

    public void SetPath(params Vect2[] path)
    {
        if (path.IsEmpty())
            return;

        foreach (var p in path)
        {
            if (App.HasCollded(p))
                break;

            _path.Enqueue(MapHelper.MapToWorld(p, Globals.TileSize));
        }
    }

}

public sealed class EntityManager
{
    [Flags] private enum DirtyState { None, AddOrRemove }

    private readonly List<Entity> _entities = [], _active = [];
    private DirtyState _state;

    public IReadOnlyList<Entity> Scenes => _entities;


    public void Update(FrameTime frameTime)
    {
        if (_entities.Count == 0)
            return;

        if (_state != DirtyState.None)
        {
            _active.EnsureCapacity(_entities.Count);
            _active.Clear();

            foreach (var entity in _entities)
            {
                if (entity == null || entity.IsDestroyed)
                    continue;

                _active.Add(entity);

                if (!entity.Initialized)
                {
                    entity.OnEnter();
                    entity.Initialized = true;
                }
            }

            _state = DirtyState.None;
        }

        foreach (var entity in _active)
            entity.OnUpdate(frameTime);
    }

    public void Draw(SpriteBatcher batch, FrameTime frameTime)
    {
        foreach (var scene in _active)
        {
            if (scene == null || scene.IsDestroyed)
                continue;

            scene.OnDraw(batch, frameTime);
        }
    }

    public void Add(params Entity[] entities)
    {
        if (entities.IsEmpty())
            return;

        var anyAdded = false;
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];

            if (entity == null || entity.IsDestroyed)
                continue;

            _entities.Add(entity);

            entity.Manager = this;

            anyAdded = true;
        }

        if (anyAdded)
            _state |= DirtyState.AddOrRemove;
    }

    public void Remove(params Entity[] entities)
    {
        if (entities.IsEmpty())
            return;

        var anyRemoved = false;
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];

            if (entity == null || entity.IsDestroyed)
                continue;
            if (!_entities.Remove(entity))
                continue;

            entity.OnExit();
            anyRemoved = true;
        }

        if (anyRemoved)
            _state |= DirtyState.AddOrRemove;
    }


    public void Clear()
    {
        if (_entities.Count == 0)
            return;

        Remove([.. _entities]);
    }
}
