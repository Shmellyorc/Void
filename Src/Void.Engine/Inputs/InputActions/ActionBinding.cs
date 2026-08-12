using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a named input action with multiple bindings.
/// </summary>
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
    public ActionBinding AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        return this;
    }

    /// <summary>
    /// Adds a keyboard key binding.
    /// </summary>
    public ActionBinding AddKey(KeyboardKey key) => AddBinding(InputBinding.FromKey(key));

    /// <summary>
    /// Adds a mouse button binding.
    /// </summary>
    public ActionBinding AddMouse(MouseButton button) => AddBinding(InputBinding.FromMouse(button));

    /// <summary>
    /// Adds a gamepad button binding.
    /// </summary>
    public ActionBinding AddGamepad(GamepadButton button) => AddBinding(InputBinding.FromGamepad(button));

    /// <summary>
    /// Removes all bindings.
    /// </summary>
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
