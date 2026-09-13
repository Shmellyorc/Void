// ============================================================================
//  PlayerIndex.cs
// ============================================================================
//  Identifies one of VOID's four supported gamepad slots.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Identifies one of the four gamepad slots supported by <see cref="Gamepad"/>.
/// </summary>
public enum PlayerIndex
{
    /// <summary>
    /// The first gamepad slot, index 0.
    /// </summary>
    One = 0,

    /// <summary>
    /// The second gamepad slot, index 1.
    /// </summary>
    Two = 1,

    /// <summary>
    /// The third gamepad slot, index 2.
    /// </summary>
    Three = 2,

    /// <summary>
    /// The fourth gamepad slot, index 3.
    /// </summary>
    Four = 3
}
