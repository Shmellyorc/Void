// ============================================================================
//  FlowField.cs
// ============================================================================
//  Read-only next-step guidance for moving through a graph toward one target.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Collections.Generic;

namespace Void.Engine.Pathfinding;

/// <summary>
/// Provides precomputed next-node and direction lookups toward a pathfinding target.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created by <see cref="AStar2D.ComputeFlowField(int)"/>. A node is
/// included only when it can reach the target under the graph's directed connections,
/// disabled-point state, diagonal rules, and enabled neighbor filter at computation time.
/// The target itself is not stored because it has no next step.
/// </para>
/// <para>
/// Recompute the field after changing graph connections, relevant point positions,
/// point weights, disabled states, or search overrides that affect traversal costs.
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
    /// Gets the next point to visit from a node.
    /// </summary>
    /// <param name="currentNodeId">The current point ID.</param>
    /// <returns>
    /// The next point ID toward the target, or <c>-1</c> when the node has no flow-field entry.
    /// </returns>
    public int GetNextNode(int currentNodeId) =>
        _nextNode.TryGetValue(currentNodeId, out int next) ? next : -1;

    /// <summary>
    /// Gets the normalized direction from a node toward its next flow-field point.
    /// </summary>
    /// <param name="currentNodeId">The current point ID.</param>
    /// <returns>
    /// The direction toward the next point, or <see cref="Vect2.Zero"/> when the node
    /// has no entry or the current and next points share the same position.
    /// </returns>
    public Vect2 GetDirection(int currentNodeId) =>
        _direction.TryGetValue(currentNodeId, out Vect2 direction) ? direction : Vect2.Zero;

    /// <summary>
    /// Determines whether a node has a next-step entry in this flow field.
    /// </summary>
    /// <param name="currentNodeId">The point ID to test.</param>
    /// <returns><see langword="true"/> when next-step data exists for the node.</returns>
    public bool HasNode(int currentNodeId) =>
        _nextNode.ContainsKey(currentNodeId);

    /// <summary>
    /// Gets the number of non-target nodes that have next-step data.
    /// </summary>
    public int Count => _nextNode.Count;
}
