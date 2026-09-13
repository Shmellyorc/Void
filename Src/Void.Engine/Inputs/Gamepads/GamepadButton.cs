// ============================================================================
//  GamepadButton.cs
// ============================================================================
//  Defines VOID's standardized mapped gamepad buttons and stick directions.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Identifies standardized gamepad buttons, triggers, and virtual stick directions.
/// </summary>
/// <remarks>
/// <para>
/// Physical controller layouts are normalized through SDL's gamepad mapping system.
/// The face-button names follow the conventional Xbox-style labels used by VOID;
/// the physical labels shown on PlayStation and Nintendo controllers differ.
/// </para>
/// <para>
/// Trigger and stick-direction values can be queried as analog values with
/// <see cref="GamepadState.GetForce(GamepadButton)"/>. <see cref="LeftStick"/>
/// and <see cref="RightStick"/> can be passed to <see cref="GamepadState.GetStick(GamepadButton)"/>.
/// </para>
/// </remarks>
public enum GamepadButton
{
    /// <summary>
    /// Represents no gamepad button.
    /// </summary>
    None = -1,

    /// <summary>
    /// The mapped south face button.
    /// </summary>
    A,

    /// <summary>
    /// The mapped east face button.
    /// </summary>
    B,

    /// <summary>
    /// The mapped west face button.
    /// </summary>
    X,

    /// <summary>
    /// The mapped north face button.
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
    /// The left shoulder or bumper button.
    /// </summary>
    LeftShoulder,

    /// <summary>
    /// The right shoulder or bumper button.
    /// </summary>
    RightShoulder,

    /// <summary>
    /// The left analog trigger.
    /// </summary>
    LeftTrigger,

    /// <summary>
    /// The right analog trigger.
    /// </summary>
    RightTrigger,

    /// <summary>
    /// The left thumbstick press.
    /// </summary>
    LeftStick,

    /// <summary>
    /// The right thumbstick press.
    /// </summary>
    RightStick,

    /// <summary>
    /// A virtual button indicating upward left-stick deflection.
    /// </summary>
    LeftStickUp,

    /// <summary>
    /// A virtual button indicating downward left-stick deflection.
    /// </summary>
    LeftStickDown,

    /// <summary>
    /// A virtual button indicating leftward left-stick deflection.
    /// </summary>
    LeftStickLeft,

    /// <summary>
    /// A virtual button indicating rightward left-stick deflection.
    /// </summary>
    LeftStickRight,

    /// <summary>
    /// A virtual button indicating upward right-stick deflection.
    /// </summary>
    RightStickUp,

    /// <summary>
    /// A virtual button indicating downward right-stick deflection.
    /// </summary>
    RightStickDown,

    /// <summary>
    /// A virtual button indicating leftward right-stick deflection.
    /// </summary>
    RightStickLeft,

    /// <summary>
    /// A virtual button indicating rightward right-stick deflection.
    /// </summary>
    RightStickRight,

    /// <summary>
    /// The mapped start, menu, options, or plus button.
    /// </summary>
    Start,

    /// <summary>
    /// The mapped back, view, share, or minus button.
    /// </summary>
    Back,

    /// <summary>
    /// The mapped guide or home button.
    /// </summary>
    Guide,

    /// <summary>
    /// The first mapped rear paddle button.
    /// </summary>
    Paddle1,

    /// <summary>
    /// The second mapped rear paddle button.
    /// </summary>
    Paddle2,

    /// <summary>
    /// The third mapped rear paddle button.
    /// </summary>
    Paddle3,

    /// <summary>
    /// The fourth mapped rear paddle button.
    /// </summary>
    Paddle4,

    /// <summary>
    /// The mapped touchpad button when supported by the controller.
    /// </summary>
    Touchpad,

    /// <summary>
    /// The mapped controller-specific miscellaneous button.
    /// </summary>
    Misc1
}
