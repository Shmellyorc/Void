// ============================================================================
//  Gamepad.cs
// ============================================================================
//  SDL3-backed gamepad input for up to four connected gamepads.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Provides access to mapped gamepad input for up to four connected gamepads.
/// SDL3 is an internal implementation detail; callers continue to use VOID's
/// GamepadButton and GamepadState types.
/// </summary>
public static class Gamepad
{
    private const int MaxGamepads = 4;

    private static readonly GamepadState[] _states = new GamepadState[MaxGamepads];
    private static readonly IntPtr[] _handles = new IntPtr[MaxGamepads];
    private static readonly uint[] _instanceIds = new uint[MaxGamepads];
    private static bool _initialized;

    /// <summary>
    /// Initializes the gamepad system and loads VOID's bundled SDL mapping database.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        if (!SDL3.SDL.InitSubSystem(SDL3.SDL.InitFlags.Gamepad))
            throw new InvalidOperationException($"SDL gamepad initialization failed: {SDL3.SDL.GetError()}");

        try
        {
            GamepadDatabase.Load();
            _initialized = true;
        }
        catch
        {
            GamepadDatabase.Reset();
            SDL3.SDL.QuitSubSystem(SDL3.SDL.InitFlags.Gamepad);
            throw;
        }
    }

    /// <summary>
    /// Gets the current state of the gamepad for the specified player.
    /// </summary>
    public static GamepadState GetState(PlayerIndex player) => GetState((int)player);

    /// <summary>
    /// Gets the current state of the gamepad at the specified index.
    /// </summary>
    public static GamepadState GetState(int index = 0)
    {
        Initialize();
        if (index < 0 || index >= MaxGamepads)
            return default;

        RefreshGamepads();
        UpdateState(index);
        return _states[index];
    }

    internal static void Shutdown()
    {
        for (int i = 0; i < MaxGamepads; i++)
            CloseSlot(i);

        if (!_initialized)
            return;

        SDL3.SDL.QuitSubSystem(SDL3.SDL.InitFlags.Gamepad);
        GamepadDatabase.Reset();
        _initialized = false;
    }

    private static void RefreshGamepads()
    {
        uint[] ids = SDL3.SDL.GetGamepads(out int count) ?? [];
        int available = Math.Min(Math.Min(count, ids.Length), MaxGamepads);

        for (int i = 0; i < MaxGamepads; i++)
        {
            uint desiredId = i < available ? ids[i] : 0;

            if (desiredId == 0)
            {
                CloseSlot(i);
                continue;
            }

            if (_instanceIds[i] == desiredId &&
                _handles[i] != IntPtr.Zero &&
                SDL3.SDL.GamepadConnected(_handles[i]))
            {
                continue;
            }

            CloseSlot(i);

            IntPtr handle = SDL3.SDL.OpenGamepad(desiredId);
            if (handle == IntPtr.Zero)
                continue;

            _handles[i] = handle;
            _instanceIds[i] = desiredId;
        }
    }

    private static void CloseSlot(int index)
    {
        if (_handles[index] != IntPtr.Zero)
        {
            SDL3.SDL.CloseGamepad(_handles[index]);
            _handles[index] = IntPtr.Zero;
        }

        _instanceIds[index] = 0;
        _states[index] = default;
    }

    private static void UpdateState(int index)
    {
        if (GameSettings.Instance.IgnoreInputWhenUnfocused &&
            (!Game.Instance.Window.IsOpen || !Game.Instance.Window.IsFocused))
        {
            _states[index] = DisconnectedState();
            return;
        }

        IntPtr gamepad = _handles[index];
        if (gamepad == IntPtr.Zero || !SDL3.SDL.GamepadConnected(gamepad))
        {
            _states[index] = DisconnectedState();
            return;
        }

        ulong buttons = 0;

        SetButton(ref buttons, GamepadButton.A, gamepad, SDL3.SDL.GamepadButton.South);
        SetButton(ref buttons, GamepadButton.B, gamepad, SDL3.SDL.GamepadButton.East);
        SetButton(ref buttons, GamepadButton.X, gamepad, SDL3.SDL.GamepadButton.West);
        SetButton(ref buttons, GamepadButton.Y, gamepad, SDL3.SDL.GamepadButton.North);

        SetButton(ref buttons, GamepadButton.DPadUp, gamepad, SDL3.SDL.GamepadButton.DPadUp);
        SetButton(ref buttons, GamepadButton.DPadDown, gamepad, SDL3.SDL.GamepadButton.DPadDown);
        SetButton(ref buttons, GamepadButton.DPadLeft, gamepad, SDL3.SDL.GamepadButton.DPadLeft);
        SetButton(ref buttons, GamepadButton.DPadRight, gamepad, SDL3.SDL.GamepadButton.DPadRight);

        SetButton(ref buttons, GamepadButton.LeftShoulder, gamepad, SDL3.SDL.GamepadButton.LeftShoulder);
        SetButton(ref buttons, GamepadButton.RightShoulder, gamepad, SDL3.SDL.GamepadButton.RightShoulder);
        SetButton(ref buttons, GamepadButton.LeftStick, gamepad, SDL3.SDL.GamepadButton.LeftStick);
        SetButton(ref buttons, GamepadButton.RightStick, gamepad, SDL3.SDL.GamepadButton.RightStick);
        SetButton(ref buttons, GamepadButton.Start, gamepad, SDL3.SDL.GamepadButton.Start);
        SetButton(ref buttons, GamepadButton.Back, gamepad, SDL3.SDL.GamepadButton.Back);
        SetButton(ref buttons, GamepadButton.Guide, gamepad, SDL3.SDL.GamepadButton.Guide);

        // SDL's paddle ordering is right-upper, left-upper, right-lower, left-lower.
        SetButton(ref buttons, GamepadButton.Paddle1, gamepad, SDL3.SDL.GamepadButton.RightPaddle1);
        SetButton(ref buttons, GamepadButton.Paddle2, gamepad, SDL3.SDL.GamepadButton.LeftPaddle1);
        SetButton(ref buttons, GamepadButton.Paddle3, gamepad, SDL3.SDL.GamepadButton.RightPaddle2);
        SetButton(ref buttons, GamepadButton.Paddle4, gamepad, SDL3.SDL.GamepadButton.LeftPaddle2);
        SetButton(ref buttons, GamepadButton.Touchpad, gamepad, SDL3.SDL.GamepadButton.Touchpad);
        SetButton(ref buttons, GamepadButton.Misc1, gamepad, SDL3.SDL.GamepadButton.Misc1);

        float deadZone = GameSettings.Instance.DeadZone;

        float lx = ApplyDeadZone(ReadStickAxis(gamepad, SDL3.SDL.GamepadAxis.LeftX), deadZone);
        float ly = ApplyDeadZone(ReadStickAxis(gamepad, SDL3.SDL.GamepadAxis.LeftY), deadZone);
        float rx = ApplyDeadZone(ReadStickAxis(gamepad, SDL3.SDL.GamepadAxis.RightX), deadZone);
        float ry = ApplyDeadZone(ReadStickAxis(gamepad, SDL3.SDL.GamepadAxis.RightY), deadZone);

        Vect2 leftStick = new(lx, ly);
        Vect2 rightStick = new(rx, ry);

        SetStickButtons(ref buttons, lx, ly,
            GamepadButton.LeftStickLeft, GamepadButton.LeftStickRight,
            GamepadButton.LeftStickUp, GamepadButton.LeftStickDown);

        SetStickButtons(ref buttons, rx, ry,
            GamepadButton.RightStickLeft, GamepadButton.RightStickRight,
            GamepadButton.RightStickUp, GamepadButton.RightStickDown);

        float leftTrigger = ReadTriggerAxis(gamepad, SDL3.SDL.GamepadAxis.LeftTrigger);
        float rightTrigger = ReadTriggerAxis(gamepad, SDL3.SDL.GamepadAxis.RightTrigger);

        if (leftTrigger > deadZone)
            buttons |= 1UL << (int)GamepadButton.LeftTrigger;
        if (rightTrigger > deadZone)
            buttons |= 1UL << (int)GamepadButton.RightTrigger;

        _states[index] = new GamepadState(
            buttons,
            leftTrigger,
            rightTrigger,
            leftStick,
            rightStick,
            true);
    }

    private static void SetButton(
        ref ulong buttons,
        GamepadButton voidButton,
        IntPtr gamepad,
        SDL3.SDL.GamepadButton sdlButton)
    {
        if (SDL3.SDL.GetGamepadButton(gamepad, sdlButton))
            buttons |= 1UL << (int)voidButton;
    }

    private static float ReadStickAxis(IntPtr gamepad, SDL3.SDL.GamepadAxis axis)
    {
        short raw = SDL3.SDL.GetGamepadAxis(gamepad, axis);
        return raw >= 0 ? raw / 32767f : raw / 32768f;
    }

    private static float ReadTriggerAxis(IntPtr gamepad, SDL3.SDL.GamepadAxis axis)
    {
        short raw = SDL3.SDL.GetGamepadAxis(gamepad, axis);
        if (raw <= 0)
            return 0f;

        return Math.Clamp(raw / 32767f, 0f, 1f);
    }

    private static float ApplyDeadZone(float value, float deadZone)
    {
        deadZone = Math.Clamp(deadZone, 0f, 0.9999f);

        if (MathF.Abs(value) < deadZone)
            return 0f;

        float sign = MathF.Sign(value);
        return sign * (MathF.Abs(value) - deadZone) / (1f - deadZone);
    }

    private static void SetStickButtons(
        ref ulong buttons,
        float x,
        float y,
        GamepadButton left,
        GamepadButton right,
        GamepadButton up,
        GamepadButton down)
    {
        if (x < 0f) buttons |= 1UL << (int)left;
        if (x > 0f) buttons |= 1UL << (int)right;
        if (y < 0f) buttons |= 1UL << (int)up;
        if (y > 0f) buttons |= 1UL << (int)down;
    }

    private static GamepadState DisconnectedState()
        => new(0, 0f, 0f, Vect2.Zero, Vect2.Zero, false);
}
