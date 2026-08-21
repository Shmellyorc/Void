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
/// Defines the trigger state of an input action.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ActionState"/> enumeration represents the state of an action
/// based on its current and previous frame states. It is used by
/// <see cref="InputActionState"/> to provide detailed state information.
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// <list type="bullet">
///   <item><description><see cref="Pressed"/> - The action was not active last frame but is active this frame</description></item>
///   <item><description><see cref="Held"/> - The action was active last frame and is still active this frame</description></item>
///   <item><description><see cref="Released"/> - The action was active last frame but is not active this frame</description></item>
///   <item><description><see cref="Up"/> - The action was not active last frame and is not active this frame</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = InputAction.GetState();
/// var jumpState = state.GetState("Jump");
/// 
/// switch (jumpState)
/// {
///     case ActionState.Pressed:
///         StartJump();
///         break;
///     case ActionState.Held:
///         ContinueJump();
///         break;
///     case ActionState.Released:
///         EndJump();
///         break;
/// }
/// </code>
/// </para>
/// </remarks>
public enum ActionState
{
    /// <summary>
    /// The action was just pressed this frame (transition from Up/Released to active).
    /// </summary>
    Pressed,

    /// <summary>
    /// The action is currently held down (active for multiple consecutive frames).
    /// </summary>
    Held,

    /// <summary>
    /// The action was just released this frame (transition from active to Up/Released).
    /// </summary>
    Released,

    /// <summary>
    /// The action is not active.
    /// </summary>
    Up
}

/// <summary>
/// Defines a single input binding for an action, linking it to a keyboard key,
/// mouse button, or gamepad button.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InputBinding"/> structure represents a single binding between
/// an action and a specific input device button or key. An action can have
/// multiple bindings, and the action is considered active if any of its
/// bindings are active.
/// </para>
/// <para>
/// Bindings are created using the static factory methods:
/// <list type="bullet">
///   <item><description><see cref="FromKey(KeyboardKey)"/> - Creates a keyboard binding</description></item>
///   <item><description><see cref="FromMouse(MouseButton)"/> - Creates a mouse binding</description></item>
///   <item><description><see cref="FromGamepad(GamepadButton)"/> - Creates a gamepad binding</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create bindings individually
/// var binding1 = InputBinding.FromKey(KeyboardKey.Space);
/// var binding2 = InputBinding.FromGamepad(GamepadButton.A);
/// 
/// // Add bindings to an action
/// InputAction.AddAction("Jump")
///     .AddBinding(binding1)
///     .AddBinding(binding2);
/// 
/// // Or use the convenience methods
/// InputAction.AddAction("Jump")
///     .AddKey(KeyboardKey.Space)
///     .AddGamepad(GamepadButton.A);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe.
/// </para>
/// </remarks>
public readonly struct InputBinding
{
    /// <summary>
    /// Gets the keyboard key for this binding, or <see cref="KeyboardKey.None"/> if unused.
    /// </summary>
    public KeyboardKey Key { get; }

    /// <summary>
    /// Gets the mouse button for this binding, or <see cref="MouseButton.None"/> if unused.
    /// </summary>
    public MouseButton MouseButton { get; }

    /// <summary>
    /// Gets the gamepad button for this binding, or <see cref="GamepadButton.None"/> if unused.
    /// </summary>
    public GamepadButton GamepadButton { get; }

    internal InputBinding(KeyboardKey key = KeyboardKey.None, MouseButton mouseButton = MouseButton.None, GamepadButton gamepadButton = GamepadButton.None)
    {
        Key = key;
        MouseButton = mouseButton;
        GamepadButton = gamepadButton;
    }

    /// <summary>
    /// Creates a new input binding for a keyboard key.
    /// </summary>
    /// <param name="key">The keyboard key to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified key.</returns>
    public static InputBinding FromKey(KeyboardKey key) => new(key: key);

    /// <summary>
    /// Creates a new input binding for a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified mouse button.</returns>
    public static InputBinding FromMouse(MouseButton button) => new(mouseButton: button);

    /// <summary>
    /// Creates a new input binding for a gamepad button.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified gamepad button.</returns>
    public static InputBinding FromGamepad(GamepadButton button) => new(gamepadButton: button);
}