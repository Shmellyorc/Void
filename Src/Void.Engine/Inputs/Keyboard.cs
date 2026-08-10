namespace Void.Engine.Inputs;

public static class Keyboard
{
    private static ulong _keysLow;
    private static ulong _keysHigh;
    private static bool _capsLock;
    private static bool _numLock;

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
        (!Game.Instance._window.IsOpen || !Game.Instance._window.HasFocus()))
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
    public static void Update() => UpdateState();
}