// ============================================================================
//  ButtonState.cs
// ============================================================================
//  Defines the current pressed or released state of a button input.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Defines whether a digital button input is currently pressed or released.
/// </summary>
/// <remarks>
/// This value is also reused by other button-based input snapshots, such as mouse state.
/// </remarks>
public enum ButtonState
{
    /// <summary>
    /// The button is not currently pressed.
    /// </summary>
    Released,

    /// <summary>
    /// The button is currently pressed.
    /// </summary>
    Pressed
}
