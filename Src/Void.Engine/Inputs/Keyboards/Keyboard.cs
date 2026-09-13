// ============================================================================
//  Keyboard.cs
// ============================================================================
//  SDL3-backed keyboard polling with VOID's bit-packed state format.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Provides access to the current keyboard state.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Keyboard"/> polls SDL3 and converts supported keys into VOID's
/// bit-packed <see cref="KeyboardState"/> snapshot format. Use <see cref="GetState"/>
/// when direct keyboard input is needed.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var keyboard = Keyboard.GetState();
///
/// if (keyboard.IsKeyDown(KeyboardKey.W))
///     MoveForward();
///
/// if (keyboard.IsKeyDown(KeyboardKey.Space))
///     Jump();
///
/// if (keyboard.CapsLock)
///     ShowCapsLockIndicator();
/// </code>
/// </para>
/// <para>
/// Each call to <see cref="GetState"/> polls the current keyboard state. The
/// returned <see cref="KeyboardState"/> is a snapshot and does not change after
/// it is created.
/// </para>
/// <para>
/// When <see cref="GameSettings.IgnoreInputWhenUnfocused"/> is enabled and the
/// game window is closed or unfocused, keyboard input is suppressed. All keys
/// are reported as up and the Caps Lock and Num Lock values are reported as
/// inactive for that snapshot.
/// </para>
/// <para>
/// This class uses shared polling state and is not synchronized. Keyboard input
/// should be queried from the game thread.
/// </para>
/// </remarks>
public static class Keyboard
{
    private static ulong _keysLow;
    private static ulong _keysHigh;
    private static bool _capsLock;
    private static bool _numLock;

    // KeyboardKey keeps VOID's numeric values because they are used as packed
    // state indexes. SDL scancodes use a different layout and must be mapped.
    private static readonly SDL3.SDL.Scancode[] _scancodes =
    [
        SDL3.SDL.Scancode.A,
        SDL3.SDL.Scancode.B,
        SDL3.SDL.Scancode.C,
        SDL3.SDL.Scancode.D,
        SDL3.SDL.Scancode.E,
        SDL3.SDL.Scancode.F,
        SDL3.SDL.Scancode.G,
        SDL3.SDL.Scancode.H,
        SDL3.SDL.Scancode.I,
        SDL3.SDL.Scancode.J,
        SDL3.SDL.Scancode.K,
        SDL3.SDL.Scancode.L,
        SDL3.SDL.Scancode.M,
        SDL3.SDL.Scancode.N,
        SDL3.SDL.Scancode.O,
        SDL3.SDL.Scancode.P,
        SDL3.SDL.Scancode.Q,
        SDL3.SDL.Scancode.R,
        SDL3.SDL.Scancode.S,
        SDL3.SDL.Scancode.T,
        SDL3.SDL.Scancode.U,
        SDL3.SDL.Scancode.V,
        SDL3.SDL.Scancode.W,
        SDL3.SDL.Scancode.X,
        SDL3.SDL.Scancode.Y,
        SDL3.SDL.Scancode.Z,

        SDL3.SDL.Scancode.Alpha0,
        SDL3.SDL.Scancode.Alpha1,
        SDL3.SDL.Scancode.Alpha2,
        SDL3.SDL.Scancode.Alpha3,
        SDL3.SDL.Scancode.Alpha4,
        SDL3.SDL.Scancode.Alpha5,
        SDL3.SDL.Scancode.Alpha6,
        SDL3.SDL.Scancode.Alpha7,
        SDL3.SDL.Scancode.Alpha8,
        SDL3.SDL.Scancode.Alpha9,

        SDL3.SDL.Scancode.Escape,
        SDL3.SDL.Scancode.LCtrl,
        SDL3.SDL.Scancode.LShift,
        SDL3.SDL.Scancode.LAlt,
        SDL3.SDL.Scancode.LGUI,
        SDL3.SDL.Scancode.RCtrl,
        SDL3.SDL.Scancode.RShift,
        SDL3.SDL.Scancode.RAlt,
        SDL3.SDL.Scancode.RGUI,
        SDL3.SDL.Scancode.Application,
        SDL3.SDL.Scancode.Leftbracket,
        SDL3.SDL.Scancode.Rightbracket,
        SDL3.SDL.Scancode.Semicolon,
        SDL3.SDL.Scancode.Comma,
        SDL3.SDL.Scancode.Period,
        SDL3.SDL.Scancode.Apostrophe,
        SDL3.SDL.Scancode.Slash,
        SDL3.SDL.Scancode.Backslash,
        SDL3.SDL.Scancode.Grave,
        SDL3.SDL.Scancode.Equals,
        SDL3.SDL.Scancode.Minus,
        SDL3.SDL.Scancode.Space,
        SDL3.SDL.Scancode.Return,
        SDL3.SDL.Scancode.Backspace,
        SDL3.SDL.Scancode.Tab,
        SDL3.SDL.Scancode.Pageup,
        SDL3.SDL.Scancode.Pagedown,
        SDL3.SDL.Scancode.End,
        SDL3.SDL.Scancode.Home,
        SDL3.SDL.Scancode.Insert,
        SDL3.SDL.Scancode.Delete,
        SDL3.SDL.Scancode.KpPlus,
        SDL3.SDL.Scancode.KpMinus,
        SDL3.SDL.Scancode.KpMultiply,
        SDL3.SDL.Scancode.KpDivide,
        SDL3.SDL.Scancode.Left,
        SDL3.SDL.Scancode.Right,
        SDL3.SDL.Scancode.Up,
        SDL3.SDL.Scancode.Down,
        SDL3.SDL.Scancode.Kp0,
        SDL3.SDL.Scancode.Kp1,
        SDL3.SDL.Scancode.Kp2,
        SDL3.SDL.Scancode.Kp3,
        SDL3.SDL.Scancode.Kp4,
        SDL3.SDL.Scancode.Kp5,
        SDL3.SDL.Scancode.Kp6,
        SDL3.SDL.Scancode.Kp7,
        SDL3.SDL.Scancode.Kp8,
        SDL3.SDL.Scancode.Kp9,
        SDL3.SDL.Scancode.F1,
        SDL3.SDL.Scancode.F2,
        SDL3.SDL.Scancode.F3,
        SDL3.SDL.Scancode.F4,
        SDL3.SDL.Scancode.F5,
        SDL3.SDL.Scancode.F6,
        SDL3.SDL.Scancode.F7,
        SDL3.SDL.Scancode.F8,
        SDL3.SDL.Scancode.F9,
        SDL3.SDL.Scancode.F10,
        SDL3.SDL.Scancode.F11,
        SDL3.SDL.Scancode.F12,
        SDL3.SDL.Scancode.F13,
        SDL3.SDL.Scancode.F14,
        SDL3.SDL.Scancode.F15,
        SDL3.SDL.Scancode.Pause,
    ];
    /// <summary>
    /// Polls the keyboard and returns a snapshot of its current state.
    /// </summary>
    /// <remarks>
    /// Each call reads the current SDL keyboard state before creating the snapshot.
    /// When <see cref="GameSettings.IgnoreInputWhenUnfocused"/> is enabled and the
    /// game window is closed or unfocused, the returned snapshot reports every key
    /// as up and both lock states as inactive.
    /// </remarks>
    /// <returns>A snapshot containing the current key and keyboard lock states.</returns>
    public static KeyboardState GetState()
    {
        UpdateState();
        return new KeyboardState(_keysLow, _keysHigh, _capsLock, _numLock);
    }

    private static void UpdateState()
    {
        _keysLow = 0;
        _keysHigh = 0;

        if (GameSettings.Instance.IgnoreInputWhenUnfocused &&
            (!Game.Instance.Window.IsOpen || !Game.Instance.Window.IsFocused))
        {
            _capsLock = false;
            _numLock = false;
            return;
        }

        ReadOnlySpan<bool> state = SDL3.SDL.GetKeyboardState(out int keyCount);
        int voidKeyCount = Math.Min(_scancodes.Length, (int)KeyboardKey.KeyCount);

        for (int i = 0; i < voidKeyCount; i++)
        {
            int scancode = (int)_scancodes[i];
            if (scancode < 0 || scancode >= keyCount || scancode >= state.Length || !state[scancode])
                continue;

            if (i < 64)
                _keysLow |= 1UL << i;
            else
                _keysHigh |= 1UL << (i - 64);
        }

        var modifiers = SDL3.SDL.GetModState();
        _capsLock = (modifiers & SDL3.SDL.Keymod.Caps) != SDL3.SDL.Keymod.None;
        _numLock = (modifiers & SDL3.SDL.Keymod.Num) != SDL3.SDL.Keymod.None;
    }
}
