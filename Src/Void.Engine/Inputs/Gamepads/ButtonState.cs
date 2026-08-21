// ============================================================================
//  ButtonState.cs
// ============================================================================
//  Defines the state of a button input for gamepads, mice, and other input devices.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Defines the possible states of a button input.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ButtonState"/> enumeration represents whether a button is
/// currently pressed or released. It is used across multiple input systems
/// including gamepads, keyboards, and mice.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var mouseState = Mouse.GetState();
/// if (mouseState[MouseButton.Left] == ButtonState.Pressed)
/// {
///     // Handle left mouse button press
/// }
/// 
/// var gamepadState = Gamepad.GetState();
/// if (gamepadState.IsButtonPressed(GamepadButton.A))
/// {
///     // Handle A button press
/// }
/// </code>
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// For frame-by-frame input handling, use the action system's
/// <see cref="InputActions.ActionState"/> which tracks Pressed, Held, Released, and Up states.
/// </para>
/// </remarks>
public enum ButtonState
{
    /// <summary>
    /// The button is not currently being pressed.
    /// </summary>
    Released,

    /// <summary>
    /// The button is currently being pressed.
    /// </summary>
    Pressed
}