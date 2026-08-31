// ============================================================================
//  SceneGame.cs - Scavengers Demo Main Gameplay Scene
// ============================================================================
//  This is the core gameplay scene. It loads the LDtk level, sets up the
//  AStar pathfinding graph, manages all game entities, and handles the
//  game loop.
//
//  The demo shows how to:
//  - Load and parse LDtk levels
//  - Build a pathfinding graph from level data
//  - Manage entities with the EntityManager
//  - Handle game state (food, game over, transitions)
//  - Animate UI elements with tweens
// ============================================================================

namespace Scavengers.Scenes;

/// <summary>
/// Main gameplay scene. Loads a level, manages entities, and handles game logic.
/// </summary>
/// <remarks>
/// Key systems demonstrated:
/// - LDtk level loading (IntGrid, Entities, Tiles layers)
/// - AStar2D pathfinding with graph construction from walkable tiles
/// - Entity management with EntityManager
/// - Food system with animations
/// - Game over detection
/// </remarks>
public sealed class SceneGame : Scene
{
    // Collision map for the current level
    private readonly Dictionary<Vect2, bool> _collisions = [];

    // All game entities (player, enemies, food, walls, etc.)
    private readonly EntityManager _manager = new();

    private SpriteBatcher _batch;
    private bool _isGameOver;
    private LDtkLevel _level;
    private AStar2D _astar;
    private Rect2 _region;
    private bool _gameOver;

    // ============================================================================
    // Lifecycle
    // ============================================================================

    /// <summary>
    /// Called when the scene is added.
    /// Loads the level, builds the pathfinding graph, and creates entities.
    /// </summary>
    public override void OnEnter()
    {
        // Subscribe to game events
        BeaconManager.Instance.Subscribe(GameBecaons.UpdateFood, OnUpdateFood);
        BeaconManager.Instance.Subscribe(GameBecaons.GameOver, OnGameOver);

        // ========================================================================
        // Load the LDtk level
        // ========================================================================
        // Randomly select a level from the available levels
        var levelName = FastRandom.Shared.Choice(Globals.Levels);

        if (!Globals.Map.TryGetLevelByName(levelName, out _level))
            throw new InvalidOperationException($"Unable to load LDtk Level: '{levelName}'.");

        _region = new Rect2(Vect2.Zero, _level.GridSize);
        _batch = new SpriteBatcher();

        // ========================================================================
        // Setup AStar2D pathfinding
        // ========================================================================
        // Configure AStar for grid-based movement (4-directional, Manhattan heuristic)
        _astar = new AStar2D
        {
            DefaultAlgorithm = PathAlgorithm.AStar,
            DefaultDiagonalMode = DiagonalMode.Never,        // No diagonal movement
            DefaultHeuristic = Heuristic.Manhattan           // 4-directional distance
        };

        // ========================================================================
        // Parse LDtk layers
        // ========================================================================
        var paths = new List<Vect2>();

        foreach (var layer in _level.Layers)
        {
            switch (layer.Type)
            {
                case LDtkLayerType.IntGrid:
                    // IntGrid layers define walkable vs solid tiles
                    _collisions.EnsureCapacity(layer.Instances.Count);
                    foreach (var inst in layer.InstanceAs<LDtkIntGridInstance>())
                    {
                        _collisions[inst.Location] = inst.IsSolid;

                        // Collect walkable tiles for pathfinding
                        if (!inst.IsSolid)
                            paths.Add(inst.Location);
                    }
                    break;

                case LDtkLayerType.Entities:
                    // Entity layers contain game objects (player, enemies, food, walls, signs)
                    foreach (var inst in layer.InstanceAs<LDtkEntityInstance>())
                    {
                        // Use InstanceHelper to create the correct entity type based on the LDtk entity name
                        if (!InstanceHelper.TryCreateInstance<Entity>(inst.Name, true, [inst], out var instance))
                            continue;

                        _manager.Add(instance);
                    }
                    break;

                case LDtkLayerType.Tiles:
                    // Tile layers are static background tiles
                    foreach (var inst in layer.InstanceAs<LDtkTileInstance>())
                        _manager.Add(new Tile(inst.Position, inst.Source, inst.Effects));
                    break;
            }
        }

        // ========================================================================
        // Build the AStar graph
        // ========================================================================
        // Reserve space for all walkable tiles
        _astar.ReserveSpace(paths.Count);

        // Add all walkable tiles as nodes in the graph
        foreach (var p in paths)
        {
            var id = MapHelper.To1D(p, (int)_level.GridSize.X);
            _astar.AddPoint(id, p);
        }

        // Connect adjacent walkable tiles
        foreach (var p in paths)
        {
            var idFrom = MapHelper.To1D(p, (int)_level.GridSize.X);

            // Check the 4 cardinal directions
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

                // Only connect if the neighbor is walkable and not already connected
                if (!paths.Contains(n)) continue;
                if (!_astar.HasPoint(idFrom)) continue;
                if (!_astar.HasPoint(idTo)) continue;
                if (_astar.ArePointsConnected(idFrom, idTo)) continue;

                _astar.ConnectPoints(idFrom, idTo);
            }
        }

        // Start the background music with a fade in
        CoroutineManager.Instance.Run(Globals.FadeInMusic());

        base.OnEnter();
    }

    /// <summary>
    /// Called when the scene is removed.
    /// Cleans up resources.
    /// </summary>
    public override void OnExit()
    {
        BeaconManager.Instance.Unsubscribe(GameBecaons.UpdateFood, OnUpdateFood);
        BeaconManager.Instance.Unsubscribe(GameBecaons.GameOver, OnGameOver);

        _manager.Clear();

        base.OnExit();
    }

    // ============================================================================
    // Game Event Handlers
    // ============================================================================

    private void OnGameOver(BeaconHandle handle) => _gameOver = true;

    /// <summary>
    /// Handles food updates from various sources (player movement, attacks, collecting food, enemy hits).
    /// </summary>
    private void OnUpdateFood(BeaconHandle handle)
    {
        var amount = handle.Get<int>(0);

        // Positive amounts = collecting food
        if (amount > 0)
            Globals.Data.Looted += amount;

        // Negative amounts = losing food
        if (amount < 0)
        {
            Globals.Data.Food += amount;

            // Check for game over
            if (Globals.Data.Food <= 0)
            {
                // Lock all units so the game state is frozen
                BeaconManager.Instance.Publish(GameBecaons.LockUnits);
                BeaconManager.Instance.Publish(GameBecaons.GameOver);

                if (!_isGameOver)
                {
                    // Show the game over screen after a 2 second delay
                    CoroutineManager.Instance.Run(new DelayCall(2f, () => SceneManager.Instance.Add(new SceneGameOver())));
                    _isGameOver = true;
                }
            }
        }
        else
        {
            // Animate positive food changes (smoothly increasing the counter)
            CoroutineManager.Instance.Run(AnimateFood(amount));
        }
    }

    /// <summary>
    /// Animates the food counter when food is collected.
    /// Uses a tween to smoothly transition the value.
    /// </summary>
    private IEnumerator AnimateFood(float amount)
    {
        if (_isGameOver)
            yield break;

        var start = Globals.Data.Food;
        var end = Globals.Data.Food + amount;

        // Tween the food value over 0.1 seconds with cubic easing
        yield return new Tween<float>(start, end, 0.1f, EaseType.CubicOut, MathHelper.Lerp,
            v => Globals.Data.Food = (int)v);
    }

    // ============================================================================
    // Public Methods
    // ============================================================================

    /// <summary>
    /// Calculates a path between two grid positions using AStar.
    /// The start and end positions are excluded from the returned path.
    /// </summary>
    public Vect2[] GetPath(Vect2 start, Vect2 end)
    {
        var idFrom = MapHelper.To1D(start, (int)_level.GridSize.X);
        var idTo = MapHelper.To1D(end, (int)_level.GridSize.X);

        // Same tile, no path needed
        if (idFrom == idTo)
            return [];

        var path = _astar.GetPath(idFrom, idTo);

        if (path.IsEmpty())
            return [];

        // Remove the start and end positions from the path
        // The entity will move to the next tile in the path
        if (path.Count > 0)
        {
            path.Remove(path[0]);

            if (path.Count > 0)
                path.Remove(path[^1]);
        }

        return [.. path];
    }

    // ============================================================================
    // Game Loop
    // ============================================================================

    /// <summary>
    /// Called every frame. Updates the game state.
    /// </summary>
    public override void Update(FrameTime frameTime)
    {
        // Track play time only if the game is active and this is the top scene
        if (!_gameOver && SceneManager.Instance.Scenes[^1] == this)
            Globals.Data.PlayTime += frameTime.DeltaTime;

        // Update all entities
        _manager.Update(frameTime);

        base.Update(frameTime);
    }

    /// <summary>
    /// Called every frame. Renders the game.
    /// </summary>
    public override void Draw(FrameTime frameTime)
    {
        // ========================================================================
        // Draw the game world
        // ========================================================================
        _batch.Begin(camera: Globals.Camera);
        _manager.Draw(_batch, frameTime);
        _batch.End();

        // ========================================================================
        // Draw the UI overlay
        // ========================================================================
        _batch.Begin(camera: Globals.CameraUi);

        // Draw the food icon (soda can)
        _batch.Draw(Globals.Texture, Vect2.One * 8, Globals.Sheet.GetBound("SodaUi"), Color.White, 0f);

        // Draw the food counter
        _batch.DrawText(
            Globals.Font,
            $"{Math.Max(Globals.Data.Food, 0)}",
            new Rect2(8 + 28, 8, 100, 16),
            Color.White,
            Vect2.One,
            0f,
            TextAlignment.CenterLeft,
            TextWrapMode.None
        );

        _batch.End();

        base.Draw(frameTime);
    }

    // ============================================================================
    // Collision Methods
    // ============================================================================

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

        // Update the pathfinding graph
        _astar.SetPointDisabled(id, isSolid);

        // Update the collision map
        _collisions[location] = isSolid;
    }
}