// ============================================================================
//  KeyboardKey.cs
// ============================================================================
//  Defines all keyboard keys supported by the input system.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Defines all keyboard keys supported by the input system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KeyboardKey"/> enumeration provides a comprehensive list
/// of keyboard keys, including letters, numbers, function keys, navigation
/// keys, and modifier keys.
/// </para>
/// <para>
/// <b>Key Categories:</b>
/// <list type="bullet">
///   <item><description><b>Letters:</b> <see cref="A"/> through <see cref="Z"/></description></item>
///   <item><description><b>Numbers:</b> <see cref="Num0"/> through <see cref="Num9"/></description></item>
///   <item><description><b>Function Keys:</b> <see cref="F1"/> through <see cref="F15"/></description></item>
///   <item><description><b>Navigation:</b> <see cref="Left"/>, <see cref="Right"/>, <see cref="Up"/>, <see cref="Down"/>, <see cref="PageUp"/>, <see cref="PageDown"/>, <see cref="Home"/>, <see cref="End"/></description></item>
///   <item><description><b>Modifiers:</b> <see cref="LControl"/>, <see cref="RControl"/>, <see cref="LShift"/>, <see cref="RShift"/>, <see cref="LAlt"/>, <see cref="RAlt"/></description></item>
///   <item><description><b>Numpad:</b> <see cref="Numpad0"/> through <see cref="Numpad9"/>, <see cref="Add"/>, <see cref="Subtract"/>, <see cref="Multiply"/>, <see cref="Divide"/></description></item>
///   <item><description><b>System:</b> <see cref="LSystem"/>, <see cref="RSystem"/>, <see cref="Menu"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Keyboard.GetState();
/// 
/// // Check letter keys
/// if (state.IsKeyDown(KeyboardKey.W))
///     MoveForward();
/// 
/// // Check modifier combinations
/// if (state.IsKeyDown(KeyboardKey.LControl) &amp;&amp; state.IsKeyDown(KeyboardKey.S))
///     SaveGame();
/// 
/// // Check navigation keys
/// if (state.IsKeyDown(KeyboardKey.Left))
///     MoveLeft();
/// 
/// // Check function keys
/// if (state.IsKeyDown(KeyboardKey.F11))
///     ToggleFullscreen();
/// </code>
/// </para>
/// <para>
/// <b>Obsolete Keys:</b>
/// Some keys have been renamed for consistency. The obsolete entries remain
/// for backward compatibility but should not be used in new code.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This enumeration is thread-safe by nature and can be used from any thread.
/// </para>
/// </remarks>
public enum KeyboardKey
{
    /// <summary>
    /// Represents an unknown or unbound key.
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
    /// The 0 key.
    /// </summary>
    Num0 = 26,

    /// <summary>
    /// The 1 key.
    /// </summary>
    Num1 = 27,

    /// <summary>
    /// The 2 key.
    /// </summary>
    Num2 = 28,

    /// <summary>
    /// The 3 key.
    /// </summary>
    Num3 = 29,

    /// <summary>
    /// The 4 key.
    /// </summary>
    Num4 = 30,

    /// <summary>
    /// The 5 key.
    /// </summary>
    Num5 = 31,

    /// <summary>
    /// The 6 key.
    /// </summary>
    Num6 = 32,

    /// <summary>
    /// The 7 key.
    /// </summary>
    Num7 = 33,

    /// <summary>
    /// The 8 key.
    /// </summary>
    Num8 = 34,

    /// <summary>
    /// The 9 key.
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
    /// The left system key (Windows key on Windows, Command on macOS).
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
    /// The right system key (Windows key on Windows, Command on macOS).
    /// </summary>
    RSystem = 44,

    /// <summary>
    /// The menu key (context menu key on Windows).
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
    /// The slash key (/).
    /// </summary>
    Slash = 52,

    /// <summary>
    /// The backslash key (\).
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
    /// The space key.
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
    /// The Numpad Add key (+).
    /// </summary>
    Add = 67,

    /// <summary>
    /// The Numpad Subtract key (-).
    /// </summary>
    Subtract = 68,

    /// <summary>
    /// The Numpad Multiply key (*).
    /// </summary>
    Multiply = 69,

    /// <summary>
    /// The Numpad Divide key (/).
    /// </summary>
    Divide = 70,

    /// <summary>
    /// The Left arrow key.
    /// </summary>
    Left = 71,

    /// <summary>
    /// The Right arrow key.
    /// </summary>
    Right = 72,

    /// <summary>
    /// The Up arrow key.
    /// </summary>
    Up = 73,

    /// <summary>
    /// The Down arrow key.
    /// </summary>
    Down = 74,

    /// <summary>
    /// The Numpad 0 key.
    /// </summary>
    Numpad0 = 75,

    /// <summary>
    /// The Numpad 1 key.
    /// </summary>
    Numpad1 = 76,

    /// <summary>
    /// The Numpad 2 key.
    /// </summary>
    Numpad2 = 77,

    /// <summary>
    /// The Numpad 3 key.
    /// </summary>
    Numpad3 = 78,

    /// <summary>
    /// The Numpad 4 key.
    /// </summary>
    Numpad4 = 79,

    /// <summary>
    /// The Numpad 5 key.
    /// </summary>
    Numpad5 = 80,

    /// <summary>
    /// The Numpad 6 key.
    /// </summary>
    Numpad6 = 81,

    /// <summary>
    /// The Numpad 7 key.
    /// </summary>
    Numpad7 = 82,

    /// <summary>
    /// The Numpad 8 key.
    /// </summary>
    Numpad8 = 83,

    /// <summary>
    /// The Numpad 9 key.
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
    /// The total number of keyboard keys.
    /// </summary>
    KeyCount = 101,

    /// <summary>
    /// Obsolete. Use <see cref="Grave"/> instead.
    /// </summary>
    [Obsolete("Replace with Grave")]
    Tilde = 54,

    /// <summary>
    /// Obsolete. Use <see cref="Hyphen"/> instead.
    /// </summary>
    [Obsolete("Replace with Hyphen")]
    Dash = 56,

    /// <summary>
    /// Obsolete. Use <see cref="Backspace"/> instead.
    /// </summary>
    [Obsolete("Replace with Backspace")]
    BackSpace = 59,

    /// <summary>
    /// Obsolete. Use <see cref="Enter"/> instead.
    /// </summary>
    [Obsolete("Replace with Enter")]
    Return = 58,

    /// <summary>
    /// Obsolete. Use <see cref="Backslash"/> instead.
    /// </summary>
    [Obsolete("Replace with Backslash")]
    BackSlash = 53,

    /// <summary>
    /// Obsolete. Use <see cref="Semicolon"/> instead.
    /// </summary>
    [Obsolete("Replace with Semicolon")]
    SemiColon = 48,

    /// <summary>
    /// Obsolete. Use <see cref="Apostrophe"/> instead.
    /// </summary>
    [Obsolete("Replace with Apostrophe")]
    Quote = 51,

    /// <summary>
    /// Alias for <see cref="Unknown"/>.
    /// </summary>
    None = Unknown,
}