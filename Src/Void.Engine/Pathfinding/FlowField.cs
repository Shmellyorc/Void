// ============================================================================
//  FlowField.cs
// ============================================================================
//  Represents a computed flow field for pathfinding, providing direction
//  guidance and next-node lookup for agents moving toward a target.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections.Generic;

namespace Void.Engine.Pathfinding;

/// <summary>
/// Represents a computed flow field that provides directional guidance and
/// next-node lookup for agents moving toward a target.
/// </summary>
/// <remarks>
/// <para>
/// A flow field is a data structure that stores, for each node in a navigation
/// grid, the optimal direction and next node to move toward a target. It is
/// computed using a combination of Dijkstra's algorithm (for distance-to-target)
/// and a follow-the-gradient approach to generate smooth, natural movement.
/// </para>
/// <para>
/// Flow fields are ideal for scenarios with many agents moving toward the
/// same target, such as real-time strategy games, crowd simulations, or
/// flocking behaviors. The computation is performed once per target change,
/// and all agents can then query the field efficiently.
/// </para>
/// <para>
/// <b>How It Works:</b>
/// <list type="number">
///   <item><description>Dijkstra's algorithm computes the shortest distance from every node to the target</description></item>
///   <item><description>For each node, the algorithm identifies the neighbor with the lowest distance value</description></item>
///   <item><description>The direction is calculated as the normalized vector from the current node to the chosen neighbor</description></item>
///   <item><description>The flow field is stored as two dictionaries: next-node mapping and direction mapping</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a navigation grid
/// var grid = new Grid(100, 100, 1.0f);
/// 
/// // Set obstacles
/// grid.SetObstacle(10, 10, true);
/// 
/// // Compute flow field to a target position
/// var flowField = FlowField.Compute(grid, targetNodeId, 0, 9999);
/// 
/// // Agents query the flow field
/// int nextNode = flowField.GetNextNode(currentNodeId);
/// Vect2 direction = flowField.GetDirection(currentNodeId);
/// 
/// // Check if a node is reachable
/// if (flowField.HasNode(currentNodeId))
/// {
///     // Move agent in the direction of the flow field
///     agent.Move(direction);
/// }
/// </code>
/// </para>
/// <para>
/// <b>Performance Considerations:</b>
/// <list type="bullet">
///   <item><description>Flow field computation is O(N) where N is the number of reachable nodes</description></item>
///   <item><description>Once computed, queries are O(1) dictionary lookups</description></item>
///   <item><description>The flow field should be recomputed when the target changes or the environment changes</description></item>
///   <item><description>For dynamic environments, consider incremental updates or frequent recomputation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe after construction. All methods are read-only.
/// </para>
/// </remarks>
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
    /// Gets the next node to move to from the specified node.
    /// </summary>
    /// <param name="currentNodeId">The ID of the current node.</param>
    /// <returns>The ID of the next node toward the target, or -1 if the node is unreachable or not in the flow field.</returns>
    public int GetNextNode(int currentNodeId) =>
        _nextNode.TryGetValue(currentNodeId, out int next) ? next : -1;

    /// <summary>
    /// Gets the normalized direction vector from the specified node toward the target.
    /// </summary>
    /// <param name="currentNodeId">The ID of the current node.</param>
    /// <returns>A normalized <see cref="Vect2"/> direction toward the target, or <see cref="Vect2.Zero"/> if the node is unreachable or not in the flow field.</returns>
    public Vect2 GetDirection(int currentNodeId) =>
        _direction.TryGetValue(currentNodeId, out Vect2 dir) ? dir : Vect2.Zero;

    /// <summary>
    /// Determines whether the specified node has flow field data.
    /// </summary>
    /// <param name="currentNodeId">The ID of the node to check.</param>
    /// <returns><see langword="true"/> if the node is in the flow field and reachable; otherwise, <see langword="false"/>.</returns>
    public bool HasNode(int currentNodeId) =>
        _nextNode.ContainsKey(currentNodeId);

    /// <summary>
    /// Gets the total number of nodes in the flow field.
    /// </summary>
    public int Count => _nextNode.Count;
}