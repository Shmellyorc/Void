// ============================================================================
//  LDtkEntityInstance.cs
// ============================================================================
//  Represents an entity instance placed in an LDtk entity layer.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Represents an entity instance placed in an LDtk entity layer.
/// </summary>
/// <remarks>
/// Entity instances expose their LDtk identifier, dimensions, pivot, tags, field
/// settings, grid location, and pixel position. Use <see cref="LDtkSetting"/> helpers
/// to read values from <see cref="Settings"/>.
/// </remarks>
public sealed class LDtkEntityInstance : ILDtkInstance
{
    /// <summary>
    /// Gets the LDtk entity identifier.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the normalized entity pivot reported by LDtk.
    /// </summary>
    public Vect2 Pivot { get; }

    /// <summary>
    /// Gets the unique LDtk instance identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the entity size in pixels.
    /// </summary>
    public Vect2 Size { get; }

    /// <summary>
    /// Gets the entity world coordinates reported by LDtk.
    /// </summary>
    public Vect2 Coords { get; }

    /// <summary>
    /// Gets the tags assigned to the entity definition.
    /// </summary>
    public List<string> Tags { get; }

    /// <summary>
    /// Gets the entity width in pixels.
    /// </summary>
    public float Width => Size.X;

    /// <summary>
    /// Gets the entity height in pixels.
    /// </summary>
    public float Height => Size.Y;

    /// <summary>
    /// Gets the entity field settings keyed by VOID's hashed field identifiers.
    /// </summary>
    public Dictionary<uint, LDtkSetting> Settings { get; }

    /// <summary>
    /// Gets the entity location in layer grid coordinates.
    /// </summary>
    public Vect2 Location { get; }

    /// <summary>
    /// Gets the entity position in pixels within its level.
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
    /// Parses matching entity tags as values of the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used to interpret tag names.</typeparam>
    /// <returns>
    /// A list containing each tag that could be parsed as <typeparamref name="TEnum"/>.
    /// Tags that do not match an enum value are skipped.
    /// </returns>
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
