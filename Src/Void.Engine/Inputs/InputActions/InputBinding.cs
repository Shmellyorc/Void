// ============================================================================
//  InputBinding.cs
// ============================================================================
//  Defines the trigger state of an input action and a single input binding.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Defines the transition state returned for an input action.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="InputActionState.GetState(string)"/> compares the current and previous
/// value of an action and returns exactly one <see cref="ActionState"/> for that update.
/// </para>
/// <para>
/// This enum is intentionally different from the boolean state helpers. For example,
/// an action that was first pressed this update returns <see cref="JustPressed"/> from
/// <see cref="InputActionState.GetState(string)"/>, while
/// <see cref="InputActionState.IsPressed(string)"/> is also <see langword="true"/> because
/// the action is currently pressed.
/// </para>
/// <para>
/// <b>State Flow:</b>
/// <list type="bullet">
///   <item><description><see cref="JustPressed"/> - Released previously and pressed now.</description></item>
///   <item><description><see cref="Pressed"/> - Pressed previously and still pressed now.</description></item>
///   <item><description><see cref="JustReleased"/> - Pressed previously and released now.</description></item>
///   <item><description><see cref="Up"/> - Released previously and still released now.</description></item>
/// </list>
/// </para>
/// </remarks>
public enum ActionState
{
    /// <summary>
    /// The action changed from released to pressed during the current update.
    /// </summary>
    JustPressed,

    /// <summary>
    /// The action was already pressed and remains pressed during the current update.
    /// </summary>
    Pressed,

    /// <summary>
    /// The action changed from pressed to released during the current update.
    /// </summary>
    JustReleased,

    /// <summary>
    /// The action was already released and remains released during the current update.
    /// </summary>
    Up
}

/// <summary>
/// Represents one keyboard, mouse, or gamepad binding for an input action.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="InputBinding"/> stores one device input. Unused device fields are set
/// to their corresponding <c>None</c> value. An <see cref="ActionBinding"/> may contain
/// multiple input bindings, and any active binding can activate the action.
/// </para>
/// <para>
/// Bindings are normally created with <see cref="FromKey(KeyboardKey)"/>,
/// <see cref="FromMouse(MouseButton)"/>, or <see cref="FromGamepad(GamepadButton)"/>.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var keyboard = InputBinding.FromKey(KeyboardKey.Space);
/// var gamepad = InputBinding.FromGamepad(GamepadButton.A);
///
/// InputAction.AddAction("Jump")
///     .AddBinding(keyboard)
///     .AddBinding(gamepad);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable after construction.
/// </para>
/// </remarks>
public readonly struct InputBinding
{
    /// <summary>
    /// Gets the keyboard key assigned to this binding.
    /// </summary>
    /// <value>
    /// The bound <see cref="KeyboardKey"/>, or <see cref="KeyboardKey.None"/> when this is not a keyboard binding.
    /// </value>
    public KeyboardKey Key { get; }

    /// <summary>
    /// Gets the mouse button assigned to this binding.
    /// </summary>
    /// <value>
    /// The bound <see cref="MouseButton"/>, or <see cref="MouseButton.None"/> when this is not a mouse binding.
    /// </value>
    public MouseButton MouseButton { get; }

    /// <summary>
    /// Gets the gamepad button assigned to this binding.
    /// </summary>
    /// <value>
    /// The bound <see cref="GamepadButton"/>, or <see cref="GamepadButton.None"/> when this is not a gamepad binding.
    /// </value>
    public GamepadButton GamepadButton { get; }

    internal InputBinding(KeyboardKey key = KeyboardKey.None, MouseButton mouseButton = MouseButton.None, GamepadButton gamepadButton = GamepadButton.None)
    {
        Key = key;
        MouseButton = mouseButton;
        GamepadButton = gamepadButton;
    }

    /// <summary>
    /// Creates an input binding for a keyboard key.
    /// </summary>
    /// <param name="key">The keyboard key to bind.</param>
    /// <returns>A binding that uses the specified keyboard key.</returns>
    public static InputBinding FromKey(KeyboardKey key) => new(key: key);

    /// <summary>
    /// Creates an input binding for a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>A binding that uses the specified mouse button.</returns>
    public static InputBinding FromMouse(MouseButton button) => new(mouseButton: button);

    /// <summary>
    /// Creates an input binding for a gamepad button.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <returns>A binding that uses the specified gamepad button.</returns>
    public static InputBinding FromGamepad(GamepadButton button) => new(gamepadButton: button);
}
