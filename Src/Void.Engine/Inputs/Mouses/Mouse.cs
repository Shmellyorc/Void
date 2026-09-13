// ============================================================================
//  Mouse.cs
// ============================================================================
//  Provides mouse state snapshots, cursor positioning, and scroll-wheel input.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Provides access to the current mouse state and cursor positioning.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GetState"/> returns a <see cref="MouseState"/> snapshot containing
/// the current button states, window-relative cursor position, and scroll-wheel
/// delta for the current game-loop frame.
/// </para>
/// <para>
/// Repeated calls to <see cref="GetState"/> during the same game-loop frame
/// return the same snapshot unless the cursor is repositioned through
/// <see cref="SetPosition(int, int)"/> or <see cref="SetPosition(int, int, Game)"/>.
/// This keeps scroll-wheel input consistent when multiple systems query the mouse
/// during one update.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Mouse.GetState();
///
/// if (state.IsButtonPressed(MouseButton.Left))
///     SelectAt(state.Position);
///
/// if (state.ScrollWheel != 0)
///     Zoom(state.ScrollWheel);
///
/// Mouse.SetPosition(400, 300);
/// </code>
/// </para>
/// <para>
/// When <see cref="GameSettings.IgnoreInputWhenUnfocused"/> is enabled and the
/// game window is closed or unfocused, mouse buttons are reported as released
/// and scroll-wheel movement is suppressed. The last known cursor position is
/// retained until mouse input is read again while the window is active.
/// </para>
/// <para>
/// This type is not thread-safe and should be used from the game's main thread.
/// </para>
/// </remarks>
public static class Mouse
{
    private static readonly bool[] _buttons = new bool[5];
    private static int _x;
    private static int _y;
    private static int _scrollWheel;
    private static int _previousScrollWheel;
    private static long _stateFrame = long.MinValue;
    private static MouseState _state;

    /// <summary>
    /// Gets the mouse state snapshot for the current game-loop frame.
    /// </summary>
    /// <returns>
    /// A <see cref="MouseState"/> containing the current button states,
    /// window-relative cursor position, and scroll-wheel delta since the
    /// previous mouse snapshot.
    /// </returns>
    /// <remarks>
    /// Repeated calls during the same game-loop frame return the same snapshot
    /// unless the cursor is repositioned through one of the <c>SetPosition</c>
    /// overloads.
    /// </remarks>
    public static MouseState GetState()
    {
        long frame = Game.Instance.FrameTime.TotalTime.Ticks;

        if (_stateFrame == frame)
            return _state;

        bool inputEnabled = UpdateState();

        int scrollDelta = inputEnabled
            ? _scrollWheel - _previousScrollWheel
            : 0;

        _previousScrollWheel = _scrollWheel;

        _state = new MouseState(_buttons, _x, _y, scrollDelta);
        _stateFrame = frame;

        return _state;
    }

    private static bool UpdateState()
    {
        var game = Game.Instance;
        var window = game.Window;

        if (GameSettings.Instance.IgnoreInputWhenUnfocused &&
            (!window.IsOpen || !window.IsFocused))
        {
            Array.Clear(_buttons);
            _scrollWheel = game._scrollWheel;
            return false;
        }

        window.GetMouseState(_buttons, out _x, out _y);
        _scrollWheel = game._scrollWheel;
        return true;
    }

    /// <summary>
    /// Sets the mouse cursor position in global screen coordinates.
    /// </summary>
    /// <param name="x">The global X-coordinate.</param>
    /// <param name="y">The global Y-coordinate.</param>
    public static void SetPosition(int x, int y)
    {
        Game.Instance.Window.SetGlobalMousePosition(x, y);
        _stateFrame = long.MinValue;
    }

    /// <summary>
    /// Sets the mouse cursor position relative to the specified game's window.
    /// </summary>
    /// <param name="x">The window-relative X-coordinate.</param>
    /// <param name="y">The window-relative Y-coordinate.</param>
    /// <param name="game">The game whose window receives the cursor position.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="game"/> is <see langword="null"/>.
    /// </exception>
    public static void SetPosition(int x, int y, Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        game.Window.SetMousePosition(x, y);
        _stateFrame = long.MinValue;
    }
}
