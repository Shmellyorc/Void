// ============================================================================
//  ILDtkInstance.cs
// ============================================================================
//  Common contract for data instances stored in LDtk map layers.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Assets.Loaders.LDtk.Instances;

/// <summary>
/// Defines the common position data exposed by instances stored in an LDtk layer.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MapLayer.Instances"/> exposes values through this interface so entity,
/// tile, and integer-grid instances can be handled through a shared API.
/// </para>
/// <para>
/// Use <see cref="MapLayer.InstanceAs{T}"/> when a layer's concrete instance type is known.
/// </para>
/// </remarks>
public interface ILDtkInstance
{
    /// <summary>
    /// Gets the instance location in layer grid coordinates.
    /// </summary>
    Vect2 Location { get; }

    /// <summary>
    /// Gets the instance position in pixels.
    /// </summary>
    Vect2 Position { get; }
}
