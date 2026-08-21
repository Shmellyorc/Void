// ============================================================================
//  GamepadButton.cs
// ============================================================================
//  Defines the standard button layout for gamepad controllers, supporting
//  modern Xbox, PlayStation, and Nintendo-style controllers.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Defines the standard button layout for gamepad controllers, supporting
/// modern Xbox, PlayStation, and Nintendo-style controllers through the
/// SDL gamepad mapping system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GamepadButton"/> enumeration provides a standardized set of
/// button names that map to physical buttons on supported gamepads. The
/// mapping system translates hardware-specific button indices to these
/// standardized names.
/// </para>
/// <para>
/// Button Categories:
/// <list type="bullet">
///   <item><description>Face Buttons: <see cref="A"/>, <see cref="B"/>, <see cref="X"/>, <see cref="Y"/></description></item>
///   <item><description>Directional Pad: <see cref="DPadUp"/>, <see cref="DPadDown"/>, <see cref="DPadLeft"/>, <see cref="DPadRight"/></description></item>
///   <item><description>Shoulder Buttons: <see cref="LeftShoulder"/>, <see cref="RightShoulder"/></description></item>
///   <item><description>Trigger Buttons: <see cref="LeftTrigger"/>, <see cref="RightTrigger"/></description></item>
///   <item><description>Stick Presses: <see cref="LeftStick"/>, <see cref="RightStick"/></description></item>
///   <item><description>Stick Directions: <see cref="LeftStickUp"/>, <see cref="LeftStickDown"/>, <see cref="LeftStickLeft"/>, <see cref="LeftStickRight"/>, <see cref="RightStickUp"/>, <see cref="RightStickDown"/>, <see cref="RightStickLeft"/>, <see cref="RightStickRight"/></description></item>
///   <item><description>System Buttons: <see cref="Start"/>, <see cref="Back"/>, <see cref="Guide"/></description></item>
///   <item><description>Advanced: <see cref="Paddle1"/>, <see cref="Paddle2"/>, <see cref="Paddle3"/>, <see cref="Paddle4"/>, <see cref="Touchpad"/>, <see cref="Misc1"/></description></item>
/// </list>
/// </para>
/// <para>
/// Usage Example:
/// <code>
/// var state = Gamepad.GetState();
/// 
/// if (state.IsButtonPressed(GamepadButton.A))
/// {
///     // Handle jump action
/// }
/// 
/// if (state.IsButtonPressed(GamepadButton.Start))
/// {
///     // Open pause menu
/// }
/// 
/// // Get force/analog value for triggers and sticks
/// float triggerForce = state.GetForce(GamepadButton.LeftTrigger);
/// Vect2 stick = state.GetStick(GamepadButton.LeftStick);
/// </code>
/// </para>
/// <para>
/// Controller Layout Mapping:
/// <list type="bullet">
///   <item><description><see cref="A"/> = Xbox: A, PlayStation: Cross (×), Nintendo: B (A on Switch)</description></item>
///   <item><description><see cref="B"/> = Xbox: B, PlayStation: Circle (○), Nintendo: A (B on Switch)</description></item>
///   <item><description><see cref="X"/> = Xbox: X, PlayStation: Square (□), Nintendo: Y (X on Switch)</description></item>
///   <item><description><see cref="Y"/> = Xbox: Y, PlayStation: Triangle (△), Nintendo: X (Y on Switch)</description></item>
/// </list>
/// </para>
/// <para>
/// Thread Safety:
/// This enumeration is thread-safe by nature and can be used from any thread.
/// </para>
/// </remarks>
public enum GamepadButton
{
    /// <summary>
    /// Represents no button. Used for unbound or invalid button references.
    /// </summary>
    None = -1,

    /// <summary>
    /// The primary action button (A on Xbox, Cross on PlayStation, B on Nintendo Switch).
    /// </summary>
    A,

    /// <summary>
    /// The secondary action button (B on Xbox, Circle on PlayStation, A on Nintendo Switch).
    /// </summary>
    B,

    /// <summary>
    /// The tertiary action button (X on Xbox, Square on PlayStation, Y on Nintendo Switch).
    /// </summary>
    X,

    /// <summary>
    /// The quaternary action button (Y on Xbox, Triangle on PlayStation, X on Nintendo Switch).
    /// </summary>
    Y,

    /// <summary>
    /// The directional pad up button.
    /// </summary>
    DPadUp,

    /// <summary>
    /// The directional pad down button.
    /// </summary>
    DPadDown,

    /// <summary>
    /// The directional pad left button.
    /// </summary>
    DPadLeft,

    /// <summary>
    /// The directional pad right button.
    /// </summary>
    DPadRight,

    /// <summary>
    /// The left shoulder/bumper button (LB on Xbox, L1 on PlayStation).
    /// </summary>
    LeftShoulder,

    /// <summary>
    /// The right shoulder/bumper button (RB on Xbox, R1 on PlayStation).
    /// </summary>
    RightShoulder,

    /// <summary>
    /// The left trigger analog button (LT on Xbox, L2 on PlayStation).
    /// </summary>
    LeftTrigger,

    /// <summary>
    /// The right trigger analog button (RT on Xbox, R2 on PlayStation).
    /// </summary>
    RightTrigger,

    /// <summary>
    /// The left thumbstick press (L3 on Xbox, L3 on PlayStation).
    /// </summary>
    LeftStick,

    /// <summary>
    /// The right thumbstick press (R3 on Xbox, R3 on PlayStation).
    /// </summary>
    RightStick,

    /// <summary>
    /// The left thumbstick is pushed upward.
    /// </summary>
    LeftStickUp,

    /// <summary>
    /// The left thumbstick is pushed downward.
    /// </summary>
    LeftStickDown,

    /// <summary>
    /// The left thumbstick is pushed leftward.
    /// </summary>
    LeftStickLeft,

    /// <summary>
    /// The left thumbstick is pushed rightward.
    /// </summary>
    LeftStickRight,

    /// <summary>
    /// The right thumbstick is pushed upward.
    /// </summary>
    RightStickUp,

    /// <summary>
    /// The right thumbstick is pushed downward.
    /// </summary>
    RightStickDown,

    /// <summary>
    /// The right thumbstick is pushed leftward.
    /// </summary>
    RightStickLeft,

    /// <summary>
    /// The right thumbstick is pushed rightward.
    /// </summary>
    RightStickRight,

    /// <summary>
    /// The start/menu button (Start on Xbox, Options on PlayStation, Plus on Nintendo Switch).
    /// </summary>
    Start,

    /// <summary>
    /// The back/view button (Back on Xbox, Share on PlayStation, Minus on Nintendo Switch).
    /// </summary>
    Back,

    /// <summary>
    /// The guide/home button (Xbox button on Xbox, PlayStation button on PlayStation, Home on Nintendo Switch).
    /// </summary>
    Guide,

    /// <summary>
    /// The first paddle button (found on advanced/pro controllers).
    /// </summary>
    Paddle1,

    /// <summary>
    /// The second paddle button (found on advanced/pro controllers).
    /// </summary>
    Paddle2,

    /// <summary>
    /// The third paddle button (found on advanced/pro controllers).
    /// </summary>
    Paddle3,

    /// <summary>
    /// The fourth paddle button (found on advanced/pro controllers).
    /// </summary>
    Paddle4,

    /// <summary>
    /// The touchpad button (found on PlayStation controllers).
    /// </summary>
    Touchpad,

    /// <summary>
    /// A miscellaneous button for controller-specific functions.
    /// </summary>
    Misc1
}