// ============================================================================
//  GamepadState.cs
// ============================================================================
//  Represents a snapshot of a gamepad's state at a specific moment in time.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Inputs.Gamepads;

/// <summary>
/// Represents a snapshot of a gamepad's state at a specific moment in time.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GamepadState"/> structure provides a read-only snapshot of
/// a gamepad's button states, trigger values, thumbstick positions, and
/// connection status at the moment the state was captured.
/// </para>
/// <para>
/// This structure is returned by <see cref="Gamepad.GetState(PlayerIndex)"/> and should
/// be used for all gamepad input queries within a frame. It is immutable
/// and thread-safe.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the current gamepad state
/// var state = Gamepad.GetState();
/// 
/// if (state.IsConnected)
/// {
///     // Check button presses
///     if (state.IsButtonPressed(GamepadButton.A))
///         Jump();
///     
///     // Get analog values
///     float speed = state.LeftTrigger;
///     Vect2 movement = state.LeftStick;
///     
///     // Get force values for specific directions
///     float horizontal = state.GetForce(GamepadButton.LeftStickRight) - 
///                        state.GetForce(GamepadButton.LeftStickLeft);
/// }
/// </code>
/// </para>
/// <para>
/// <b>Button State vs Force:</b>
/// <list type="bullet">
///   <item><description><see cref="IsButtonPressed"/> - Returns true/false for digital buttons</description></item>
///   <item><description><see cref="GetForce"/> - Returns analog force (0-1) for triggers and stick directions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe. All fields are read-only.
/// </para>
/// </remarks>
public struct GamepadState
{
    private readonly ulong _buttons;

    /// <summary>
    /// Gets the value of the left trigger (0 to 1).
    /// </summary>
    public float LeftTrigger { get; }

    /// <summary>
    /// Gets the value of the right trigger (0 to 1).
    /// </summary>
    public float RightTrigger { get; }

    /// <summary>
    /// Gets the position of the left thumbstick.
    /// </summary>
    public Vect2 LeftStick { get; }

    /// <summary>
    /// Gets the position of the right thumbstick.
    /// </summary>
    public Vect2 RightStick { get; }

    /// <summary>
    /// Gets a value indicating whether the gamepad is connected.
    /// </summary>
    public bool IsConnected { get; }

    internal GamepadState(ulong buttons, float leftTrigger, float rightTrigger, Vect2 leftStick, Vect2 rightStick, bool connected)
    {
        _buttons = buttons;
        LeftTrigger = leftTrigger;
        RightTrigger = rightTrigger;
        LeftStick = leftStick;
        RightStick = rightStick;
        IsConnected = connected;
    }

    /// <summary>
    /// Determines whether the specified button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
    public bool IsButtonPressed(GamepadButton button)
    {
        if (button == GamepadButton.None)
            return false;
        return (_buttons & (1UL << (int)button)) != 0;
    }

    /// <summary>
    /// Determines whether the specified button is currently released.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><see langword="true"/> if the button is released; otherwise, <see langword="false"/>.</returns>
    public bool IsButtonReleased(GamepadButton button)
    {
        if (button == GamepadButton.None)
            return true;
        return (_buttons & (1UL << (int)button)) == 0;
    }

    /// <summary>
    /// Gets the analog force value for the specified button or direction.
    /// </summary>
    /// <param name="button">The button or direction to get the force for.</param>
    /// <returns>A value between 0 and 1 representing the force applied.</returns>
    /// <remarks>
    /// <para>
    /// This method provides analog values for triggers and thumbstick directions:
    /// <list type="bullet">
    ///   <item><description><see cref="GamepadButton.LeftTrigger"/> / <see cref="GamepadButton.RightTrigger"/> - Returns 0 to 1</description></item>
    ///   <item><description>Stick directions - Returns 0 to 1 based on stick deflection</description></item>
    ///   <item><description>Digital buttons - Returns 1 if pressed, 0 if released</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public float GetForce(GamepadButton button)
    {
        if (button == GamepadButton.None)
            return 0f;

        return button switch
        {
            GamepadButton.LeftTrigger => LeftTrigger,
            GamepadButton.RightTrigger => RightTrigger,
            GamepadButton.LeftStickUp => MathF.Max(0f, -LeftStick.Y),
            GamepadButton.LeftStickDown => MathF.Max(0f, LeftStick.Y),
            GamepadButton.LeftStickLeft => MathF.Max(0f, -LeftStick.X),
            GamepadButton.LeftStickRight => MathF.Max(0f, LeftStick.X),
            GamepadButton.RightStickUp => MathF.Max(0f, -RightStick.Y),
            GamepadButton.RightStickDown => MathF.Max(0f, RightStick.Y),
            GamepadButton.RightStickLeft => MathF.Max(0f, -RightStick.X),
            GamepadButton.RightStickRight => MathF.Max(0f, RightStick.X),
            _ => IsButtonPressed(button) ? 1f : 0f
        };
    }

    /// <summary>
    /// Gets the thumbstick position for the specified stick.
    /// </summary>
    /// <param name="button">The stick button (<see cref="GamepadButton.LeftStick"/> or <see cref="GamepadButton.RightStick"/>).</param>
    /// <returns>The stick position as a <see cref="Vect2"/>, or <see cref="Vect2.Zero"/> if the button is not a stick.</returns>
    public Vect2 GetStick(GamepadButton button)
    {
        if (button == GamepadButton.None)
            return Vect2.Zero;

        return button switch
        {
            GamepadButton.LeftStick => LeftStick,
            GamepadButton.RightStick => RightStick,
            _ => Vect2.Zero
        };
    }
}