// ============================================================================
//  LDtkEntityInstance.cs
// ============================================================================
//  Represents an entity instance within an LDtk level.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents an entity instance within an LDtk level.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="LDtkEntityInstance"/> class represents a single entity
/// placed in a level. It contains the entity's name, ID, position, size,
/// pivot, tags, and field settings.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Name"/> - The name of the entity</description></item>
///   <item><description><see cref="Id"/> - The unique identifier of the entity instance</description></item>
///   <item><description><see cref="Size"/> - The size of the entity in pixels</description></item>
///   <item><description><see cref="Coords"/> - The world coordinates of the entity</description></item>
///   <item><description><see cref="Pivot"/> - The pivot point of the entity</description></item>
///   <item><description><see cref="Tags"/> - The tags associated with the entity</description></item>
///   <item><description><see cref="Settings"/> - The field settings of the entity</description></item>
///   <item><description><see cref="Location"/> - The grid location of the entity</description></item>
///   <item><description><see cref="Position"/> - The pixel position of the entity</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get all entities from a layer
/// var entities = layer.InstanceAs&lt;LDtkEntityInstance&gt;();
/// 
/// foreach (var entity in entities)
/// {
///     Console.WriteLine($"Entity: {entity.Name} at {entity.Position}");
///     
///     // Get entity tags as enums
///     var tags = entity.TagsAs&lt;MyEntityTag&gt;();
///     
///     // Access entity settings
///     if (LDtkSetting.TryGetIntSetting(entity.Settings, "Health", out int health))
///     {
///         // Use health value
///     }
/// }
/// 
/// // Get a specific entity by ID
/// var entity = map.GetEntityById("entity_id");
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is immutable and thread-safe.
/// </para>
/// </remarks>
public sealed class LDtkEntityInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the name of the entity.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the pivot point of the entity.
    /// </summary>
    public Vect2 Pivot { get; }

    /// <summary>
    /// Gets the unique identifier of the entity instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the size of the entity in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the world coordinates of the entity.
    /// </summary>
    public Vect2 Coords { get; }

    /// <summary>
    /// Gets the tags associated with the entity.
    /// </summary>
    public List<string> Tags { get; }

    /// <summary>
    /// Gets the width of the entity in pixels.
    /// </summary>
    public float Width => Size.X;

    /// <summary>
    /// Gets the height of the entity in pixels.
    /// </summary>
    public float Height => Size.Y;

    /// <summary>
    /// Gets the field settings of the entity.
    /// </summary>
    public Dictionary<uint, LDtkSetting> Settings { get; }

    /// <summary>
    /// Gets the grid location of the entity in tile coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the pixel position of the entity in world coordinates.
    /// </summary>
    public Vect2 Position { get; }

    internal LDtkEntityInstance(string name, Vect2 pivot, string id, Vect2 size,
        Vect2 coords, List<string> tags, Vect2 location, Vect2 position,
        Dictionary<uint, LDtkSetting> settings)
    {
        Name = name;
        Pivot = pivot;
        Id = id;
        Size = size;
        Coords = coords;
        Tags = tags;
        Settings = settings;
        Location = location;
        Position = position;
    }

    /// <summary>
    /// Gets the entity tags as a list of enum values.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to convert tags to.</typeparam>
    /// <returns>A list of enum values parsed from the tags.</returns>
    public List<TEnum> TagsAs<TEnum>() where TEnum : Enum
    {
        var result = new List<TEnum>(Tags.Count);

        for (int i = 0; i < Tags.Count; i++)
        {
            var tag = Tags[i];

            if (!Enum.TryParse(typeof(TEnum), tag, true, out var eResult))
                continue;

            result.Add((TEnum)eResult);
        }

        return result;
    }

    internal static List<ILDtkInstance> Process(JsonElement e)
    {
        var result = new List<ILDtkInstance>(e.GetArrayLength());

        foreach (var t in e.EnumerateArray())
        {
            var name = t.GetPropertyOrDefault("__identifier", string.Empty);
            var location = t.GetPosition("__grid");
            var pivot = t.GetPosition("__pivot");
            var id = t.GetPropertyOrDefault("iid", string.Empty);
            var cX = t.GetPropertyOrDefault<int>("width");
            var cY = t.GetPropertyOrDefault<int>("height");
            var position = t.GetPosition("px");
            var worldX = t.GetPropertyOrDefault<int>("__worldX");
            var worldY = t.GetPropertyOrDefault<int>("__worldY");
            var tags = t.GetProperty("__tags")
                .EnumerateArray()
                .Where(x => x.ValueKind != JsonValueKind.Null)
                .Select(x => x.GetString()!)
                .ToList();

            var settings = JsonHelper.GetSettings(t.GetProperty("fieldInstances"));

            result.Add(new LDtkEntityInstance(name, pivot, id, new(cX, cY),
                new(worldX, worldY), tags, location, position, settings));
        }

        return result;
    }
}