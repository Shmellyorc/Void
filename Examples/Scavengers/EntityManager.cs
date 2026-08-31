// ============================================================================
//  Entity.cs - Scavengers Demo Entity System
// ============================================================================
//  This file contains the base Entity class and EntityManager for managing
//  all game entities (Player, Enemy, Food, Wall, etc.).
//
//  The demo shows how to:
//  - Create a base entity class with common behavior
//  - Manage entity lifecycle (add, remove, update, draw)
//  - Use pathfinding for movement
//  - Communicate between entities using beacons
// ============================================================================

namespace Scavengers;

/// <summary>
/// Base class for all game entities.
/// Provides common functionality: position, movement, pathfinding, lifecycle.
/// </summary>
/// <remarks>
/// Entities are managed by the EntityManager. Each entity can:
/// - Move along a path using AStar pathfinding
/// - Communicate via beacons
/// - Be destroyed and removed from the game
/// </remarks>
public class Entity
{
    // The LDtk entity instance this entity was created from (null for non-LDtk entities)
    private readonly LDtkEntityInstance _inst;

    // Queue of world positions to move to
    private readonly Queue<Vect2> _path = [];

    // ============================================================================
    // Properties
    // ============================================================================

    /// <summary>Current world position of the entity.</summary>
    public Vect2 Position { get; set; }

    /// <summary>Size of the entity in pixels.</summary>
    public Vect2 Size { get; }

    /// <summary>Grid location of the entity (converted from world position).</summary>
    public Vect2 Location => MapHelper.WorldToMap(Position, Globals.TileSize);

    /// <summary>LDtk settings for this entity (null for non-LDtk entities).</summary>
    public IReadOnlyDictionary<uint, LDtkSetting> Settings => _inst.Settings;

    /// <summary>Returns true if the entity has been destroyed.</summary>
    public bool IsDestroyed { get; private set; }

    /// <summary>Returns the current SceneManager instance.</summary>
    public SceneManager Scene => SceneManager.Instance;

    /// <summary>Gets the SceneGame instance (convenience property).</summary>
    public SceneGame App => Scene.GetScene<SceneGame>();

    /// <summary>Returns true if the entity is locked from moving.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>Reference to the entity manager that owns this entity.</summary>
    public EntityManager Manager { get; set; }

    /// <summary>True if OnEnter has been called.</summary>
    public bool Initialized { get; set; }

    /// <summary>True if the entity is currently moving along a path.</summary>
    public bool IsMoving => _path.IsNotEmpty();

    /// <summary>Direction the entity is facing (1 = right, -1 = left).</summary>
    public int Direction = 1;

    // ============================================================================
    // Constructors
    // ============================================================================

    /// <summary>Creates an entity from an LDtk entity instance.</summary>
    public Entity(LDtkEntityInstance inst)
    {
        _inst = inst;
        Position = inst.Position;
        Size = inst.Size;
    }

    /// <summary>Creates an entity at a specific position (for non-LDtk entities like tiles).</summary>
    public Entity(Vect2 position)
    {
        Position = position;
        Size = new Vect2(Globals.TileSize);
    }

    // ============================================================================
    // Lifecycle Methods
    // ============================================================================

    /// <summary>
    /// Called when the entity is added to the EntityManager.
    /// Override this for initialization logic.
    /// </summary>
    public virtual void OnEnter()
    {
        // Subscribe to the LockUnits beacon to stop movement when needed
        BeaconManager.Instance.Subscribe(GameBecaons.LockUnits, OnLocked);
    }

    /// <summary>
    /// Called when the entity is removed from the EntityManager.
    /// Override this for cleanup logic.
    /// </summary>
    public virtual void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.LockUnits, OnLocked);

        // Mark as destroyed if not already
        if (IsDestroyed)
            return;

        IsDestroyed = true;
    }

    /// <summary>
    /// Called every frame while the entity is active.
    /// Override this for custom update logic.
    /// </summary>
    public virtual void OnUpdate(FrameTime frameTime)
    {
        // If no path, do nothing
        if (_path.IsEmpty())
            return;

        // Get the next target position
        var current = _path.Peek();
        var dist = Position.DistanceSquared(current);

        // Move towards the target
        if (MathF.Floor(dist) > 0)
        {
            Position = Position.MoveTowards(current, frameTime.DeltaTime * Globals.MoveSpeed);
        }
        else
        {
            // Reached the target, move to next
            Position = _path.Dequeue();

            // If this is the player, publish movement events
            if (this is Player player)
            {
                SoundHelper.PlayRandom([Globals.FootStep1, Globals.FootStep2], Globals.SoundFxVolume);
                BeaconManager.Instance.Publish(GameBecaons.UpdateFood, Globals.PlayerMoveFoodReduction);
                BeaconManager.Instance.Publish(GameBecaons.PlayerMoved, player);
            }
        }
    }

    /// <summary>
    /// Called every frame for rendering.
    /// Override this for custom drawing logic.
    /// </summary>
    public virtual void OnDraw(SpriteBatcher batch, FrameTime frameTime) { }

    // ============================================================================
    // Public Methods
    // ============================================================================

    /// <summary>
    /// Handles the LockUnits beacon. Locks the entity from moving.
    /// </summary>
    private void OnLocked(BeaconHandle handle) => IsLocked = true;

    /// <summary>
    /// Destroys the entity, removing it from the EntityManager.
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed)
            return;

        Manager.Remove(this);
    }

    /// <summary>
    /// Sets a path for the entity to follow.
    /// Each path point is checked for collisions before being added.
    /// </summary>
    public void SetPath(params Vect2[] path)
    {
        if (path.IsEmpty())
            return;

        foreach (var p in path)
        {
            // Stop at the first collision
            if (App.HasCollded(p))
                break;

            // Convert from grid to world coordinates
            _path.Enqueue(MapHelper.MapToWorld(p, Globals.TileSize));
        }
    }
}

/// <summary>
/// Manages all entities in the game.
/// Handles adding, removing, updating, and drawing entities.
/// </summary>
public sealed class EntityManager
{
    [Flags]
    private enum DirtyState
    {
        None,
        AddOrRemove
    }

    private readonly List<Entity> _entities = [];
    private readonly List<Entity> _active = [];
    private DirtyState _state;

    /// <summary>Returns a read-only list of all entities.</summary>
    public IReadOnlyList<Entity> Scenes => _entities;

    /// <summary>
    /// Updates all active entities.
    /// </summary>
    public void Update(FrameTime frameTime)
    {
        if (_entities.Count == 0)
            return;

        // Rebuild the active list if entities were added or removed
        if (_state != DirtyState.None)
        {
            _active.EnsureCapacity(_entities.Count);
            _active.Clear();

            foreach (var entity in _entities)
            {
                if (entity == null || entity.IsDestroyed)
                    continue;

                _active.Add(entity);

                // Initialize the entity if it hasn't been yet
                if (!entity.Initialized)
                {
                    entity.OnEnter();
                    entity.Initialized = true;
                }
            }

            _state = DirtyState.None;
        }

        // Update all active entities
        foreach (var entity in _active)
            entity.OnUpdate(frameTime);
    }

    /// <summary>
    /// Draws all active entities.
    /// </summary>
    public void Draw(SpriteBatcher batch, FrameTime frameTime)
    {
        foreach (var entity in _active)
        {
            if (entity == null || entity.IsDestroyed)
                continue;

            entity.OnDraw(batch, frameTime);
        }
    }

    /// <summary>
    /// Adds one or more entities to the manager.
    /// </summary>
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

    /// <summary>
    /// Removes one or more entities from the manager.
    /// </summary>
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

    /// <summary>
    /// Removes all entities from the manager.
    /// </summary>
    public void Clear()
    {
        if (_entities.Count == 0)
            return;

        Remove([.. _entities]);
    }
}