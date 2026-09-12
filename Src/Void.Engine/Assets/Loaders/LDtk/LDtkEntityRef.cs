// ============================================================================
//  LDtkEntityRef.cs
// ============================================================================
//  Entity-instance references parsed from LDtk field values.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk;

/// <summary>
/// Identifies an entity instance and the LDtk containers that own it.
/// </summary>
/// <remarks>
/// Entity-reference fields can point across layers, levels, and worlds. Use
/// <see cref="EntityId"/> with <see cref="LDtkMap.GetEntityById(string)"/> when
/// the referenced entity is part of the loaded map cache.
/// </remarks>
public readonly struct LDtkEntityRef
{
    /// <summary>
    /// Gets the instance ID of the referenced entity.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Gets the instance ID of the layer containing the entity.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the instance ID of the level containing the entity.
    /// </summary>
    public string LevelId { get; }

    /// <summary>
    /// Gets the instance ID of the world containing the entity.
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
