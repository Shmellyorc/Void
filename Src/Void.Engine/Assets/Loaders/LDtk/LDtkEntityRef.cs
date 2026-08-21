// ============================================================================
//  LDtkEntityRef.cs
// ============================================================================
//  Represents a reference to an entity instance within an LDtk project.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Represents a reference to an entity instance within an LDtk project.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkEntityRef"/> structure provides a way to reference an
/// entity instance by its ID, along with the IDs of the containing layer,
/// level, and world. This is used in LDtk settings that reference other
/// entities.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="EntityId"/> - The ID of the referenced entity instance</description></item>
///   <item><description><see cref="LayerId"/> - The ID of the layer containing the entity</description></item>
///   <item><description><see cref="LevelId"/> - The ID of the level containing the entity</description></item>
///   <item><description><see cref="WorldId"/> - The ID of the world containing the entity</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get an entity reference from a setting
/// var entityRef = LDtkSetting.GetEntityRefSetting(settings, "TargetEntity");
/// 
/// // Access the entity reference properties
/// string entityId = entityRef.EntityId;
/// string layerId = entityRef.LayerId;
/// string levelId = entityRef.LevelId;
/// 
/// // Get the referenced entity from the map
/// if (map.TryGetEntityById(entityRef.EntityId, out var entity))
/// {
///     // Use the entity
/// }
/// 
/// // Check if the entity reference is valid
/// if (!string.IsNullOrEmpty(entityRef.EntityId))
/// {
///     // Entity reference is valid
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe.
/// </para>
/// </remarks>
public readonly struct LDtkEntityRef
{
    /// <summary>
    /// Gets the ID of the referenced entity instance.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Gets the ID of the layer containing the entity.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the ID of the level containing the entity.
    /// </summary>
    public string LevelId { get; }

    /// <summary>
    /// Gets the ID of the world containing the entity.
    /// </summary>
    public string WorldId { get; }

    internal LDtkEntityRef(string entityId, string layerId, string levelId, string worldId)
    {
        EntityId = entityId;
        LayerId = layerId;
        LevelId = levelId;
        WorldId = worldId;
    }

    internal static LDtkEntityRef Process(JsonElement e)
    {
        var entityId = e.GetPropertyOrDefault("entityIid", string.Empty);
        var layerId = e.GetPropertyOrDefault("layerIid", string.Empty);
        var levelId = e.GetPropertyOrDefault("levelIid", string.Empty);
        var worldId = e.GetPropertyOrDefault("worldIid", string.Empty);

        return new LDtkEntityRef(entityId, layerId, levelId, worldId);
    }
}