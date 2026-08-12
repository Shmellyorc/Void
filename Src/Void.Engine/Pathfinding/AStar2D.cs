namespace Void.Engine.Pathfinding;

/// <summary>
/// Defines how diagonal movement is handled during pathfinding.
/// </summary>
public enum DiagonalMode
{
    /// <summary>
    /// The default value, uses diagonals freely.
    /// </summary>
    Always,

    /// <summary>
    /// All movement is orthogonal (no diagonals).
    /// </summary>
    Never,

    /// <summary>
    /// Allows diagonals, but prevents the path going "between" diagonally placed obstacles.
    /// </summary>
    AtLeastOneWalkable,

    /// <summary>
    /// Allows diagonals only in "open" areas, not near obstacles.
    /// </summary>
    OnlyIfNoObstacles
}

/// <summary>
/// Defines the heuristic function used to estimate cost between two points.
/// </summary>
public enum Heuristic
{
    /// <summary>
    /// No heuristic. Effectively turns A* into Dijkstra's algorithm.
    /// </summary>
    None,

    /// <summary>
    /// Manhattan distance: |dx| + |dy|
    /// Best for 4-directional movement (no diagonals).
    /// </summary>
    Manhattan,

    /// <summary>
    /// Euclidean distance: sqrt(dx² + dy²)
    /// Best for 8-directional movement with arbitrary angles.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Octile distance: max(|dx|, |dy|) + (sqrt(2) - 1) * min(|dx|, |dy|)
    /// Best for 8-directional movement where diagonals cost sqrt(2).
    /// </summary>
    Octile,

    /// <summary>
    /// Chebyshev distance: max(|dx|, |dy|)
    /// Best for 8-directional movement where diagonals cost the same as orthogonal.
    /// </summary>
    Chebyshev
}

/// <summary>
/// Defines the algorithm used for pathfinding.
/// </summary>
public enum PathAlgorithm
{
    /// <summary>
    /// A* algorithm. Uses heuristic to guide search. Fast for single path queries.
    /// Best balance of speed and optimality.
    /// </summary>
    AStar,

    /// <summary>
    /// Dijkstra's algorithm. No heuristic. Finds shortest path to all nodes from start.
    /// Slower for single path, but required for flow fields and when all costs matter equally.
    /// </summary>
    Dijkstra,

    /// <summary>
    /// Breadth-first search. Ignores weights entirely, treats all edges as equal cost.
    /// Fastest for unweighted graphs. Not optimal for weighted graphs.
    /// </summary>
    BFS,

    /// <summary>
    /// Greedy Best-First Search. Uses only heuristic, ignores actual path cost.
    /// Very fast but may not find the optimal path.
    /// </summary>
    GreedyBestFirst
}

/// <summary>
/// An implementation of A* for finding the shortest path between two vertices on a connected graph in 2D space.
/// Adapted from Godot's AStar2D with additional algorithm support and flow field computation.
/// </summary>
public sealed class AStar2D : IDisposable
{
    private const float Sqrt2 = 1.4142135623730951f;

    // Point storage
    private Vect2[] _positions;
    private float[] _weightScales;
    private bool[] _disabled;
    private bool[] _hasPoint;
    private Dictionary<int, float>[] _connections;
    private List<int>[] _neighbors;
    private int _pointCount;
    private int _capacity;
    private int _nextAvailableId;

    // Pathfinding arrays (pre-allocated)
    private float[] _gScore;
    private float[] _fScore;
    private int[] _cameFrom;
    private bool[] _visited;
    private int[] _openSetItems;
    private float[] _openSetPriorities;
    private int[] _openSetPositions;
    private int _openSetCount;
    private int[] _queue;
    private int _queueHead;
    private int _queueTail;

    // Default settings
    public DiagonalMode DefaultDiagonalMode { get; set; } = DiagonalMode.Always;
    public Heuristic DefaultHeuristic { get; set; } = Heuristic.Octile;
    public PathAlgorithm DefaultAlgorithm { get; set; } = PathAlgorithm.AStar;

    /// <summary>
    /// If true, enables filtering of neighbors via <see cref="FilterNeighborOverride"/>.
    /// </summary>
    public bool NeighborFilterEnabled { get; set; }

    /// <summary>
    /// Called when computing the cost between two connected points.
    /// </summary>
    public Func<int, int, float> ComputeCostOverride { get; set; }

    /// <summary>
    /// Called when estimating the cost between a point and the path's ending point.
    /// </summary>
    public Func<int, int, float> EstimateCostOverride { get; set; }

    /// <summary>
    /// Called when a neighbor enters processing if <see cref="NeighborFilterEnabled"/> is true.
    /// Return true to skip the neighbor.
    /// </summary>
    public Func<int, int, bool> FilterNeighborOverride { get; set; }

    /// <summary>
    /// Gets the current number of points in the graph.
    /// </summary>
    public int PointCount => _pointCount;

    /// <summary>
    /// Gets the current capacity of the backing arrays.
    /// </summary>
    public int PointCapacity => _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="AStar2D"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The number of points to pre-allocate for.</param>
    public AStar2D(int initialCapacity = 1024)
    {
        ReserveSpace(initialCapacity);
    }

    /// <summary>
    /// Reserves space internally for the specified number of points.
    /// Call this before adding many points to avoid reallocations.
    /// </summary>
    /// <param name="numNodes">The number of points to reserve space for.</param>
    public void ReserveSpace(int numNodes)
    {
        if (numNodes <= _capacity)
            return;

        int newCapacity = Math.Max(numNodes, _capacity * 2);
        int oldCapacity = _capacity;

        Array.Resize(ref _positions, newCapacity);
        Array.Resize(ref _weightScales, newCapacity);
        Array.Resize(ref _disabled, newCapacity);
        Array.Resize(ref _hasPoint, newCapacity);
        Array.Resize(ref _connections, newCapacity);
        Array.Resize(ref _neighbors, newCapacity);

        Array.Resize(ref _gScore, newCapacity);
        Array.Resize(ref _fScore, newCapacity);
        Array.Resize(ref _cameFrom, newCapacity);
        Array.Resize(ref _visited, newCapacity);
        Array.Resize(ref _openSetItems, newCapacity);
        Array.Resize(ref _openSetPriorities, newCapacity);
        Array.Resize(ref _openSetPositions, newCapacity);
        Array.Resize(ref _queue, newCapacity);

        // Initialize open set positions to -1 (not in heap)
        for (int i = oldCapacity; i < newCapacity; i++)
            _openSetPositions[i] = -1;

        _capacity = newCapacity;
    }

    /// <summary>
    /// Adds a new point at the given position with the given identifier.
    /// </summary>
    /// <param name="id">The point's ID. Must be 0 or larger.</param>
    /// <param name="position">The point's position in 2D space.</param>
    /// <param name="weightScale">The point's weight scale. Lower is preferred. Must be 0 or greater.</param>
    public void AddPoint(int id, Vect2 position, float weightScale = 1.0f)
    {
        if (id < 0)
            throw new ArgumentOutOfRangeException(nameof(id), "ID must be 0 or larger.");
        if (weightScale < 0f)
            throw new ArgumentOutOfRangeException(nameof(weightScale), "Weight scale must be 0 or greater.");

        if (id >= _capacity)
            ReserveSpace(id + 1);

        if (!_hasPoint[id])
        {
            _hasPoint[id] = true;
            _pointCount++;
            _nextAvailableId = Math.Max(_nextAvailableId, id + 1);
        }

        _positions[id] = position;
        _weightScales[id] = weightScale;
        _disabled[id] = false;
        _connections[id] ??= new Dictionary<int, float>();
        _neighbors[id] ??= new List<int>();
    }

    /// <summary>
    /// Removes the point associated with the given ID.
    /// </summary>
    /// <param name="id">The ID of the point to remove.</param>
    public void RemovePoint(int id)
    {
        if (id < 0 || id >= _capacity || !_hasPoint[id])
            return;

        // Remove connections from neighbors
        if (_neighbors[id] != null)
        {
            foreach (var neighborId in _neighbors[id])
            {
                _connections[neighborId]?.Remove(id);
                _neighbors[neighborId]?.Remove(id);
            }
        }

        _hasPoint[id] = false;
        _connections[id]?.Clear();
        _neighbors[id]?.Clear();
        _pointCount--;
    }

    /// <summary>
    /// Clears all points and connections.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _capacity; i++)
        {
            _hasPoint[i] = false;
            _connections[i]?.Clear();
            _neighbors[i]?.Clear();
        }

        _pointCount = 0;
        _nextAvailableId = 0;
    }

    /// <summary>
    /// Returns whether a point with the given ID exists.
    /// </summary>
    public bool HasPoint(int id) =>
        id >= 0 && id < _capacity && _hasPoint[id];

    /// <summary>
    /// Returns the next available point ID with no point associated to it.
    /// </summary>
    public int GetAvailablePointId()
    {
        while (_nextAvailableId < _capacity && _hasPoint[_nextAvailableId])
            _nextAvailableId++;
        return _nextAvailableId;
    }

    /// <summary>
    /// Creates a connection between the given points.
    /// </summary>
    /// <param name="id">The first point ID.</param>
    /// <param name="toId">The second point ID.</param>
    /// <param name="bidirectional">If true, creates a two-way connection.</param>
    public void ConnectPoints(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            throw new ArgumentException("Both points must exist.");

        AddConnection(id, toId);
        if (bidirectional)
            AddConnection(toId, id);
    }

    /// <summary>
    /// Removes a connection between the given points.
    /// </summary>
    public void DisconnectPoints(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            return;

        RemoveConnection(id, toId);
        if (bidirectional)
            RemoveConnection(toId, id);
    }

    /// <summary>
    /// Returns whether there is a connection between the given points.
    /// </summary>
    public bool ArePointsConnected(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            return false;

        bool forward = _connections[id]?.ContainsKey(toId) ?? false;
        bool reverse = _connections[toId]?.ContainsKey(id) ?? false;

        if (bidirectional)
            return forward && reverse;
        return forward;
    }

    /// <summary>
    /// Returns the IDs of all points that form a connection with the given point.
    /// </summary>
    public List<int> GetPointConnections(int id)
    {
        if (!HasPoint(id) || _neighbors[id] == null)
            return [];
        return new List<int>(_neighbors[id]);
    }

    /// <summary>
    /// Returns an array of all point IDs.
    /// </summary>
    public List<int> GetPointIds()
    {
        var result = new List<int>(_pointCount);
        for (int i = 0; i < _capacity; i++)
        {
            if (_hasPoint[i])
                result.Add(i);
        }
        return result;
    }

    /// <summary>
    /// Sets the position for the point with the given ID.
    /// </summary>
    public void SetPointPosition(int id, Vect2 position)
    {
        if (!HasPoint(id))
            return;
        _positions[id] = position;
    }

    /// <summary>
    /// Gets the position of the point with the given ID.
    /// </summary>
    public Vect2 GetPointPosition(int id) =>
        HasPoint(id) ? _positions[id] : Vect2.Zero;

    /// <summary>
    /// Sets the weight scale for the point with the given ID.
    /// </summary>
    public void SetPointWeightScale(int id, float weightScale)
    {
        if (weightScale < 0f)
            throw new ArgumentOutOfRangeException(nameof(weightScale), "Weight scale must be 0 or greater.");
        if (!HasPoint(id))
            return;
        _weightScales[id] = weightScale;
    }

    /// <summary>
    /// Gets the weight scale of the point with the given ID.
    /// </summary>
    public float GetPointWeightScale(int id) =>
        HasPoint(id) ? _weightScales[id] : 0f;

    /// <summary>
    /// Disables or enables the specified point for pathfinding.
    /// </summary>
    public void SetPointDisabled(int id, bool disabled = true)
    {
        if (!HasPoint(id))
            return;
        _disabled[id] = disabled;
    }

    /// <summary>
    /// Returns whether a point is disabled for pathfinding.
    /// </summary>
    public bool IsPointDisabled(int id) =>
        HasPoint(id) && _disabled[id];

    /// <summary>
    /// Returns the ID of the closest point to the given position.
    /// </summary>
    public int GetClosestPoint(Vect2 position, bool includeDisabled = false)
    {
        int closestId = -1;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _capacity; i++)
        {
            if (!_hasPoint[i])
                continue;
            if (!includeDisabled && _disabled[i])
                continue;

            float dist = Vect2.DistanceSquared(_positions[i], position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestId = i;
            }
        }

        return closestId;
    }

    /// <summary>
    /// Returns the closest position to the given position that resides inside a segment between two connected points.
    /// </summary>
    public Vect2 GetClosestPositionInSegment(Vect2 position)
    {
        Vect2 closestPoint = position;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _capacity; i++)
        {
            if (!_hasPoint[i] || _neighbors[i] == null)
                continue;

            foreach (var neighborId in _neighbors[i])
            {
                if (neighborId <= i)
                    continue;

                Vect2 a = _positions[i];
                Vect2 b = _positions[neighborId];
                Vect2 point = ClosestPointOnSegment(position, a, b);
                float dist = Vect2.DistanceSquared(position, point);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = point;
                }
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// Returns an array with the IDs of the points that form the path between the given points.
    /// </summary>
    public List<int> GetIdPath(int fromId, int toId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
    {
        if (!HasPoint(fromId) || !HasPoint(toId))
            return [];

        if (fromId == toId)
        {
            if (_disabled[fromId])
                return [];
            return [fromId];
        }

        if (_disabled[fromId])
            return [];

        var path = FindPath(fromId, toId, allowPartialPath, diagonalMode, heuristic, algorithm);
        return path ?? [];
    }

    /// <summary>
    /// Returns an array with the positions of the points that form the path between the given points.
    /// </summary>
    public List<Vect2> GetPointPath(int fromId, int toId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
    {
        var idPath = GetIdPath(fromId, toId, allowPartialPath, diagonalMode, heuristic, algorithm);
        var result = new List<Vect2>(idPath.Count);

        foreach (var id in idPath)
            result.Add(_positions[id]);

        return result;
    }

    /// <summary>
    /// Convenience method that returns positions between two points.
    /// Empty list if start == end or no path exists.
    /// </summary>
    public List<Vect2> GetPath(int startId, int endId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
    {
        if (startId == endId)
            return [];

        if (!HasPoint(startId) || !HasPoint(endId))
            return [];

        if (_disabled[startId])
            return [];

        return GetPointPath(startId, endId, allowPartialPath, diagonalMode, heuristic, algorithm) ?? [];
    }

    /// <summary>
    /// Computes a flow field from all reachable points toward the target.
    /// Uses Dijkstra's algorithm from the target outward.
    /// </summary>
    /// <param name="targetId">The ID of the target point.</param>
    /// <returns>A <see cref="FlowField"/> containing direction information for all reachable points.</returns>
    public FlowField ComputeFlowField(int targetId)
    {
        if (!HasPoint(targetId))
            return new FlowField(new Dictionary<int, int>(), new Dictionary<int, Vect2>());

        var nextNode = new Dictionary<int, int>(_pointCount);
        var direction = new Dictionary<int, Vect2>(_pointCount);

        // Run Dijkstra from target (reverse)
        Array.Clear(_visited, 0, _capacity);
        Array.Fill(_gScore, float.MaxValue, 0, _capacity);
        ClearOpenSet();

        _gScore[targetId] = 0f;
        PushOpenSet(targetId, 0f);

        while (_openSetCount > 0)
        {
            int current = PopOpenSet();

            if (_visited[current])
                continue;

            _visited[current] = true;

            if (_neighbors[current] != null)
            {
                foreach (var neighborId in _neighbors[current])
                {
                    if (_disabled[neighborId])
                        continue;

                    float newCost = _gScore[current] + ComputeCost(current, neighborId);
                    if (newCost < _gScore[neighborId])
                    {
                        _gScore[neighborId] = newCost;
                        _cameFrom[neighborId] = current;
                        PushOpenSet(neighborId, newCost);
                    }
                }
            }
        }

        // Build flow field from cameFrom
        for (int i = 0; i < _capacity; i++)
        {
            if (!_hasPoint[i] || !_visited[i] || i == targetId)
                continue;

            int next = _cameFrom[i];
            nextNode[i] = next;
            direction[i] = Vect2.Normalize(_positions[next] - _positions[i]);
        }

        return new FlowField(nextNode, direction);
    }

    /// <summary>
    /// Releases all resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        Clear();
        _positions = null;
        _weightScales = null;
        _disabled = null;
        _hasPoint = null;
        _connections = null;
        _neighbors = null;
        _gScore = null;
        _fScore = null;
        _cameFrom = null;
        _visited = null;
        _openSetItems = null;
        _openSetPriorities = null;
        _openSetPositions = null;
        _queue = null;
    }

    #region Internal Pathfinding

    private List<int> FindPath(int fromId, int toId, bool allowPartialPath,
        DiagonalMode? diagonalMode, Heuristic? heuristic, PathAlgorithm? algorithm)
    {
        var diagMode = diagonalMode ?? DefaultDiagonalMode;
        var heur = heuristic ?? DefaultHeuristic;
        var algo = algorithm ?? DefaultAlgorithm;

        if (algo == PathAlgorithm.BFS)
            return FindPathBFS(fromId, toId, allowPartialPath, diagMode, heur);

        return FindPathWeighted(fromId, toId, allowPartialPath, diagMode, heur, algo);
    }

    private List<int> FindPathBFS(int fromId, int toId, bool allowPartialPath,
        DiagonalMode diagMode, Heuristic heur)
    {
        Array.Clear(_visited, 0, _capacity);
        Array.Fill(_cameFrom, -1, 0, _capacity);

        _queueHead = 0;
        _queueTail = 0;
        _queue[_queueTail++] = fromId;
        _visited[fromId] = true;

        int closestId = fromId;
        float closestDist = EstimateCost(fromId, toId, heur);

        while (_queueHead < _queueTail)
        {
            int current = _queue[_queueHead++];

            float dist = EstimateCost(current, toId, heur);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestId = current;
            }

            if (current == toId)
                return ReconstructPath(fromId, toId);

            if (_neighbors[current] == null)
                continue;

            foreach (var neighborId in _neighbors[current])
            {
                if (_visited[neighborId] || _disabled[neighborId])
                    continue;

                if (!CanMove(current, neighborId, diagMode))
                    continue;

                if (ShouldFilterNeighbor(current, neighborId))
                    continue;

                _visited[neighborId] = true;
                _cameFrom[neighborId] = current;
                _queue[_queueTail++] = neighborId;
            }
        }

        if (allowPartialPath && closestId != fromId)
            return ReconstructPath(fromId, closestId);

        return null;
    }

    private List<int> FindPathWeighted(int fromId, int toId, bool allowPartialPath,
        DiagonalMode diagMode, Heuristic heur, PathAlgorithm algo)
    {
        Array.Clear(_visited, 0, _capacity);
        Array.Fill(_gScore, float.MaxValue, 0, _capacity);
        Array.Fill(_fScore, float.MaxValue, 0, _capacity);
        Array.Fill(_cameFrom, -1, 0, _capacity);
        ClearOpenSet();

        _gScore[fromId] = 0f;
        _fScore[fromId] = EstimateCost(fromId, toId, heur);
        PushOpenSet(fromId, _fScore[fromId]);

        int closestId = fromId;
        float closestHeuristic = EstimateCost(fromId, toId, heur);

        while (_openSetCount > 0)
        {
            int current = PopOpenSet();

            if (_visited[current])
                continue;

            _visited[current] = true;

            float h = EstimateCost(current, toId, heur);
            if (h < closestHeuristic)
            {
                closestHeuristic = h;
                closestId = current;
            }

            if (current == toId)
                return ReconstructPath(fromId, toId);

            if (_neighbors[current] == null)
                continue;

            foreach (var neighborId in _neighbors[current])
            {
                if (_visited[neighborId] || _disabled[neighborId])
                    continue;

                if (!CanMove(current, neighborId, diagMode))
                    continue;

                if (ShouldFilterNeighbor(current, neighborId))
                    continue;

                float cost = ComputeCost(current, neighborId);
                float newG = _gScore[current] + cost;

                if (newG < _gScore[neighborId])
                {
                    _gScore[neighborId] = newG;
                    _cameFrom[neighborId] = current;

                    float priority;
                    switch (algo)
                    {
                        case PathAlgorithm.Dijkstra:
                            priority = newG;
                            break;
                        case PathAlgorithm.GreedyBestFirst:
                            priority = EstimateCost(neighborId, toId, heur);
                            break;
                        default: // AStar
                            priority = newG + EstimateCost(neighborId, toId, heur);
                            break;
                    }

                    _fScore[neighborId] = priority;
                    PushOpenSet(neighborId, priority);
                }
            }
        }

        if (allowPartialPath && closestId != fromId)
            return ReconstructPath(fromId, closestId);

        return null;
    }

    private List<int> ReconstructPath(int fromId, int toId)
    {
        var path = new List<int>();
        int current = toId;

        while (current != -1 && current != fromId)
        {
            path.Add(current);
            current = _cameFrom[current];
        }

        path.Add(fromId);
        path.Reverse();
        return path;
    }

    private float ComputeCost(int fromId, int toId)
    {
        if (ComputeCostOverride != null)
            return ComputeCostOverride(fromId, toId);

        return Vect2.Distance(_positions[fromId], _positions[toId]) * _weightScales[toId];
    }

    private float EstimateCost(int fromId, int toId, Heuristic heuristic)
    {
        if (EstimateCostOverride != null)
            return EstimateCostOverride(fromId, toId);

        Vect2 from = _positions[fromId];
        Vect2 to = _positions[toId];
        float dx = MathF.Abs(from.X - to.X);
        float dy = MathF.Abs(from.Y - to.Y);

        return heuristic switch
        {
            Heuristic.None => 0f,
            Heuristic.Manhattan => dx + dy,
            Heuristic.Euclidean => MathF.Sqrt(dx * dx + dy * dy),
            Heuristic.Octile => MathF.Max(dx, dy) + (Sqrt2 - 1f) * MathF.Min(dx, dy),
            Heuristic.Chebyshev => MathF.Max(dx, dy),
            _ => 0f
        };
    }

    private bool ShouldFilterNeighbor(int fromId, int neighborId)
    {
        if (!NeighborFilterEnabled || FilterNeighborOverride == null)
            return false;
        return FilterNeighborOverride(fromId, neighborId);
    }

    private bool CanMove(int fromId, int toId, DiagonalMode mode)
    {
        if (mode == DiagonalMode.Always)
            return true;

        Vect2 from = _positions[fromId];
        Vect2 to = _positions[toId];

        bool isDiagonal = from.X != to.X && from.Y != to.Y;

        if (!isDiagonal)
            return true;

        if (mode == DiagonalMode.Never)
            return false;

        Vect2 corner1 = new(to.X, from.Y);
        Vect2 corner2 = new(from.X, to.Y);

        int corner1Id = FindPointAt(corner1);
        int corner2Id = FindPointAt(corner2);

        if (mode == DiagonalMode.AtLeastOneWalkable)
        {
            return IsWalkable(corner1Id) || IsWalkable(corner2Id);
        }

        if (mode == DiagonalMode.OnlyIfNoObstacles)
        {
            return IsWalkable(corner1Id) && IsWalkable(corner2Id);
        }

        return true;
    }

    private int FindPointAt(Vect2 position)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (_hasPoint[i] && _positions[i] == position)
                return i;
        }
        return -1;
    }

    private bool IsWalkable(int id) =>
        id != -1 && !_disabled[id];

    private static Vect2 ClosestPointOnSegment(Vect2 point, Vect2 a, Vect2 b)
    {
        Vect2 ab = b - a;
        float t = Vect2.Dot(point - a, ab) / Vect2.Dot(ab, ab);
        t = Math.Clamp(t, 0f, 1f);
        return a + ab * t;
    }

    #endregion

    #region Binary Heap

    private void ClearOpenSet()
    {
        for (int i = 0; i < _openSetCount; i++)
            _openSetPositions[_openSetItems[i]] = -1;
        _openSetCount = 0;
    }

    private void PushOpenSet(int item, float priority)
    {
        if (_openSetPositions[item] != -1)
        {
            UpdateOpenSet(item, priority);
            return;
        }

        _openSetItems[_openSetCount] = item;
        _openSetPriorities[_openSetCount] = priority;
        _openSetPositions[item] = _openSetCount;
        _openSetCount++;
        BubbleUp(_openSetCount - 1);
    }

    private int PopOpenSet()
    {
        int result = _openSetItems[0];
        _openSetCount--;

        if (_openSetCount > 0)
        {
            _openSetItems[0] = _openSetItems[_openSetCount];
            _openSetPriorities[0] = _openSetPriorities[_openSetCount];
            _openSetPositions[_openSetItems[0]] = 0;
            BubbleDown(0);
        }

        _openSetPositions[result] = -1;
        return result;
    }

    private void UpdateOpenSet(int item, float newPriority)
    {
        int index = _openSetPositions[item];
        float oldPriority = _openSetPriorities[index];
        _openSetPriorities[index] = newPriority;

        if (newPriority < oldPriority)
            BubbleUp(index);
        else
            BubbleDown(index);
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_openSetPriorities[index] >= _openSetPriorities[parent])
                break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void BubbleDown(int index)
    {
        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left < _openSetCount && _openSetPriorities[left] < _openSetPriorities[smallest])
                smallest = left;
            if (right < _openSetCount && _openSetPriorities[right] < _openSetPriorities[smallest])
                smallest = right;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        int tempItem = _openSetItems[a];
        float tempPriority = _openSetPriorities[a];

        _openSetItems[a] = _openSetItems[b];
        _openSetPriorities[a] = _openSetPriorities[b];

        _openSetItems[b] = tempItem;
        _openSetPriorities[b] = tempPriority;

        _openSetPositions[_openSetItems[a]] = a;
        _openSetPositions[_openSetItems[b]] = b;
    }

    #endregion

    #region Connection Helpers

    private void AddConnection(int id, int toId)
    {
        _connections[id] ??= new Dictionary<int, float>();
        _neighbors[id] ??= new List<int>();

        if (!_connections[id].ContainsKey(toId))
        {
            _connections[id][toId] = 0f;
            _neighbors[id].Add(toId);
        }
    }

    private void RemoveConnection(int id, int toId)
    {
        _connections[id]?.Remove(toId);
        _neighbors[id]?.Remove(toId);
    }

    #endregion
}