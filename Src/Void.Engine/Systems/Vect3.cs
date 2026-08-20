// ============================================================================
//  Vect3.cs
// ============================================================================
//  3D vector structure for spatial calculations including positions,
//  directions, and transformations in three-dimensional space.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 3D vector with floating-point components for position,
/// direction, and velocity calculations in three-dimensional space.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Vect3"/> structure provides a foundation for 3D spatial
/// calculations including addition, subtraction, multiplication, division,
/// distance calculations, normalization, and interpolation.
/// </para>
/// <para>
/// This structure is immutable and thread-safe, with all components being
/// read-only properties.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Create vectors
/// var position = new Vect3(10f, 20f, 5f);
/// var direction = new Vect3(1f, 0f, 0f);
/// 
/// // Calculate distance
/// float distance = position.Distance(new Vect3(100f, 50f, 10f));
/// 
/// // Normalize a direction
/// var normalized = new Vect3(3f, 4f, 0f).Normalized();
/// 
/// // Interpolate between positions
/// var midPoint = position.Lerp(target, 0.5f);
/// </code>
/// </para>
/// </remarks>
public readonly struct Vect3
{
    /// <summary>
    /// Gets the X-component of the vector.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y-component of the vector.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the Z-component of the vector.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vect3"/> structure with the specified X, Y, and Z components.
    /// </summary>
    /// <param name="x">The X-component of the vector.</param>
    /// <param name="y">The Y-component of the vector.</param>
    /// <param name="z">The Z-component of the vector.</param>
    public Vect3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}