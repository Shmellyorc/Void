// ============================================================================
//  AStar2D.cs
// ============================================================================
//  2D graph pathfinding with A*, Dijkstra, BFS, Greedy Best-First, and flow fields.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Engine.Pathfinding;

/// <summary>
/// Defines how geometrically diagonal connections are handled during pathfinding.
/// </summary>
public enum DiagonalMode
{
    /// <summary>
    /// Allows diagonal connections without checking adjacent corner points.
    /// </summary>
    Always,

    /// <summary>
    /// Rejects connections whose endpoints differ on both axes.
    /// </summary>
    Never,

    /// <summary>
    /// Allows a diagonal connection when at least one adjacent corner point exists and is enabled.
    /// </summary>
    AtLeastOneWalkable,

    /// <summary>
    /// Allows a diagonal connection only when both adjacent corner points exist and are enabled.
    /// </summary>
    OnlyIfNoObstacles
}

/// <summary>
/// Defines the built-in distance estimate used by pathfinding.
/// </summary>
public enum Heuristic
{
    /// <summary>
    /// Returns zero for every estimate.
    /// </summary>
    None,

    /// <summary>
    /// Uses Manhattan distance: <c>|dx| + |dy|</c>.
    /// </summary>
    Manhattan,

    /// <summary>
    /// Uses straight-line Euclidean distance.
    /// </summary>
    Euclidean,

    /// <summary>
    /// Uses octile distance for movement that commonly mixes orthogonal and diagonal steps.
    /// </summary>
    Octile,

    /// <summary>
    /// Uses Chebyshev distance: <c>max(|dx|, |dy|)</c>.
    /// </summary>
    Chebyshev
}

/// <summary>
/// Defines the search algorithm used for a path query.
/// </summary>
public enum PathAlgorithm
{
    /// <summary>
    /// Uses accumulated path cost plus the selected heuristic.
    /// </summary>
    AStar,

    /// <summary>
    /// Uses accumulated path cost only.
    /// </summary>
    Dijkstra,

    /// <summary>
    /// Traverses by edge count and ignores edge weights.
    /// </summary>
    BFS,

    /// <summary>
    /// Prioritizes the heuristic estimate and does not guarantee an optimal path.
    /// </summary>
    GreedyBestFirst
}

/// <summary>
/// Stores a 2D navigation graph and performs path and flow-field queries over it.
/// </summary>
/// <remarks>
/// <para>
/// Points are identified by non-negative integer IDs. Connections can be directed or
/// bidirectional. The built-in edge cost is the Euclidean distance between connected
/// points multiplied by the destination point's weight scale.
/// </para>
/// <para>
/// Public path methods return an empty list when the requested path cannot be produced.
/// When <c>allowPartialPath</c> is enabled, the returned path may instead end at the
/// reachable point judged closest to the target by the selected heuristic.
/// </para>
/// <para>
/// Diagonal restrictions are based on point positions rather than graph topology alone.
/// A connection is considered diagonal when its endpoints differ on both axes. The
/// corner-checking modes are most useful for grid-like graphs whose corner points exist
/// at the corresponding axis-aligned positions.
/// </para>
/// <example>
/// <code>
/// using var pathfinder = new AStar2D();
///
/// pathfinder.AddPoint(0, new Vect2(0, 0));
/// pathfinder.AddPoint(1, new Vect2(1, 0));
/// pathfinder.AddPoint(2, new Vect2(2, 0));
///
/// pathfinder.ConnectPoints(0, 1);
/// pathfinder.ConnectPoints(1, 2);
///
/// List&lt;int&gt; path = pathfinder.GetIdPath(0, 2);
/// FlowField flow = pathfinder.ComputeFlowField(2);
/// </code>
/// </example>
/// </remarks>
public sealed class AStar2D : IDisposable
{
    private const float Sqrt2 = 1.4142135623730951f;

    private Vect2[] _positions;
    private float[] _weightScales;
    private bool[] _disabled;
    private bool[] _hasPoint;
    private Dictionary<int, float>[] _connections;
    private List<int>[] _neighbors;
    private List<int>[] _incomingNeighbors;
    private int _pointCount;
    private int _capacity;
    private int _nextAvailableId;
    private bool _isDisposed;

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

    /// <summary>
    /// Gets or sets the diagonal movement mode used when a query does not provide one.
    /// </summary>
    public DiagonalMode DefaultDiagonalMode { get; set; } = DiagonalMode.Always;

    /// <summary>
    /// Gets or sets the heuristic used when a query does not provide one.
    /// </summary>
    public Heuristic DefaultHeuristic { get; set; } = Heuristic.Octile;

    /// <summary>
    /// Gets or sets the algorithm used when a query does not provide one.
    /// </summary>
    public PathAlgorithm DefaultAlgorithm { get; set; } = PathAlgorithm.AStar;

    /// <summary>
    /// Gets or sets whether <see cref="FilterNeighborOverride"/> is applied during searches.
    /// </summary>
    public bool NeighborFilterEnabled { get; set; }

    /// <summary>
    /// Gets or sets an optional edge-cost callback.
    /// </summary>
    /// <remarks>
    /// The callback receives the source point ID and destination point ID. Pathfinding
    /// algorithms expect returned costs to be finite and non-negative.
    /// </remarks>
    public Func<int, int, float> ComputeCostOverride { get; set; }

    /// <summary>
    /// Gets or sets an optional heuristic callback.
    /// </summary>
    /// <remarks>
    /// The callback receives the point being estimated and the target point.
    /// </remarks>
    public Func<int, int, float> EstimateCostOverride { get; set; }

    /// <summary>
    /// Gets or sets an optional callback used to reject a candidate outgoing connection.
    /// </summary>
    /// <remarks>
    /// Return <see langword="true"/> to prevent movement from the first point ID to the
    /// second point ID. The callback is used only while <see cref="NeighborFilterEnabled"/>
    /// is <see langword="true"/>.
    /// </remarks>
    public Func<int, int, bool> FilterNeighborOverride { get; set; }

    /// <summary>
    /// Gets the number of points currently stored in the graph.
    /// </summary>
    public int PointCount => _pointCount;

    /// <summary>
    /// Gets the number of point slots currently reserved by the backing arrays.
    /// </summary>
    public int PointCapacity => _capacity;

    /// <summary>
    /// Initializes a pathfinding graph with an optional initial point capacity.
    /// </summary>
    /// <param name="initialCapacity">The number of point slots to reserve initially.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="initialCapacity"/> is negative.
    /// </exception>
    public AStar2D(int initialCapacity = 1024)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative.");

        ReserveSpace(initialCapacity);
    }

    /// <summary>
    /// Ensures the graph can address at least the specified number of point IDs without resizing.
    /// </summary>
    /// <param name="numNodes">The minimum capacity to reserve.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="numNodes"/> is negative.
    /// </exception>
    public void ReserveSpace(int numNodes)
    {
        if (numNodes < 0)
            throw new ArgumentOutOfRangeException(nameof(numNodes), "Reserved node count cannot be negative.");
        if (numNodes <= _capacity)
            return;

        int newCapacity = Math.Max(numNodes, Math.Max(1, _capacity * 2));
        int oldCapacity = _capacity;

        Array.Resize(ref _positions, newCapacity);
        Array.Resize(ref _weightScales, newCapacity);
        Array.Resize(ref _disabled, newCapacity);
        Array.Resize(ref _hasPoint, newCapacity);
        Array.Resize(ref _connections, newCapacity);
        Array.Resize(ref _neighbors, newCapacity);
        Array.Resize(ref _incomingNeighbors, newCapacity);

        Array.Resize(ref _gScore, newCapacity);
        Array.Resize(ref _fScore, newCapacity);
        Array.Resize(ref _cameFrom, newCapacity);
        Array.Resize(ref _visited, newCapacity);
        Array.Resize(ref _openSetItems, newCapacity);
        Array.Resize(ref _openSetPriorities, newCapacity);
        Array.Resize(ref _openSetPositions, newCapacity);
        Array.Resize(ref _queue, newCapacity);

        for (int i = oldCapacity; i < newCapacity; i++)
            _openSetPositions[i] = -1;

        _capacity = newCapacity;
    }

    /// <summary>
    /// Adds a point or updates an existing point with the same ID.
    /// </summary>
    /// <param name="id">The non-negative point ID.</param>
    /// <param name="position">The point position.</param>
    /// <param name="weightScale">
    /// The non-negative finite multiplier applied when this point is the destination of
    /// the built-in edge-cost calculation.
    /// </param>
    /// <remarks>
    /// Updating an existing point preserves its connections and re-enables the point.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="id"/> is negative or <paramref name="weightScale"/>
    /// is negative, NaN, or infinite.
    /// </exception>
    public void AddPoint(int id, Vect2 position, float weightScale = 1.0f)
    {
        if (id < 0)
            throw new ArgumentOutOfRangeException(nameof(id), "ID must be 0 or larger.");
        ValidateWeightScale(weightScale);

        if (id >= _capacity)
            ReserveSpace(id + 1);

        if (!_hasPoint[id])
        {
            _hasPoint[id] = true;
            _pointCount++;

            if (id == _nextAvailableId)
            {
                while (_nextAvailableId < _capacity && _hasPoint[_nextAvailableId])
                    _nextAvailableId++;
            }
        }

        _positions[id] = position;
        _weightScales[id] = weightScale;
        _disabled[id] = false;
        _connections[id] ??= new Dictionary<int, float>();
        _neighbors[id] ??= new List<int>();
        _incomingNeighbors[id] ??= new List<int>();
    }

    /// <summary>
    /// Removes a point and every incoming and outgoing connection attached to it.
    /// </summary>
    /// <param name="id">The point ID to remove.</param>
    public void RemovePoint(int id)
    {
        if (!HasPoint(id))
            return;

        if (_neighbors[id] != null)
        {
            while (_neighbors[id].Count > 0)
                RemoveConnection(id, _neighbors[id][^1]);
        }

        if (_incomingNeighbors[id] != null)
        {
            while (_incomingNeighbors[id].Count > 0)
                RemoveConnection(_incomingNeighbors[id][^1], id);
        }

        _hasPoint[id] = false;
        _disabled[id] = false;
        _connections[id]?.Clear();
        _neighbors[id]?.Clear();
        _incomingNeighbors[id]?.Clear();
        _pointCount--;
        _nextAvailableId = Math.Min(_nextAvailableId, id);
    }

    /// <summary>
    /// Removes all points and connections while keeping the allocated capacity.
    /// </summary>
    public void Clear()
    {
        if (_isDisposed)
            return;

        for (int i = 0; i < _capacity; i++)
        {
            _hasPoint[i] = false;
            _disabled[i] = false;
            _connections[i]?.Clear();
            _neighbors[i]?.Clear();
            _incomingNeighbors[i]?.Clear();
        }

        ClearOpenSet();
        _pointCount = 0;
        _nextAvailableId = 0;
    }

    /// <summary>
    /// Determines whether the specified point ID exists in the graph.
    /// </summary>
    /// <param name="id">The point ID to test.</param>
    /// <returns><see langword="true"/> when the point exists; otherwise, <see langword="false"/>.</returns>
    public bool HasPoint(int id) =>
        !_isDisposed && id >= 0 && id < _capacity && _hasPoint[id];

    /// <summary>
    /// Gets the lowest currently unused non-negative point ID.
    /// </summary>
    /// <returns>
    /// The lowest unused ID. The returned value can equal <see cref="PointCapacity"/>;
    /// adding that ID automatically grows the graph.
    /// </returns>
    public int GetAvailablePointId()
    {
        if (_isDisposed)
            return 0;

        while (_nextAvailableId < _capacity && _hasPoint[_nextAvailableId])
            _nextAvailableId++;

        return _nextAvailableId;
    }

    /// <summary>
    /// Connects two existing points.
    /// </summary>
    /// <param name="id">The source point ID.</param>
    /// <param name="toId">The destination point ID.</param>
    /// <param name="bidirectional">
    /// Whether to also create the reverse connection from <paramref name="toId"/> to <paramref name="id"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when either point does not exist.
    /// </exception>
    public void ConnectPoints(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            throw new ArgumentException("Both points must exist.");

        AddConnection(id, toId);

        if (bidirectional)
            AddConnection(toId, id);
    }

    /// <summary>
    /// Disconnects two points when they exist.
    /// </summary>
    /// <param name="id">The source point ID.</param>
    /// <param name="toId">The destination point ID.</param>
    /// <param name="bidirectional">
    /// Whether to also remove the reverse connection from <paramref name="toId"/> to <paramref name="id"/>.
    /// </param>
    public void DisconnectPoints(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            return;

        RemoveConnection(id, toId);

        if (bidirectional)
            RemoveConnection(toId, id);
    }

    /// <summary>
    /// Determines whether the requested connection exists.
    /// </summary>
    /// <param name="id">The source point ID.</param>
    /// <param name="toId">The destination point ID.</param>
    /// <param name="bidirectional">
    /// When <see langword="true"/>, both directions must exist. When
    /// <see langword="false"/>, only <paramref name="id"/> to <paramref name="toId"/> is checked.
    /// </param>
    /// <returns><see langword="true"/> when the requested connection exists.</returns>
    public bool ArePointsConnected(int id, int toId, bool bidirectional = true)
    {
        if (!HasPoint(id) || !HasPoint(toId))
            return false;

        bool forward = _connections[id]?.ContainsKey(toId) ?? false;

        if (!bidirectional)
            return forward;

        bool reverse = _connections[toId]?.ContainsKey(id) ?? false;
        return forward && reverse;
    }

    /// <summary>
    /// Gets a copy of the outgoing connection IDs for a point.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <returns>The outgoing destination IDs, or an empty list when the point does not exist.</returns>
    public List<int> GetPointConnections(int id)
    {
        if (!HasPoint(id) || _neighbors[id] == null)
            return [];

        return new List<int>(_neighbors[id]);
    }

    /// <summary>
    /// Gets all point IDs currently stored in the graph.
    /// </summary>
    /// <returns>A list of existing point IDs in ascending numeric order.</returns>
    public List<int> GetPointIds()
    {
        if (_isDisposed)
            return [];

        var result = new List<int>(_pointCount);

        for (int i = 0; i < _capacity; i++)
        {
            if (_hasPoint[i])
                result.Add(i);
        }

        return result;
    }

    /// <summary>
    /// Updates the position of an existing point.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <param name="position">The new position.</param>
    public void SetPointPosition(int id, Vect2 position)
    {
        if (!HasPoint(id))
            return;

        _positions[id] = position;
    }

    /// <summary>
    /// Gets the position of a point.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <returns>The stored position, or <see cref="Vect2.Zero"/> when the point does not exist.</returns>
    public Vect2 GetPointPosition(int id) =>
        HasPoint(id) ? _positions[id] : Vect2.Zero;

    /// <summary>
    /// Updates the weight scale of an existing point.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <param name="weightScale">The new non-negative finite weight scale.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="weightScale"/> is negative, NaN, or infinite.
    /// </exception>
    public void SetPointWeightScale(int id, float weightScale)
    {
        ValidateWeightScale(weightScale);

        if (!HasPoint(id))
            return;

        _weightScales[id] = weightScale;
    }

    /// <summary>
    /// Gets the weight scale of a point.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <returns>The stored weight scale, or <c>0</c> when the point does not exist.</returns>
    public float GetPointWeightScale(int id) =>
        HasPoint(id) ? _weightScales[id] : 0f;

    /// <summary>
    /// Enables or disables a point for pathfinding.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <param name="disabled"><see langword="true"/> to disable the point.</param>
    public void SetPointDisabled(int id, bool disabled = true)
    {
        if (!HasPoint(id))
            return;

        _disabled[id] = disabled;
    }

    /// <summary>
    /// Determines whether an existing point is disabled.
    /// </summary>
    /// <param name="id">The point ID.</param>
    /// <returns><see langword="true"/> only when the point exists and is disabled.</returns>
    public bool IsPointDisabled(int id) =>
        HasPoint(id) && _disabled[id];

    /// <summary>
    /// Finds the point nearest to a position.
    /// </summary>
    /// <param name="position">The position to search around.</param>
    /// <param name="includeDisabled">Whether disabled points are eligible.</param>
    /// <returns>The nearest point ID, or <c>-1</c> when no eligible point exists.</returns>
    public int GetClosestPoint(Vect2 position, bool includeDisabled = false)
    {
        if (_isDisposed)
            return -1;

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
    /// Finds the nearest position lying on any connected graph segment.
    /// </summary>
    /// <param name="position">The position to project onto graph connections.</param>
    /// <returns>
    /// The nearest point on a connected segment, or <paramref name="position"/> when the graph has no connections.
    /// </returns>
    /// <remarks>
    /// Directed connections are considered as geometric segments as well. A bidirectional
    /// connection may therefore be examined twice, which does not change the returned position.
    /// </remarks>
    public Vect2 GetClosestPositionInSegment(Vect2 position)
    {
        if (_isDisposed)
            return position;

        Vect2 closestPoint = position;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _capacity; i++)
        {
            if (!_hasPoint[i] || _neighbors[i] == null)
                continue;

            foreach (int neighborId in _neighbors[i])
            {
                if (!HasPoint(neighborId))
                    continue;

                Vect2 point = ClosestPointOnSegment(position, _positions[i], _positions[neighborId]);
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
    /// Finds a path and returns its point IDs.
    /// </summary>
    /// <param name="fromId">The starting point ID.</param>
    /// <param name="toId">The target point ID.</param>
    /// <param name="allowPartialPath">
    /// Whether an unreachable target may return a path to the reachable point closest to it.
    /// </param>
    /// <param name="diagonalMode">Optional diagonal mode override for this query.</param>
    /// <param name="heuristic">Optional heuristic override for this query.</param>
    /// <param name="algorithm">Optional algorithm override for this query.</param>
    /// <returns>
    /// A path containing both the start and final point IDs. Returns an empty list when
    /// the path cannot be produced. When start and target are the same enabled point,
    /// the returned list contains that single ID.
    /// </returns>
    public List<int> GetIdPath(int fromId, int toId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
    {
        if (!HasPoint(fromId) || !HasPoint(toId))
            return [];
        if (_disabled[fromId])
            return [];
        if (fromId == toId)
            return [fromId];

        return FindPath(fromId, toId, allowPartialPath, diagonalMode, heuristic, algorithm);
    }

    /// <summary>
    /// Finds a path and returns the stored positions of its points.
    /// </summary>
    /// <param name="fromId">The starting point ID.</param>
    /// <param name="toId">The target point ID.</param>
    /// <param name="allowPartialPath">
    /// Whether an unreachable target may return a path to the reachable point closest to it.
    /// </param>
    /// <param name="diagonalMode">Optional diagonal mode override for this query.</param>
    /// <param name="heuristic">Optional heuristic override for this query.</param>
    /// <param name="algorithm">Optional algorithm override for this query.</param>
    /// <returns>
    /// The path positions, including the start and final point. Returns an empty list when
    /// the path cannot be produced.
    /// </returns>
    public List<Vect2> GetPointPath(int fromId, int toId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
    {
        List<int> idPath = GetIdPath(fromId, toId, allowPartialPath, diagonalMode, heuristic, algorithm);

        if (idPath.Count == 0)
            return [];

        var result = new List<Vect2>(idPath.Count);

        foreach (int id in idPath)
            result.Add(_positions[id]);

        return result;
    }

    /// <summary>
    /// Finds a path and returns the stored positions of its points.
    /// </summary>
    /// <param name="startId">The starting point ID.</param>
    /// <param name="endId">The target point ID.</param>
    /// <param name="allowPartialPath">
    /// Whether an unreachable target may return a path to the reachable point closest to it.
    /// </param>
    /// <param name="diagonalMode">Optional diagonal mode override for this query.</param>
    /// <param name="heuristic">Optional heuristic override for this query.</param>
    /// <param name="algorithm">Optional algorithm override for this query.</param>
    /// <returns>
    /// The path positions, including the start and final point. Returns an empty list when
    /// the path cannot be produced.
    /// </returns>
    public List<Vect2> GetPath(int startId, int endId, bool allowPartialPath = false,
        DiagonalMode? diagonalMode = null, Heuristic? heuristic = null, PathAlgorithm? algorithm = null)
        => GetPointPath(startId, endId, allowPartialPath, diagonalMode, heuristic, algorithm);

    /// <summary>
    /// Computes next-step guidance for points that can reach a target.
    /// </summary>
    /// <param name="targetId">The target point ID.</param>
    /// <returns>
    /// A flow field for reachable non-target points. An invalid or disabled target
    /// produces an empty flow field.
    /// </returns>
    /// <remarks>
    /// Flow-field traversal follows incoming connections so directed graphs are handled
    /// correctly. It uses <see cref="DefaultDiagonalMode"/>, the current point-disabled
    /// state, <see cref="ComputeCostOverride"/>, and the neighbor filter when enabled.
    /// </remarks>
    public FlowField ComputeFlowField(int targetId)
    {
        if (!HasPoint(targetId) || _disabled[targetId])
            return CreateEmptyFlowField();

        var nextNode = new Dictionary<int, int>(_pointCount);
        var direction = new Dictionary<int, Vect2>(_pointCount);

        Array.Clear(_visited, 0, _capacity);
        Array.Fill(_gScore, float.MaxValue, 0, _capacity);
        Array.Fill(_cameFrom, -1, 0, _capacity);
        ClearOpenSet();

        _gScore[targetId] = 0f;
        PushOpenSet(targetId, 0f);

        while (_openSetCount > 0)
        {
            int current = PopOpenSet();

            if (_visited[current])
                continue;

            _visited[current] = true;

            if (_incomingNeighbors[current] == null)
                continue;

            foreach (int fromId in _incomingNeighbors[current])
            {
                if (_visited[fromId] || !_hasPoint[fromId] || _disabled[fromId])
                    continue;
                if (!CanMove(fromId, current, DefaultDiagonalMode))
                    continue;
                if (ShouldFilterNeighbor(fromId, current))
                    continue;

                float newCost = _gScore[current] + ComputeCost(fromId, current);

                if (newCost >= _gScore[fromId])
                    continue;

                _gScore[fromId] = newCost;
                _cameFrom[fromId] = current;
                PushOpenSet(fromId, newCost);
            }
        }

        for (int i = 0; i < _capacity; i++)
        {
            if (!_hasPoint[i] || !_visited[i] || i == targetId)
                continue;

            int next = _cameFrom[i];

            if (next < 0 || !HasPoint(next))
                continue;

            nextNode[i] = next;

            Vect2 delta = _positions[next] - _positions[i];
            direction[i] = delta.IsZero ? Vect2.Zero : Vect2.Normalize(delta);
        }

        return new FlowField(nextNode, direction);
    }

    /// <summary>
    /// Releases the graph's retained storage.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent. The instance should not be used for pathfinding after disposal.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        Clear();
        _isDisposed = true;

        _positions = null;
        _weightScales = null;
        _disabled = null;
        _hasPoint = null;
        _connections = null;
        _neighbors = null;
        _incomingNeighbors = null;
        _gScore = null;
        _fScore = null;
        _cameFrom = null;
        _visited = null;
        _openSetItems = null;
        _openSetPriorities = null;
        _openSetPositions = null;
        _queue = null;

        _capacity = 0;
        _pointCount = 0;
        _nextAvailableId = 0;
    }

    private List<int> FindPath(int fromId, int toId, bool allowPartialPath,
        DiagonalMode? diagonalMode, Heuristic? heuristic, PathAlgorithm? algorithm)
    {
        DiagonalMode diagMode = diagonalMode ?? DefaultDiagonalMode;
        Heuristic heur = heuristic ?? DefaultHeuristic;
        PathAlgorithm algo = algorithm ?? DefaultAlgorithm;

        return algo == PathAlgorithm.BFS
            ? FindPathBFS(fromId, toId, allowPartialPath, diagMode, heur)
            : FindPathWeighted(fromId, toId, allowPartialPath, diagMode, heur, algo);
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

            foreach (int neighborId in _neighbors[current])
            {
                if (_visited[neighborId] || !_hasPoint[neighborId] || _disabled[neighborId])
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

        return [];
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

            foreach (int neighborId in _neighbors[current])
            {
                if (_visited[neighborId] || !_hasPoint[neighborId] || _disabled[neighborId])
                    continue;
                if (!CanMove(current, neighborId, diagMode))
                    continue;
                if (ShouldFilterNeighbor(current, neighborId))
                    continue;

                float newG = _gScore[current] + ComputeCost(current, neighborId);

                if (newG >= _gScore[neighborId])
                    continue;

                _gScore[neighborId] = newG;
                _cameFrom[neighborId] = current;

                float priority = algo switch
                {
                    PathAlgorithm.Dijkstra => newG,
                    PathAlgorithm.GreedyBestFirst => EstimateCost(neighborId, toId, heur),
                    _ => newG + EstimateCost(neighborId, toId, heur)
                };

                _fScore[neighborId] = priority;
                PushOpenSet(neighborId, priority);
            }
        }

        if (allowPartialPath && closestId != fromId)
            return ReconstructPath(fromId, closestId);

        return [];
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

        if (current != fromId)
            return [];

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

        int corner1Id = FindPointAt(new Vect2(to.X, from.Y));
        int corner2Id = FindPointAt(new Vect2(from.X, to.Y));

        return mode switch
        {
            DiagonalMode.AtLeastOneWalkable => IsWalkable(corner1Id) || IsWalkable(corner2Id),
            DiagonalMode.OnlyIfNoObstacles => IsWalkable(corner1Id) && IsWalkable(corner2Id),
            _ => true
        };
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
        id >= 0 && id < _capacity && _hasPoint[id] && !_disabled[id];

    private static Vect2 ClosestPointOnSegment(Vect2 point, Vect2 a, Vect2 b)
    {
        Vect2 ab = b - a;

        if (ab.IsZero)
            return a;

        float denominator = Vect2.Dot(ab, ab);
        float t = Vect2.Dot(point - a, ab) / denominator;
        t = Math.Clamp(t, 0f, 1f);

        return a + ab * t;
    }

    private void ClearOpenSet()
    {
        if (_openSetItems == null || _openSetPositions == null)
        {
            _openSetCount = 0;
            return;
        }

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

    private void AddConnection(int id, int toId)
    {
        _connections[id] ??= new Dictionary<int, float>();
        _neighbors[id] ??= new List<int>();
        _incomingNeighbors[toId] ??= new List<int>();

        if (_connections[id].ContainsKey(toId))
            return;

        _connections[id][toId] = 0f;
        _neighbors[id].Add(toId);
        _incomingNeighbors[toId].Add(id);
    }

    private void RemoveConnection(int id, int toId)
    {
        if (_connections[id]?.Remove(toId) != true)
            return;

        _neighbors[id]?.Remove(toId);
        _incomingNeighbors[toId]?.Remove(id);
    }

    private static void ValidateWeightScale(float weightScale)
    {
        if (weightScale < 0f || !float.IsFinite(weightScale))
            throw new ArgumentOutOfRangeException(nameof(weightScale), "Weight scale must be finite and 0 or greater.");
    }

    private static FlowField CreateEmptyFlowField() =>
        new(new Dictionary<int, int>(), new Dictionary<int, Vect2>());
}
