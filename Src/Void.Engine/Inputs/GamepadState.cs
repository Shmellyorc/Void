public struct GamepadState
{
    private readonly ulong _buttons;

    public float LeftTrigger { get; }
    public float RightTrigger { get; }
    public Vect2 LeftStick { get; }
    public Vect2 RightStick { get; }
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

    public bool IsButtonPressed(GamepadButton button) => (_buttons & (1UL << (int)button)) != 0;
    public bool IsButtonReleased(GamepadButton button) => (_buttons & (1UL << (int)button)) == 0;

    public float GetForce(GamepadButton button)
    {
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

    public Vect2 GetStick(GamepadButton button)
    {
        return button switch
        {
            GamepadButton.LeftStick => LeftStick,
            GamepadButton.RightStick => RightStick,
            _ => Vect2.Zero
        };
    }
}