// ============================================================================
//  ActionBinding.cs
// ============================================================================
//  Represents a named input action with multiple bindings to keyboard,
//  mouse, and gamepad inputs.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a named input action with multiple bindings to keyboard,
/// mouse, and gamepad inputs.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ActionBinding"/> class defines a named action that can be
/// triggered by one or more input bindings. Each binding can be a keyboard key,
/// mouse button, or gamepad button. When any of the bound inputs are active,
/// the action is considered to be triggered.
/// </para>
/// <para>
/// Actions are created and managed through the <see cref="InputAction"/>
/// static class and are typically defined once during game initialization.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create an action with multiple bindings
/// var jumpAction = InputAction.AddAction("Jump")
///     .AddKey(KeyboardKey.Space)
///     .AddKey(KeyboardKey.Up)
///     .AddGamepad(GamepadButton.A);
/// 
/// // Create a movement action
/// var moveAction = InputAction.AddAction("MoveLeft")
///     .AddKey(KeyboardKey.Left)
///     .AddKey(KeyboardKey.A)
///     .AddGamepad(GamepadButton.DPadLeft)
///     .AddGamepad(GamepadButton.LeftStickLeft);
/// 
/// // Check the action state each frame
/// var state = InputAction.GetState();
/// if (state.IsPressed("Jump"))
/// {
///     // Handle jump
/// }
/// </code>
/// </para>
/// <para>
/// <b>Evaluation Order:</b>
/// Bindings are evaluated in the order they were added. The first binding
/// that is active will cause the action to return true.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed on
/// the main thread.
/// </para>
/// </remarks>
public sealed class ActionBinding
{
    private readonly List<InputBinding> _bindings = new();

    /// <summary>
    /// Gets the name of this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the current bindings for this action.
    /// </summary>
    public IReadOnlyList<InputBinding> Bindings => _bindings;

    internal ActionBinding(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds a binding to this action.
    /// </summary>
    /// <param name="binding">The input binding to add.</param>
    /// <returns>This action binding instance for method chaining.</returns>
    public ActionBinding AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        return this;
    }

    /// <summary>
    /// Adds a keyboard key binding to this action.
    /// </summary>
    /// <param name="key">The keyboard key to bind.</param>
    /// <returns>This action binding instance for method chaining.</returns>
    public ActionBinding AddKey(KeyboardKey key) => AddBinding(InputBinding.FromKey(key));

    /// <summary>
    /// Adds a mouse button binding to this action.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>This action binding instance for method chaining.</returns>
    public ActionBinding AddMouse(MouseButton button) => AddBinding(InputBinding.FromMouse(button));

    /// <summary>
    /// Adds a gamepad button binding to this action.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <returns>This action binding instance for method chaining.</returns>
    public ActionBinding AddGamepad(GamepadButton button) => AddBinding(InputBinding.FromGamepad(button));

    /// <summary>
    /// Removes all bindings from this action.
    /// </summary>
    /// <returns>This action binding instance for method chaining.</returns>
    public ActionBinding ClearBindings()
    {
        _bindings.Clear();
        return this;
    }

    internal bool Evaluate(MouseState mouse, KeyboardState keyboard, GamepadState gamepad)
    {
        foreach (var binding in _bindings)
        {
            if (binding.Key != KeyboardKey.None && keyboard.IsKeyDown(binding.Key))
                return true;

            if (binding.MouseButton != MouseButton.None && mouse.IsButtonPressed(binding.MouseButton))
                return true;

            if (binding.GamepadButton != GamepadButton.None && gamepad.IsButtonPressed(binding.GamepadButton))
                return true;
        }

        return false;
    }
}