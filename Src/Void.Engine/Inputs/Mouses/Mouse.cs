// ============================================================================
//  Mouse.cs
// ============================================================================
//  Provides access to mouse input including position, button states, and
//  scroll wheel delta.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Provides access to mouse input including position, button states, and
/// scroll wheel delta.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Mouse"/> class manages mouse input by polling the current
/// state of all mouse buttons, position, and scroll wheel. It provides
/// snapshot-based state access through <see cref="GetState"/>.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the current mouse state
/// var state = Mouse.GetState();
/// 
/// // Check button states
/// if (state.IsButtonPressed(MouseButton.Left))
///     HandleClick();
/// 
/// // Get mouse position
/// var position = state.Position;
/// float x = state.X;
/// float y = state.Y;
/// 
/// // Get scroll wheel delta
/// int scroll = state.ScrollWheel;
/// 
/// // Set mouse position
/// Mouse.SetPosition(100, 200);
/// </code>
/// </para>
/// <para>
/// <b>Scroll Wheel:</b>
/// The scroll wheel delta represents the number of notches scrolled since
/// the last frame. Positive values indicate scrolling up, negative values
/// indicate scrolling down.
/// </para>
/// <para>
/// <b>Input Focus:</b>
/// When <see cref="GameSettings.IgnoreInputWhenUnfocused"/> is enabled,
/// mouse input is ignored when the game window is not focused.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed on
/// the main thread.
/// </para>
/// </remarks>
public static class Mouse
{
    private readonly static bool[] _buttons = new bool[5];
    private static int _x;
    private static int _y;
    private static int _scrollWheel;
    private static int _previousScrollWheel;

    /// <summary>
    /// Gets a snapshot of the current mouse state.
    /// </summary>
    /// <returns>A <see cref="MouseState"/> containing the current button states, position, and scroll delta.</returns>
    public static MouseState GetState()
    {
        UpdateState();

        int delta = _scrollWheel - _previousScrollWheel;
        _previousScrollWheel = _scrollWheel;

        return new MouseState(_buttons, _x, _y, delta);
    }

    private static void UpdateState()
    {
        if (GameSettings.Instance.IgnoreInputWhenUnfocused &&
            (!Game.Instance.Window.IsOpen || !Game.Instance.Window.IsFocused))
            return;

        for (int i = 0; i < 5; i++)
        {
            var sfmlButton = (SFMouse.Button)i;
            _buttons[i] = SFMouse.IsButtonPressed(sfmlButton);
        }

        var pos = SFMouse.GetPosition(Game.Instance.Window._window);
        _x = pos.X;
        _y = pos.Y;
        _scrollWheel = Game.Instance._scrollWheel;
    }

    /// <summary>
    /// Sets the mouse cursor position in screen coordinates.
    /// </summary>
    /// <param name="x">The X-coordinate to set the mouse position to.</param>
    /// <param name="y">The Y-coordinate to set the mouse position to.</param>
    public static void SetPosition(int x, int y)
        => SFMouse.SetPosition(new Vect2(x, y));

    /// <summary>
    /// Sets the mouse cursor position relative to the specified game window.
    /// </summary>
    /// <param name="x">The X-coordinate to set the mouse position to.</param>
    /// <param name="y">The Y-coordinate to set the mouse position to.</param>
    /// <param name="game">The game instance containing the target window.</param>
    public static void SetPosition(int x, int y, Game game)
        => SFMouse.SetPosition(new Vect2(x, y), game.Window);

    /// <summary>
    /// Updates the mouse state. This method is called automatically
    /// by the engine and should not need to be called manually.
    /// </summary>
    public static void Update() => UpdateState();
}