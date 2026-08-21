// ============================================================================
//  MouseState.cs
// ============================================================================
//  Represents a snapshot of the mouse state including button states,
//  position, and scroll wheel delta.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System.Diagnostics.CodeAnalysis;
using Void.Engine.Inputs.Gamepads;

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Represents a snapshot of the mouse state including button states,
/// position, and scroll wheel delta.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MouseState"/> structure provides a read-only snapshot of
/// the mouse at a specific moment in time. It is returned by
/// <see cref="Mouse.GetState"/> and should be used for all mouse queries
/// within a frame.
/// </para>
/// <para>
/// Usage Example: state = Mouse.GetState().
/// Check button states with IsButtonPressed() and IsButtonReleased().
/// Get position with Position, X, or Y.
/// Get scroll wheel delta with ScrollWheel (positive = up/forward, negative = down/backward).
/// </para>
/// <para>
/// Button States:
/// <list type="bullet">
///   <item><description><see cref="ButtonState.Pressed"/> - The button is currently pressed</description></item>
///   <item><description><see cref="ButtonState.Released"/> - The button is not pressed</description></item>
/// </list>
/// </para>
/// <para>
/// Scroll Wheel:
/// The scroll wheel delta represents the number of notches scrolled since
/// the previous frame. Positive values indicate scrolling up/forward,
/// negative values indicate scrolling down/backward.
/// </para>
/// <para>
/// Thread Safety:
/// This structure is immutable and thread-safe. All fields are read-only.
/// </para>
/// </remarks>
public struct MouseState
{
    private readonly bool _leftButton;
    private readonly bool _rightButton;
    private readonly bool _middleButton;
    private readonly bool _xButton1;
    private readonly bool _xButton2;
    private readonly int _x;
    private readonly int _y;
    private readonly Vect2 _position;
    private readonly int _scrollWheel;

    /// <summary>
    /// Gets the X-coordinate of the mouse position in screen coordinates.
    /// </summary>
    public readonly int X => _x;

    /// <summary>
    /// Gets the Y-coordinate of the mouse position in screen coordinates.
    /// </summary>
    public readonly int Y => _y;

    /// <summary>
    /// Gets the mouse position as a <see cref="Vect2"/> in screen coordinates.
    /// </summary>
    public readonly Vect2 Position => _position;

    /// <summary>
    /// Gets the scroll wheel delta since the previous frame.
    /// </summary>
    public readonly int ScrollWheel => _scrollWheel;

    /// <summary>
    /// Gets a value indicating whether the left mouse button is pressed.
    /// </summary>
    public readonly bool LeftButton => _leftButton;

    /// <summary>
    /// Gets a value indicating whether the right mouse button is pressed.
    /// </summary>
    public readonly bool RightButton => _rightButton;

    /// <summary>
    /// Gets a value indicating whether the middle mouse button is pressed.
    /// </summary>
    public readonly bool MiddleButton => _middleButton;

    /// <summary>
    /// Gets a value indicating whether the first extended button is pressed.
    /// </summary>
    public readonly bool XButton1 => _xButton1;

    /// <summary>
    /// Gets a value indicating whether the second extended button is pressed.
    /// </summary>
    public readonly bool XButton2 => _xButton2;

    /// <summary>
    /// Gets the state of the specified mouse button.
    /// </summary>
    /// <param name="button">The mouse button to query.</param>
    /// <returns><see cref="ButtonState.Pressed"/> if the button is pressed; otherwise, <see cref="ButtonState.Released"/>.</returns>
    public ButtonState this[MouseButton button]
    {
        get
        {
            return button switch
            {
                MouseButton.Left => _leftButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Right => _rightButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.Middle => _middleButton ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.XButton1 => _xButton1 ? ButtonState.Pressed : ButtonState.Released,
                MouseButton.XButton2 => _xButton2 ? ButtonState.Pressed : ButtonState.Released,
                _ => ButtonState.Released
            };
        }
    }

    internal MouseState(bool[] buttons, int x, int y, int scrollWheel)
    {
        _leftButton = buttons.Length > 0 && buttons[0];
        _rightButton = buttons.Length > 1 && buttons[1];
        _middleButton = buttons.Length > 2 && buttons[2];
        _xButton1 = buttons.Length > 3 && buttons[3];
        _xButton2 = buttons.Length > 4 && buttons[4];
        _x = x;
        _y = y;
        _position = new Vect2(x, y);
        _scrollWheel = scrollWheel;
    }

    /// <summary>
    /// Determines whether the specified mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns><see langword="true"/> if the button is pressed; otherwise, <see langword="false"/>.</returns>
    public bool IsButtonPressed(MouseButton button) => this[button] == ButtonState.Pressed;

    /// <summary>
    /// Determines whether the specified mouse button is currently released.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns><see langword="true"/> if the button is released; otherwise, <see langword="false"/>.</returns>
    public bool IsButtonReleased(MouseButton button) => this[button] == ButtonState.Released;

    /// <summary>
    /// Determines whether the current mouse state is equal to the specified object.
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (!(obj is MouseState other))
            return false;

        return
            _leftButton == other._leftButton &&
            _rightButton == other._rightButton &&
            _middleButton == other._middleButton &&
            _xButton1 == other._xButton1 &&
            _xButton2 == other._xButton2 &&
            _x == other._x &&
            _y == other._y &&
            _scrollWheel == other._scrollWheel;
    }

    /// <summary>
    /// Returns the hash code for the current mouse state.
    /// </summary>
    public override int GetHashCode()
    {
        int hash = 17;

        hash = hash * 31 + _leftButton.GetHashCode();
        hash = hash * 31 + _rightButton.GetHashCode();
        hash = hash * 31 + _middleButton.GetHashCode();
        hash = hash * 31 + _xButton1.GetHashCode();
        hash = hash * 31 + _xButton2.GetHashCode();
        hash = hash * 31 + _x.GetHashCode();
        hash = hash * 31 + _y.GetHashCode();
        hash = hash * 31 + _scrollWheel.GetHashCode();

        return hash;
    }

    /// <summary>
    /// Determines whether two mouse states are equal.
    /// </summary>
    public static bool operator ==(in MouseState a, in MouseState b) => a.Equals(b);

    /// <summary>
    /// Determines whether two mouse states are not equal.
    /// </summary>
    public static bool operator !=(in MouseState a, in MouseState b) => !a.Equals(b);
}