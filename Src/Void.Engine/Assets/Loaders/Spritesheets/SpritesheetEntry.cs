// ============================================================================
//  SpritesheetEntry.cs
// ============================================================================
//  Stores the parsed bounds, patch region, and pivot for one spritesheet slice.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.Spritesheets;

/// <summary>
/// Describes the parsed geometry for a single spritesheet slice.
/// </summary>
public readonly struct SpritesheetEntry
{
    /// <summary>
    /// Gets the source rectangle of the sprite within its texture.
    /// </summary>
    public Rect2 Bounds { get; }

    /// <summary>
    /// Gets the center region used for nine-patch rendering.
    /// </summary>
    public Rect2 Patch { get; }

    /// <summary>
    /// Gets the sprite pivot stored by the spritesheet definition.
    /// </summary>
    public Vect2 Pivot { get; }

    internal SpritesheetEntry(Rect2 bounds, Rect2 patch, Vect2 pivot)
    {
        Bounds = bounds;
        Patch = patch;
        Pivot = pivot;
    }
}
