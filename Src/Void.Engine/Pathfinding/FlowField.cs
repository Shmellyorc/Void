namespace Void.Engine.Pathfinding;

/// <summary>
/// Represents a computed flow field for pathfinding.
/// Stores the next node and direction for each reachable point toward a target.
/// </summary>
public sealed class FlowField
{
    private readonly Dictionary<int, int> _nextNode;
    private readonly Dictionary<int, Vect2> _direction;

    internal FlowField(Dictionary<int, int> nextNode, Dictionary<int, Vect2> direction)
    {
        _nextNode = nextNode;
        _direction = direction;
    }

    /// <summary>
    /// Gets the next node to move to from the given node ID.
    /// </summary>
    /// <param name="currentNodeId">The current node ID.</param>
    /// <returns>The next node ID toward the target, or -1 if unreachable.</returns>
    public int GetNextNode(int currentNodeId) =>
        _nextNode.TryGetValue(currentNodeId, out int next) ? next : -1;

    /// <summary>
    /// Gets the direction vector from the given node ID toward the target.
    /// </summary>
    /// <param name="currentNodeId">The current node ID.</param>
    /// <returns>The normalized direction, or zero if unreachable.</returns>
    public Vect2 GetDirection(int currentNodeId) =>
        _direction.TryGetValue(currentNodeId, out Vect2 dir) ? dir : Vect2.Zero;

    /// <summary>
    /// Checks if the given node has flow field data.
    /// </summary>
    public bool HasNode(int currentNodeId) =>
        _nextNode.ContainsKey(currentNodeId);

    /// <summary>
    /// Gets the total number of nodes in the flow field.
    /// </summary>
    public int Count => _nextNode.Count;
}