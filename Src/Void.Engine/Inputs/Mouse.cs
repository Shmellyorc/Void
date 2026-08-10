namespace Void.Engine.Inputs;

public static class Mouse
{
    private readonly static bool[] _buttons = new bool[5];
    private static int _x;
    private static int _y;
    private static int _scrollWheel;
    private static int _previousScrollWheel;

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
        (!Game.Instance._window.IsOpen || !Game.Instance._window.HasFocus()))
            return;

        for (int i = 0; i < 5; i++)
        {
            var sfmlButton = (SFMouse.Button)i;
            _buttons[i] = SFMouse.IsButtonPressed(sfmlButton);
        }

        var pos = SFMouse.GetPosition();
        _x = pos.X;
        _y = pos.Y;
        _scrollWheel = Game.Instance._scrollWheel;
    }

    public static void SetPosition(int x, int y)
        => SFMouse.SetPosition(new Vect2(x, y));

    public static void SetPosition(int x, int y, Game game)
        => SFMouse.SetPosition(new Vect2(x, y), game._window);
    public static void Update() => UpdateState();
}