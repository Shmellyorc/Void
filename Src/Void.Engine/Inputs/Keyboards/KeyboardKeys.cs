// ============================================================================
//  KeyboardKeys.cs
// ============================================================================
//  Defines all keyboard keys supported by the input system.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Identifies a keyboard key supported by VOID's keyboard input system.
/// </summary>
/// <remarks>
/// <para>
/// Values from <see cref="A"/> through <see cref="Pause"/> are used as indexes
/// into the packed key data stored by <see cref="KeyboardState"/>. Their numeric
/// values are therefore part of the keyboard-state layout and should remain stable.
/// </para>
/// <para>
/// <see cref="Unknown"/> and <see cref="None"/> are non-key values and are treated
/// as up by <see cref="KeyboardState"/>. <see cref="KeyCount"/> marks the number
/// of supported packed keys and is not itself a keyboard key.
/// </para>
/// </remarks>
public enum KeyboardKey
{
    /// <summary>
    /// Represents an unknown keyboard key.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The A key.
    /// </summary>
    A = 0,

    /// <summary>
    /// The B key.
    /// </summary>
    B = 1,

    /// <summary>
    /// The C key.
    /// </summary>
    C = 2,

    /// <summary>
    /// The D key.
    /// </summary>
    D = 3,

    /// <summary>
    /// The E key.
    /// </summary>
    E = 4,

    /// <summary>
    /// The F key.
    /// </summary>
    F = 5,

    /// <summary>
    /// The G key.
    /// </summary>
    G = 6,

    /// <summary>
    /// The H key.
    /// </summary>
    H = 7,

    /// <summary>
    /// The I key.
    /// </summary>
    I = 8,

    /// <summary>
    /// The J key.
    /// </summary>
    J = 9,

    /// <summary>
    /// The K key.
    /// </summary>
    K = 10,

    /// <summary>
    /// The L key.
    /// </summary>
    L = 11,

    /// <summary>
    /// The M key.
    /// </summary>
    M = 12,

    /// <summary>
    /// The N key.
    /// </summary>
    N = 13,

    /// <summary>
    /// The O key.
    /// </summary>
    O = 14,

    /// <summary>
    /// The P key.
    /// </summary>
    P = 15,

    /// <summary>
    /// The Q key.
    /// </summary>
    Q = 16,

    /// <summary>
    /// The R key.
    /// </summary>
    R = 17,

    /// <summary>
    /// The S key.
    /// </summary>
    S = 18,

    /// <summary>
    /// The T key.
    /// </summary>
    T = 19,

    /// <summary>
    /// The U key.
    /// </summary>
    U = 20,

    /// <summary>
    /// The V key.
    /// </summary>
    V = 21,

    /// <summary>
    /// The W key.
    /// </summary>
    W = 22,

    /// <summary>
    /// The X key.
    /// </summary>
    X = 23,

    /// <summary>
    /// The Y key.
    /// </summary>
    Y = 24,

    /// <summary>
    /// The Z key.
    /// </summary>
    Z = 25,

    /// <summary>
    /// The 0 key on the main keyboard.
    /// </summary>
    Num0 = 26,

    /// <summary>
    /// The 1 key on the main keyboard.
    /// </summary>
    Num1 = 27,

    /// <summary>
    /// The 2 key on the main keyboard.
    /// </summary>
    Num2 = 28,

    /// <summary>
    /// The 3 key on the main keyboard.
    /// </summary>
    Num3 = 29,

    /// <summary>
    /// The 4 key on the main keyboard.
    /// </summary>
    Num4 = 30,

    /// <summary>
    /// The 5 key on the main keyboard.
    /// </summary>
    Num5 = 31,

    /// <summary>
    /// The 6 key on the main keyboard.
    /// </summary>
    Num6 = 32,

    /// <summary>
    /// The 7 key on the main keyboard.
    /// </summary>
    Num7 = 33,

    /// <summary>
    /// The 8 key on the main keyboard.
    /// </summary>
    Num8 = 34,

    /// <summary>
    /// The 9 key on the main keyboard.
    /// </summary>
    Num9 = 35,

    /// <summary>
    /// The Escape key.
    /// </summary>
    Escape = 36,

    /// <summary>
    /// The left Control key.
    /// </summary>
    LControl = 37,

    /// <summary>
    /// The left Shift key.
    /// </summary>
    LShift = 38,

    /// <summary>
    /// The left Alt key.
    /// </summary>
    LAlt = 39,

    /// <summary>
    /// The left system key, such as the Windows key or Command key.
    /// </summary>
    LSystem = 40,

    /// <summary>
    /// The right Control key.
    /// </summary>
    RControl = 41,

    /// <summary>
    /// The right Shift key.
    /// </summary>
    RShift = 42,

    /// <summary>
    /// The right Alt key.
    /// </summary>
    RAlt = 43,

    /// <summary>
    /// The right system key, such as the Windows key or Command key.
    /// </summary>
    RSystem = 44,

    /// <summary>
    /// The application or context-menu key.
    /// </summary>
    Menu = 45,

    /// <summary>
    /// The left bracket key ([).
    /// </summary>
    LBracket = 46,

    /// <summary>
    /// The right bracket key (]).
    /// </summary>
    RBracket = 47,

    /// <summary>
    /// The semicolon key (;).
    /// </summary>
    Semicolon = 48,

    /// <summary>
    /// The comma key (,).
    /// </summary>
    Comma = 49,

    /// <summary>
    /// The period key (.).
    /// </summary>
    Period = 50,

    /// <summary>
    /// The apostrophe key (').
    /// </summary>
    Apostrophe = 51,

    /// <summary>
    /// The forward slash key (/).
    /// </summary>
    Slash = 52,

    /// <summary>
    /// The backslash key (\\).
    /// </summary>
    Backslash = 53,

    /// <summary>
    /// The grave accent key (`).
    /// </summary>
    Grave = 54,

    /// <summary>
    /// The equals key (=).
    /// </summary>
    Equal = 55,

    /// <summary>
    /// The hyphen key (-).
    /// </summary>
    Hyphen = 56,

    /// <summary>
    /// The Space key.
    /// </summary>
    Space = 57,

    /// <summary>
    /// The Enter key.
    /// </summary>
    Enter = 58,

    /// <summary>
    /// The Backspace key.
    /// </summary>
    Backspace = 59,

    /// <summary>
    /// The Tab key.
    /// </summary>
    Tab = 60,

    /// <summary>
    /// The Page Up key.
    /// </summary>
    PageUp = 61,

    /// <summary>
    /// The Page Down key.
    /// </summary>
    PageDown = 62,

    /// <summary>
    /// The End key.
    /// </summary>
    End = 63,

    /// <summary>
    /// The Home key.
    /// </summary>
    Home = 64,

    /// <summary>
    /// The Insert key.
    /// </summary>
    Insert = 65,

    /// <summary>
    /// The Delete key.
    /// </summary>
    Delete = 66,

    /// <summary>
    /// The numeric keypad Add key (+).
    /// </summary>
    Add = 67,

    /// <summary>
    /// The numeric keypad Subtract key (-).
    /// </summary>
    Subtract = 68,

    /// <summary>
    /// The numeric keypad Multiply key (*).
    /// </summary>
    Multiply = 69,

    /// <summary>
    /// The numeric keypad Divide key (/).
    /// </summary>
    Divide = 70,

    /// <summary>
    /// The Left Arrow key.
    /// </summary>
    Left = 71,

    /// <summary>
    /// The Right Arrow key.
    /// </summary>
    Right = 72,

    /// <summary>
    /// The Up Arrow key.
    /// </summary>
    Up = 73,

    /// <summary>
    /// The Down Arrow key.
    /// </summary>
    Down = 74,

    /// <summary>
    /// The 0 key on the numeric keypad.
    /// </summary>
    Numpad0 = 75,

    /// <summary>
    /// The 1 key on the numeric keypad.
    /// </summary>
    Numpad1 = 76,

    /// <summary>
    /// The 2 key on the numeric keypad.
    /// </summary>
    Numpad2 = 77,

    /// <summary>
    /// The 3 key on the numeric keypad.
    /// </summary>
    Numpad3 = 78,

    /// <summary>
    /// The 4 key on the numeric keypad.
    /// </summary>
    Numpad4 = 79,

    /// <summary>
    /// The 5 key on the numeric keypad.
    /// </summary>
    Numpad5 = 80,

    /// <summary>
    /// The 6 key on the numeric keypad.
    /// </summary>
    Numpad6 = 81,

    /// <summary>
    /// The 7 key on the numeric keypad.
    /// </summary>
    Numpad7 = 82,

    /// <summary>
    /// The 8 key on the numeric keypad.
    /// </summary>
    Numpad8 = 83,

    /// <summary>
    /// The 9 key on the numeric keypad.
    /// </summary>
    Numpad9 = 84,

    /// <summary>
    /// The F1 function key.
    /// </summary>
    F1 = 85,

    /// <summary>
    /// The F2 function key.
    /// </summary>
    F2 = 86,

    /// <summary>
    /// The F3 function key.
    /// </summary>
    F3 = 87,

    /// <summary>
    /// The F4 function key.
    /// </summary>
    F4 = 88,

    /// <summary>
    /// The F5 function key.
    /// </summary>
    F5 = 89,

    /// <summary>
    /// The F6 function key.
    /// </summary>
    F6 = 90,

    /// <summary>
    /// The F7 function key.
    /// </summary>
    F7 = 91,

    /// <summary>
    /// The F8 function key.
    /// </summary>
    F8 = 92,

    /// <summary>
    /// The F9 function key.
    /// </summary>
    F9 = 93,

    /// <summary>
    /// The F10 function key.
    /// </summary>
    F10 = 94,

    /// <summary>
    /// The F11 function key.
    /// </summary>
    F11 = 95,

    /// <summary>
    /// The F12 function key.
    /// </summary>
    F12 = 96,

    /// <summary>
    /// The F13 function key.
    /// </summary>
    F13 = 97,

    /// <summary>
    /// The F14 function key.
    /// </summary>
    F14 = 98,

    /// <summary>
    /// The F15 function key.
    /// </summary>
    F15 = 99,

    /// <summary>
    /// The Pause key.
    /// </summary>
    Pause = 100,

    /// <summary>
    /// The number of supported keyboard keys stored in a keyboard snapshot.
    /// </summary>
    KeyCount = 101,

    /// <summary>
    /// Represents no keyboard binding. This value is equivalent to <see cref="Unknown"/>.
    /// </summary>
    None = Unknown,
}
