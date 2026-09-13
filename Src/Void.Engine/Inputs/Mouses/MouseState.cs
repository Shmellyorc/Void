// ============================================================================
//  MouseState.cs
// ============================================================================
//  Represents an immutable snapshot of mouse buttons, position, and wheel input.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Diagnostics.CodeAnalysis;

using Void.Engine.Inputs.Gamepads;

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Represents an immutable snapshot of mouse input.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MouseState"/> stores the mouse button states, window-relative
/// cursor position, and scroll-wheel delta captured by <see cref="Mouse.GetState"/>.
/// The wheel value represents movement accumulated since the previous mouse snapshot.
/// Retaining a state value does not cause it to change when later mouse input is read.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Mouse.GetState();
///
/// if (state.IsButtonPressed(MouseButton.Left))
///     SelectAt(state.Position);
///
/// if (state.IsButtonReleased(MouseButton.Right))
///     CloseContextMenu();
///
/// if (state.ScrollWheel > 0)
///     ZoomIn();
/// </code>
/// </para>
/// <para>
/// Unsupported button values, including <see cref="MouseButton.None"/>, are treated
/// as released.
/// </para>
/// </remarks>
public readonly struct MouseState : IEquatable<MouseState>
{
    private readonly bool _leftButton;
    private readonly bool _rightButton;
    private readonly bool _middleButton;
    private readonly bool _extra1;
    private readonly bool _extra2;
    private readonly int _x;
    private readonly int _y;
    private readonly Vect2 _position;
    private readonly int _scrollWheel;

    /// <summary>
    /// Gets the window-relative X-coordinate of the cursor.
    /// </summary>
    public int X => _x;

    /// <summary>
    /// Gets the window-relative Y-coordinate of the cursor.
    /// </summary>
    public int Y => _y;

    /// <summary>
    /// Gets the window-relative cursor position.
    /// </summary>
    public Vect2 Position => _position;

    /// <summary>
    /// Gets the scroll-wheel delta captured for this snapshot.
    /// </summary>
    /// <remarks>
    /// A positive value represents upward or forward scrolling and a negative
    /// value represents downward or backward scrolling. A value of zero means
    /// no wheel movement was captured.
    /// </remarks>
    public int ScrollWheel => _scrollWheel;

    /// <summary>
    /// Gets whether the left mouse button is currently pressed.
    /// </summary>
    public bool LeftButton => _leftButton;

    /// <summary>
    /// Gets whether the right mouse button is currently pressed.
    /// </summary>
    public bool RightButton => _rightButton;

    /// <summary>
    /// Gets whether the middle mouse button is currently pressed.
    /// </summary>
    public bool MiddleButton => _middleButton;

    /// <summary>
    /// Gets whether the first extra mouse button is currently pressed.
    /// </summary>
    public bool Extra1 => _extra1;

    /// <summary>
    /// Gets whether the second extra mouse button is currently pressed.
    /// </summary>
    public bool Extra2 => _extra2;

    /// <summary>
    /// Gets the current state of a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    /// <returns>
    /// <see cref="ButtonState.Pressed"/> when <paramref name="button"/> is pressed;
    /// otherwise, <see cref="ButtonState.Released"/>.
    /// </returns>
    /// <remarks>
    /// Unknown or unsupported button values are treated as released.
    /// </remarks>
    public ButtonState this[MouseButton button]
    {
        get
        {
            return button switch
            {
                MouseButton.Left => _leftButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Right => _rightButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Middle => _middleButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Extra1 => _extra1 ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Extra2 => _extra2 ? ButtonState.Pressed : ButtonState.Released,
                _ => ButtonState.Released
            };
        }
    }

    internal MouseState(bool[] buttons, int x, int y, int scrollWheel)
    {
        _leftButton = buttons.Length > 0 && buttons[0];
        _rightButton = buttons.Length > 1 && buttons[1];
        _middleButton = buttons.Length > 2 && buttons[2];
        _extra1 = buttons.Length > 3 && buttons[3];
        _extra2 = buttons.Length > 4 && buttons[4];
        _x = x;
        _y = y;
        _position = new Vect2(x, y);
        _scrollWheel = scrollWheel;
    }

    /// <summary>
    /// Determines whether a mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="button"/> is pressed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsButtonPressed(MouseButton button)
        => this[button] == ButtonState.Pressed;

    /// <summary>
    /// Determines whether a mouse button is currently released.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="button"/> is released or unsupported;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsButtonReleased(MouseButton button)
        => this[button] == ButtonState.Released;

    /// <summary>
    /// Determines whether this state contains the same mouse input as another state.
    /// </summary>
    /// <param name="other">The mouse state to compare with this state.</param>
    /// <returns>
    /// <see langword="true"/> when the states contain the same input values;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(MouseState other)
    {
        return
            _leftButton == other._leftButton &&
            _rightButton == other._rightButton &&
            _middleButton == other._middleButton &&
            _extra1 == other._extra1 &&
            _extra2 == other._extra2 &&
            _x == other._x &&
            _y == other._y &&
            _scrollWheel == other._scrollWheel;
    }

    /// <summary>
    /// Determines whether this state contains the same mouse input as another object.
    /// </summary>
    /// <param name="obj">The object to compare with this state.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is an equal
    /// <see cref="MouseState"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object obj)
        => obj is MouseState other && Equals(other);

    /// <summary>
    /// Returns a hash code for this mouse state.
    /// </summary>
    /// <returns>A hash code derived from the button, position, and scroll-wheel state.</returns>
    public override int GetHashCode()
    {
        int hash = 17;

        hash = hash * 31 + _leftButton.GetHashCode();
        hash = hash * 31 + _rightButton.GetHashCode();
        hash = hash * 31 + _middleButton.GetHashCode();
        hash = hash * 31 + _extra1.GetHashCode();
        hash = hash * 31 + _extra2.GetHashCode();
        hash = hash * 31 + _x.GetHashCode();
        hash = hash * 31 + _y.GetHashCode();
        hash = hash * 31 + _scrollWheel.GetHashCode();

        return hash;
    }

    /// <summary>
    /// Determines whether two mouse states contain the same input values.
    /// </summary>
    /// <param name="a">The first state to compare.</param>
    /// <param name="b">The second state to compare.</param>
    /// <returns><see langword="true"/> when the states are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(in MouseState a, in MouseState b) => a.Equals(b);

    /// <summary>
    /// Determines whether two mouse states contain different input values.
    /// </summary>
    /// <param name="a">The first state to compare.</param>
    /// <param name="b">The second state to compare.</param>
    /// <returns><see langword="true"/> when the states are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(in MouseState a, in MouseState b) => !a.Equals(b);
}
