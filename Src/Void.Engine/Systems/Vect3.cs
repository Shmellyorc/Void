// ============================================================================
//  Vect3.cs
// ============================================================================
//  Immutable three-component floating-point vector.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Represents a three-component floating-point vector.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Vect3"/> stores immutable X, Y, and Z components and can be used
/// anywhere a simple three-value vector representation is needed.
/// </para>
/// <para>
/// Example:
/// <code>
/// var value = new Vect3(10f, 20f, 5f);
/// float z = value.Z;
/// </code>
/// </para>
/// </remarks>
public readonly struct Vect3
{
    /// <summary>
    /// Gets the X component.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the Z component.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Creates a vector from X, Y, and Z components.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vect3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
