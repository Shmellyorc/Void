// ============================================================================
//  Keyboard.cs
// ============================================================================
//  Provides access to keyboard input with bit-packed key states for
//  low-memory and high-performance key state tracking.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Provides access to keyboard input with bit-packed key states for
/// low-memory and high-performance key state tracking.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Keyboard"/> class manages keyboard input by polling the
/// current state of all keyboard keys and packing them into two 64-bit
/// integers for efficient storage and querying.
/// </para>
/// <para>
/// <b>Key Storage:</b>
/// <list type="bullet">
///   <item><description>Keys 0-63 are stored in <c>_keysLow</c></description></item>
///   <item><description>Keys 64-100 are stored in <c>_keysHigh</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Update keyboard state (called automatically by the engine)
/// Keyboard.Update();
/// 
/// // Get the current keyboard state snapshot
/// var state = Keyboard.GetState();
/// 
/// // Query individual keys
/// if (state.IsKeyDown(KeyboardKey.W))
///     MoveForward();
/// 
/// if (state.IsKeyDown(KeyboardKey.Escape))
///     ExitGame();
/// 
/// // Check modifier keys
/// if (state.IsKeyDown(KeyboardKey.LControl) &amp;&amp; state.IsKeyDown(KeyboardKey.S))
///     SaveGame();
/// </code>
/// </para>
/// <para>
/// <b>Input Focus:</b>
/// When <see cref="GameSettings.IgnoreInputWhenUnfocused"/> is enabled,
/// keyboard input is ignored when the game window is not focused.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed on
/// the main thread.
/// </para>
/// </remarks>
public static class Keyboard
{
    private static ulong _keysLow;
    private static ulong _keysHigh;
    private static bool _capsLock;
    private static bool _numLock;

    /// <summary>
    /// Gets a snapshot of the current keyboard state.
    /// </summary>
    /// <returns>A <see cref="KeyboardState"/> containing the current key states.</returns>
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
            return;

        for (int i = 0; i < 64; i++)
        {
            var key = (SFKeyboard.Key)i;
            if (SFKeyboard.IsKeyPressed(key))
                _keysLow |= (1UL << i);
        }

        for (int i = 64; i < 101; i++)
        {
            var key = (SFKeyboard.Key)i;
            if (SFKeyboard.IsKeyPressed(key))
                _keysHigh |= (1UL << (i - 64));
        }

        _capsLock = false;
        _numLock = false;
    }
}