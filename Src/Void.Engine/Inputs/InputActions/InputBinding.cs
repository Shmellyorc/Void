using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Defines the trigger state of an input action.
/// </summary>
public enum ActionState
{
    /// <summary>
    /// Action was just pressed this frame.
    /// </summary>
    Pressed,

    /// <summary>
    /// Action is currently held down.
    /// </summary>
    Held,

    /// <summary>
    /// Action was just released this frame.
    /// </summary>
    Released,

    /// <summary>
    /// Action is not active.
    /// </summary>
    Up
}

/// <summary>
/// Defines a single input binding for an action.
/// </summary>
public readonly struct InputBinding
{
    /// <summary>
    /// Gets the keyboard key for this binding (KeyboardKey.None if unused).
    /// </summary>
    public KeyboardKey Key { get; }

    /// <summary>
    /// Gets the mouse button for this binding (MouseButton.None if unused).
    /// </summary>
    public MouseButton MouseButton { get; }

    /// <summary>
    /// Gets the gamepad button for this binding (GamepadButton.None if unused).
    /// </summary>
    public GamepadButton GamepadButton { get; }

    internal InputBinding(KeyboardKey key = KeyboardKey.None, MouseButton mouseButton = MouseButton.None, GamepadButton gamepadButton = GamepadButton.None)
    {
        Key = key;
        MouseButton = mouseButton;
        GamepadButton = gamepadButton;
    }

    public static InputBinding FromKey(KeyboardKey key) => new(key: key);
    public static InputBinding FromMouse(MouseButton button) => new(mouseButton: button);
    public static InputBinding FromGamepad(GamepadButton button) => new(gamepadButton: button);
}
