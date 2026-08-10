

namespace Void.Engine.Inputs;

public static class Gamepad
{
    private const int MaxGamepads = 4;
    private static readonly GamepadState[] _states = new GamepadState[MaxGamepads];
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        GamepadDatabase.Load();
        _initialized = true;
    }

    public static GamepadState GetState(PlayerIndex player) => GetState((int)player);
    public static void Update(PlayerIndex player) => Update((int)player);

    public static GamepadState GetState(int index = 0)
    {
        Initialize();
        if (index < 0 || index >= MaxGamepads) return default;
        UpdateState(index);
        return _states[index];
    }

    public static void Update(int index)
    {
        Initialize();
        if (index < 0 || index >= MaxGamepads) return;
        UpdateState(index);
    }

    public static void UpdateAll()
    {
        Initialize();
        for (int i = 0; i < MaxGamepads; i++)
            UpdateState(i);
    }

    private static void UpdateState(int index)
    {
        if (GameSettings.Instance.IgnoreInputWhenUnfocused &&
        (!Game.Instance._window.IsOpen || !Game.Instance._window.HasFocus()))
        {
            _states[index] = new GamepadState(0, 0f, 0f, Vect2.Zero, Vect2.Zero, false);
            return;
        }

        if (!SFJoystick.IsConnected((uint)index))
        {
            _states[index] = new GamepadState(0, 0f, 0f, Vect2.Zero, Vect2.Zero, false);
            return;
        }

        var id = SFJoystick.GetIdentification((uint)index);

        // SDL-style GUID: 16 hex digits from VendorId + ProductId
        string guid = $"{id.VendorId:x4}0000{id.ProductId:x4}000000000000";

        var mapping = GamepadDatabase.GetMapping(guid);

        ulong buttons = 0;
        float leftTrigger = 0f, rightTrigger = 0f;
        Vect2 leftStick = Vect2.Zero, rightStick = Vect2.Zero;
        float deadZone = GameSettings.Instance.DeadZone;
        uint buttonCount = SFJoystick.GetButtonCount((uint)index);

        if (mapping != null)
        {
            ProcessMappedInput(ref buttons, ref leftTrigger, ref rightTrigger, ref leftStick, ref rightStick,
                mapping, index, buttonCount, deadZone);
        }
        else
        {
            ProcessFallbackInput(ref buttons, ref leftTrigger, ref rightTrigger, ref leftStick, ref rightStick,
                index, buttonCount, deadZone);
        }

        _states[index] = new GamepadState(buttons, leftTrigger, rightTrigger, leftStick, rightStick, true);
    }

    private static void ProcessMappedInput(ref ulong buttons, ref float leftTrigger, ref float rightTrigger,
        ref Vect2 leftStick, ref Vect2 rightStick, GamepadMapping mapping, int index, uint buttonCount, float deadZone)
    {
        SetButton(ref buttons, GamepadButton.A, mapping.A, index, buttonCount);
        SetButton(ref buttons, GamepadButton.B, mapping.B, index, buttonCount);
        SetButton(ref buttons, GamepadButton.X, mapping.X, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Y, mapping.Y, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Start, mapping.Start, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Back, mapping.Back, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Guide, mapping.Guide, index, buttonCount);
        SetButton(ref buttons, GamepadButton.LeftShoulder, mapping.LeftShoulder, index, buttonCount);
        SetButton(ref buttons, GamepadButton.RightShoulder, mapping.RightShoulder, index, buttonCount);
        SetButton(ref buttons, GamepadButton.LeftStick, mapping.LeftStick, index, buttonCount);
        SetButton(ref buttons, GamepadButton.RightStick, mapping.RightStick, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Touchpad, mapping.Touchpad, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Paddle1, mapping.Paddle1, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Paddle2, mapping.Paddle2, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Paddle3, mapping.Paddle3, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Paddle4, mapping.Paddle4, index, buttonCount);
        SetButton(ref buttons, GamepadButton.Misc1, mapping.Misc1, index, buttonCount);

        SetButton(ref buttons, GamepadButton.DPadUp, mapping.DPadUp, index, buttonCount);
        SetButton(ref buttons, GamepadButton.DPadDown, mapping.DPadDown, index, buttonCount);
        SetButton(ref buttons, GamepadButton.DPadLeft, mapping.DPadLeft, index, buttonCount);
        SetButton(ref buttons, GamepadButton.DPadRight, mapping.DPadRight, index, buttonCount);

        float lx = GetAxisValue(mapping.LeftX, index, deadZone);
        float ly = GetAxisValue(mapping.LeftY, index, deadZone);
        float rx = GetAxisValue(mapping.RightX, index, deadZone);
        float ry = GetAxisValue(mapping.RightY, index, deadZone);

        leftStick = new Vect2(lx, ly);
        rightStick = new Vect2(rx, ry);

        SetStickButtons(ref buttons, lx, ly, GamepadButton.LeftStickLeft, GamepadButton.LeftStickRight,
            GamepadButton.LeftStickUp, GamepadButton.LeftStickDown, deadZone);
        SetStickButtons(ref buttons, rx, ry, GamepadButton.RightStickLeft, GamepadButton.RightStickRight,
            GamepadButton.RightStickUp, GamepadButton.RightStickDown, deadZone);

        leftTrigger = GetAxisValue(mapping.LeftTrigger, index, 0f);
        rightTrigger = GetAxisValue(mapping.RightTrigger, index, 0f);

        if (leftTrigger > deadZone)
            buttons |= 1UL << (int)GamepadButton.LeftTrigger;
        if (rightTrigger > deadZone)
            buttons |= 1UL << (int)GamepadButton.RightTrigger;
    }

    private static void ProcessFallbackInput(ref ulong buttons, ref float leftTrigger, ref float rightTrigger,
        ref Vect2 leftStick, ref Vect2 rightStick, int index, uint buttonCount, float deadZone)
    {
        // SFML default: 0=A,  1=B,  2=X,  3=Y,  4=LB, 5=RB, 6=Back, 7=Start, 8=L3, 9=R3
        // Axes:         0=LX, 1=LY, 2=LT, 3=RX, 4=RY, 5=RT (Xbox layout)

        uint[] sfmlToButton = { (uint)GamepadButton.A, (uint)GamepadButton.B, (uint)GamepadButton.X, (uint)GamepadButton.Y,
                            (uint)GamepadButton.LeftShoulder, (uint)GamepadButton.RightShoulder,
                            (uint)GamepadButton.Back, (uint)GamepadButton.Start,
                            (uint)GamepadButton.LeftStick, (uint)GamepadButton.RightStick };

        for (uint i = 0; i < Math.Min(buttonCount, 10); i++)
        {
            if (SFJoystick.IsButtonPressed((uint)index, i))
                buttons |= 1UL << (int)sfmlToButton[i];
        }

        if (SFJoystick.HasAxis((uint)index, SFJoystick.Axis.PovX))
        {
            float povX = SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.PovX) / 100f;
            float povY = SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.PovY) / 100f;

            if (povX < -deadZone) buttons |= 1UL << (int)GamepadButton.DPadLeft;
            if (povX > deadZone) buttons |= 1UL << (int)GamepadButton.DPadRight;
            if (povY < -deadZone) buttons |= 1UL << (int)GamepadButton.DPadUp;
            if (povY > deadZone) buttons |= 1UL << (int)GamepadButton.DPadDown;
        }

        // Axes - Xbox layout: X=LX, Y=LY, Z=LT, R=RT, U=RX, V=RY
        float lx = ApplyDeadZone(SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.X) / 100f, deadZone);
        float ly = ApplyDeadZone(SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.Y) / 100f, deadZone);
        float rx = ApplyDeadZone(SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.U) / 100f, deadZone);
        float ry = ApplyDeadZone(SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.V) / 100f, deadZone);

        leftStick = new Vect2(lx, ly);
        rightStick = new Vect2(rx, ry);

        SetStickButtons(ref buttons, lx, ly, GamepadButton.LeftStickLeft, GamepadButton.LeftStickRight,
            GamepadButton.LeftStickUp, GamepadButton.LeftStickDown, deadZone);
        SetStickButtons(ref buttons, rx, ry, GamepadButton.RightStickLeft, GamepadButton.RightStickRight,
            GamepadButton.RightStickUp, GamepadButton.RightStickDown, deadZone);

        leftTrigger = ApplyDeadZone((SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.Z) + 100f) / 200f, 0f);
        rightTrigger = ApplyDeadZone((SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.R) + 100f) / 200f, 0f);

        if (leftTrigger > deadZone) buttons |= 1UL << (int)GamepadButton.LeftTrigger;
        if (rightTrigger > deadZone) buttons |= 1UL << (int)GamepadButton.RightTrigger;
    }

    private static void SetButton(ref ulong buttons, GamepadButton button, GamepadInput input, int index, uint buttonCount)
    {
        if (input.Type == InputType.None) return;

        bool pressed = false;

        switch (input.Type)
        {
            case InputType.Button:
                if (input.Index < buttonCount)
                    pressed = SFJoystick.IsButtonPressed((uint)index, (uint)input.Index);
                break;
            case InputType.Hat:
                if (SFJoystick.HasAxis((uint)index, SFJoystick.Axis.PovX))
                {
                    uint povValue = GetHatValue(index);
                    pressed = (povValue & input.HatMask) != 0;
                }
                break;
            case InputType.Axis:
            case InputType.AxisDirection:
                float value = SFJoystick.GetAxisPosition((uint)index, (SFJoystick.Axis)input.Index) / 100f;
                if (input.AxisInverted) value = -value;
                if (input.AxisNegative)
                    pressed = value < -0.5f;
                else
                    pressed = value > 0.5f;
                break;
        }

        if (pressed)
            buttons |= 1UL << (int)button;
    }

    private static float GetAxisValue(GamepadInput input, int index, float deadZone)
    {
        if (input.Type == InputType.None) return 0f;

        float value = 0f;
        if (input.Type == InputType.Axis || input.Type == InputType.AxisDirection)
        {
            value = SFJoystick.GetAxisPosition((uint)index, (SFJoystick.Axis)input.Index) / 100f;
            if (input.AxisInverted) value = -value;
        }
        else if (input.Type == InputType.Button)
        {
            value = SFJoystick.IsButtonPressed((uint)index, (uint)input.Index) ? 1f : 0f;
        }

        return ApplyDeadZone(value, deadZone);
    }

    private static float ApplyDeadZone(float value, float deadZone)
    {
        if (Math.Abs(value) < deadZone)
            return 0f;

        float sign = MathF.Sign(value);
        return sign * (Math.Abs(value) - deadZone) / (1f - deadZone);
    }

    private static void SetStickButtons(ref ulong buttons, float x, float y,
        GamepadButton left, GamepadButton right, GamepadButton up, GamepadButton down, float deadZone)
    {
        if (x < -deadZone) buttons |= 1UL << (int)left;
        if (x > deadZone) buttons |= 1UL << (int)right;
        if (y < -deadZone) buttons |= 1UL << (int)up;
        if (y > deadZone) buttons |= 1UL << (int)down;
    }

    private static uint GetHatValue(int index)
    {
        float povX = SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.PovX);
        float povY = SFJoystick.GetAxisPosition((uint)index, SFJoystick.Axis.PovY);

        uint hat = 0;
        if (povX < -50f) hat |= 8;     // Left
        if (povX > 50f) hat |= 2;      // Right
        if (povY < -50f) hat |= 1;     // Up
        if (povY > 50f) hat |= 4;      // Down

        return hat;
    }
}