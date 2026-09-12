// ============================================================================
//  Glyph.cs
// ============================================================================
//  Character metrics and atlas placement for font rendering.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Describes the atlas position and layout metrics for one font glyph.
/// </summary>
public struct Glyph
{
    /// <summary>
    /// The top-left position of the glyph inside the font atlas, in pixels.
    /// </summary>
    public Vect2 Position;

    /// <summary>
    /// The width and height of the glyph region, in pixels.
    /// </summary>
    public Vect2 Size;

    /// <summary>
    /// The positional offset applied when placing the glyph relative to the text cursor.
    /// </summary>
    public Vect2 Offset;

    /// <summary>
    /// The horizontal distance to advance the text cursor after the glyph.
    /// </summary>
    public float Advance;

    /// <summary>
    /// Gets whether the glyph has no positive drawable width or height.
    /// </summary>
    public readonly bool IsEmpty => Size.X <= 0 || Size.Y <= 0;

    /// <summary>
    /// Returns a string containing the glyph position, size, offset, and advance.
    /// </summary>
    /// <returns>A readable representation of the glyph metrics.</returns>
    public override readonly string ToString()
        => $"Glyph(Pos:{Position}, Size:{Size}, Offset:{Offset}, Advance:{Advance})";
}
