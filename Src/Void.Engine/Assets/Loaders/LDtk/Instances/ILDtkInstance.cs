// ============================================================================
//  ILDtkInstance.cs
// ============================================================================
//  Interface for all LDtk instance types (entities, tiles, int grid).
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Interface for all LDtk instance types including entities, tiles, and integer grid values.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ILDtkInstance"/> interface is implemented by all LDtk instance
/// types that can appear in a <see cref="MapLayer"/>. It provides common
/// properties for positioning instances within the level.
/// </para>
/// <para>
/// <b>Implementations:</b>
/// <list type="bullet">
///   <item><description><see cref="LDtkEntityInstance"/> - Entity instances</description></item>
///   <item><description><see cref="LDtkTileInstance"/> - Tile instances</description></item>
///   <item><description><see cref="LDtkIntGridInstance"/> - Integer grid values</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Location"/> - The grid location of the instance</description></item>
///   <item><description><see cref="Position"/> - The pixel position of the instance</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get instances from a layer
/// var instances = layer.Instances;
/// 
/// // Iterate over instances
/// foreach (var instance in instances)
/// {
///     Vect2 gridPos = instance.Location;
///     Vect2 pixelPos = instance.Position;
///     
///     // Handle based on type
///     if (instance is LDtkEntityInstance entity)
///     {
///         // Handle entity
///     }
///     else if (instance is LDtkTileInstance tile)
///     {
///         // Handle tile
///     }
/// }
/// 
/// // Filter instances by type using InstanceAs
/// var entities = layer.InstanceAs&lt;LDtkEntityInstance&gt;();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This interface is thread-safe when used in a read-only manner.
/// </para>
/// </remarks>
public interface ILDtkInstance
{
    /// <summary>
    /// Gets the grid location of the instance in tile coordinates.
    /// </summary>
    Vect2 Location { get; }

    /// <summary>
    /// Gets the pixel position of the instance in world coordinates.
    /// </summary>
    Vect2 Position { get; }
}