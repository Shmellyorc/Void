// ============================================================================
//  ActionBinding.cs
// ============================================================================
//  Represents a named input action with multiple bindings to keyboard,
//  mouse, and gamepad inputs.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a named input action and the inputs that can activate it.
/// </summary>
/// <remarks>
/// <para>
/// An action may contain any number of keyboard, mouse, and gamepad bindings.
/// During action evaluation, the action is considered active when any one of its
/// bindings is active.
/// </para>
/// <para>
/// Instances are created through <see cref="InputAction.AddAction(string)"/> or
/// <see cref="InputAction.AddAction(Enum)"/>. Binding methods return the same instance,
/// allowing an action to be configured with method chaining.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// InputAction.AddAction("Jump")
///     .AddKey(KeyboardKey.Space)
///     .AddKey(KeyboardKey.Up)
///     .AddGamepad(GamepadButton.A);
///
/// InputAction.AddAction("Pause")
///     .AddKey(KeyboardKey.Escape)
///     .AddMouse(MouseButton.Middle);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Binding changes are intended for the main game thread.
/// </para>
/// </remarks>
public sealed class ActionBinding
{
    private readonly List<InputBinding> _bindings = new();

    /// <summary>
    /// Gets the name used to identify this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the bindings currently assigned to this action.
    /// </summary>
    /// <remarks>
    /// The collection is read-only to callers. Use the add methods or <see cref="ClearBindings"/>
    /// to modify the action's bindings.
    /// </remarks>
    public IReadOnlyList<InputBinding> Bindings => _bindings;

    internal ActionBinding(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds an input binding to this action.
    /// </summary>
    /// <param name="binding">The binding to add.</param>
    /// <returns>This <see cref="ActionBinding"/> instance.</returns>
    public ActionBinding AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        return this;
    }

    /// <summary>
    /// Adds a keyboard key that can activate this action.
    /// </summary>
    /// <param name="key">The keyboard key to bind.</param>
    /// <returns>This <see cref="ActionBinding"/> instance.</returns>
    public ActionBinding AddKey(KeyboardKey key) => AddBinding(InputBinding.FromKey(key));

    /// <summary>
    /// Adds a mouse button that can activate this action.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>This <see cref="ActionBinding"/> instance.</returns>
    public ActionBinding AddMouse(MouseButton button) => AddBinding(InputBinding.FromMouse(button));

    /// <summary>
    /// Adds a gamepad button that can activate this action.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <returns>This <see cref="ActionBinding"/> instance.</returns>
    public ActionBinding AddGamepad(GamepadButton button) => AddBinding(InputBinding.FromGamepad(button));

    /// <summary>
    /// Removes every input binding from this action.
    /// </summary>
    /// <returns>This <see cref="ActionBinding"/> instance.</returns>
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
