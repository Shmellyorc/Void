// ============================================================================
//  Vect4.cs
// ============================================================================
//  4D vector structure for homogeneous coordinates, quaternions, and other
//  four-component spatial calculations.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents a 4D vector with floating-point components for homogeneous
/// coordinates, quaternions, and other four-component spatial calculations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Vect4"/> structure provides a foundation for 4D spatial
/// calculations including addition, subtraction, multiplication, division,
/// and other common vector operations.
/// </para>
/// <para>
/// This structure is commonly used for:
/// <list type="bullet">
///   <item><description>Homogeneous coordinates for 3D transformations</description></item>
///   <item><description>Quaternion storage (X, Y, Z, W)</description></item>
///   <item><description>Color with alpha channel (R, G, B, A)</description></item>
///   <item><description>Direction vectors with an additional component</description></item>
/// </list>
/// </para>
/// <para>
/// This structure is immutable and thread-safe, with all components being
/// read-only properties.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// // Create a homogeneous coordinate
/// var point3D = new Vect4(10f, 20f, 5f, 1f);
/// 
/// // Create a quaternion
/// var quaternion = new Vect4(0f, 0f, 0f, 1f);
/// 
/// // Create a color with alpha
/// var color = new Vect4(1f, 0.5f, 0f, 1f);
/// </code>
/// </para>
/// </remarks>
public readonly struct Vect4
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
    /// Gets the W-component of the vector.
    /// </summary>
    public float W { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vect4"/> structure with the specified X, Y, Z, and W components.
    /// </summary>
    /// <param name="x">The X-component of the vector.</param>
    /// <param name="y">The Y-component of the vector.</param>
    /// <param name="z">The Z-component of the vector.</param>
    /// <param name="w">The W-component of the vector.</param>
    public Vect4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}