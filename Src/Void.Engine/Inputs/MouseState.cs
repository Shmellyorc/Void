namespace Void.Engine.Inputs;

public struct MouseState
{
    private readonly bool _leftButton;
    private readonly bool _rightButton;
    private readonly bool _middleButton;
    private readonly bool _xButton1;
    private readonly bool _xButton2;
    private readonly int _x;
    private readonly int _y;
    private readonly int _scrollWheel;

    public readonly int X => _x;
    public readonly int Y => _y;
    public readonly int ScrollWheel => _scrollWheel;

    public readonly bool LeftButton => _leftButton;
    public readonly bool RightButton => _rightButton;
    public readonly bool MiddleButton => _middleButton;
    public readonly bool XButton1 => _xButton1;
    public readonly bool XButton2 => _xButton2;


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
        _scrollWheel = scrollWheel;
    }

    public bool IsButtonPressed(MouseButton button) => this[button] == ButtonState.Pressed;
    public bool IsButtonReleased(MouseButton button) => this[button] == ButtonState.Released;

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

    public static bool operator ==(in MouseState a, in MouseState b) => a.Equals(b);
    public static bool operator !=(in MouseState a, in MouseState b) => !a.Equals(b);
}