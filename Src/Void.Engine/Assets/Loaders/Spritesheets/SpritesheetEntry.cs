// ============================================================================
//  SpritesheetEntry.cs
// ============================================================================
//  Represents a single sprite entry within a spritesheet.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.Spritesheets;

/// <summary>
/// Represents a single sprite entry within a spritesheet.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SpritesheetEntry"/> structure contains the data for a
/// single sprite defined in a spritesheet, including its bounds, patch
/// (center/9-slice data), and pivot point.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Bounds"/> - The rectangular region of the sprite within the spritesheet texture</description></item>
///   <item><description><see cref="Patch"/> - The center or 9-slice region of the sprite</description></item>
///   <item><description><see cref="Pivot"/> - The pivot point for positioning the sprite</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Access entries through the spritesheet
/// var spritesheet = AssetManager.Instance.Load&lt;Spritesheet&gt;("sprites/player.sheet");
/// 
/// // Get a sprite entry
/// Rect2 bounds = spritesheet.GetBound("walking_01");
/// Rect2 patch = spritesheet.GetPatch("walking_01");
/// Vect2 pivot = spritesheet.GetPivot("walking_01");
/// 
/// // Or use the entry directly through the spritesheet API
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe.
/// </para>
/// </remarks>
public readonly struct SpritesheetEntry
{
    /// <summary>
    /// Gets the rectangular region of the sprite within the spritesheet texture.
    /// </summary>
    public Rect2 Bounds { get; }

    /// <summary>
    /// Gets the center or 9-slice region of the sprite.
    /// </summary>
    public Rect2 Patch { get; }

    /// <summary>
    /// Gets the pivot point for positioning the sprite.
    /// </summary>
    public Vect2 Pivot { get; }

    internal SpritesheetEntry(Rect2 bounds, Rect2 patch, Vect2 pivot)
    {
        Bounds = bounds;
        Patch = patch;
        Pivot = pivot;
    }
}