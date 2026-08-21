// ============================================================================
//  Glyph.cs
// ============================================================================
//  Represents a single character glyph in a font texture atlas.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.Fonts;

/// <summary>
/// Represents a single character glyph in a font texture atlas.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Glyph"/> structure contains the positional and sizing data
/// for a single character in a font texture, including its location, dimensions,
/// offset from baseline, and advance distance.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Position"/> - The X/Y position of the glyph in the texture atlas</description></item>
///   <item><description><see cref="Size"/> - The width and height of the glyph in pixels</description></item>
///   <item><description><see cref="Offset"/> - The offset from the baseline (X/Y offset)</description></item>
///   <item><description><see cref="Advance"/> - The distance to move the cursor after rendering</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get glyph from a font
/// var font = AssetManager.Instance.Load&lt;SpriteFont&gt;("fonts/arial.png");
/// Glyph glyph = font.GetGlyph('A');
/// 
/// // Check if glyph is valid
/// if (!glyph.IsEmpty)
/// {
///     // Render the glyph at the current position
///     // Advance cursor by glyph.Advance
/// }
/// 
/// // Display glyph data
/// Console.WriteLine(glyph.ToString());
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is mutable but thread-safe when used in a read-only manner.
/// </para>
/// </remarks>
public struct Glyph
{
    /// <summary>
    /// The X/Y position of the glyph in the texture atlas.
    /// </summary>
    public Vect2 Position;

    /// <summary>
    /// The width and height of the glyph in pixels.
    /// </summary>
    public Vect2 Size;

    /// <summary>
    /// The offset from the baseline (X/Y offset).
    /// </summary>
    public Vect2 Offset;

    /// <summary>
    /// The distance to move the cursor after rendering.
    /// </summary>
    public float Advance;

    /// <summary>
    /// Gets a value indicating whether the glyph has valid dimensions.
    /// </summary>
    public readonly bool IsEmpty => Size.X <= 0 || Size.Y <= 0;

    /// <summary>
    /// Returns a string representation of the glyph.
    /// </summary>
    /// <returns>A string containing the glyph's position, size, offset, and advance.</returns>
    public override readonly string ToString()
        => $"Glyph(Pos:{Position}, Size:{Size}, Offset:{Offset}, Advance:{Advance})";
}