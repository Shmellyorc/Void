namespace Void.Engine.Inputs;

public struct KeyboardState
{
    // 101 keys fit in 2 ulongs (128 bits total)
    // Keys 0-63 in _keysLow, Keys 64-100 in _keysHigh
    private ulong _keysLow;
    private ulong _keysHigh;
    private readonly bool _capsLock;
    private readonly bool _numLock;

    public readonly bool CapsLock => _capsLock;
    public readonly bool NumLock => _numLock;

    public KeyState this[KeyboardKey key]
    {
        get
        {
            int index = (int)key;
            if (key == KeyboardKey.Unknown || index < 0 || index >= 101)
                return KeyState.Up;

            bool isPressed;
            if (index < 64)
                isPressed = (_keysLow & (1UL << index)) != 0;
            else
                isPressed = (_keysHigh & (1UL << (index - 64))) != 0;

            return isPressed ? KeyState.Down : KeyState.Up;
        }
    }

    internal KeyboardState(byte[] keyStates, bool capsLock, bool numLock)
    {
        _keysLow = 0;
        _keysHigh = 0;
        _capsLock = capsLock;
        _numLock = numLock;

        if (keyStates != null)
        {
            for (int i = 0; i < Math.Min(101, keyStates.Length); i++)
            {
                if (keyStates[i] == 1)
                    SetKey(i, true);
            }
        }
    }

    internal KeyboardState(ulong keysLow, ulong keysHigh, bool capsLock, bool numLock)
    {
        _keysLow = keysLow;
        _keysHigh = keysHigh;
        _capsLock = capsLock;
        _numLock = numLock;
    }

    private void SetKey(int index, bool pressed)
    {
        if (index < 64)
        {
            if (pressed)
                _keysLow |= (1UL << index);
            else
                _keysLow &= ~(1UL << index);
        }
        else
        {
            int bitIndex = index - 64;
            if (pressed)
                _keysHigh |= (1UL << bitIndex);
            else
                _keysHigh &= ~(1UL << bitIndex);
        }
    }

    private bool IsKeyPressed(int index)
    {
        if (index < 64)
            return (_keysLow & (1UL << index)) != 0;
        else
            return (_keysHigh & (1UL << (index - 64))) != 0;
    }

    public bool IsKeyDown(KeyboardKey key) => this[key] == KeyState.Down;
    public bool IsKeyUp(KeyboardKey key) => this[key] == KeyState.Up;

    public int GetPressedKeyCount()
    {
        int count = 0;
        ulong low = _keysLow;

        while (low != 0)
        {
            count++;
            low &= low - 1; 
        }
        
        ulong high = _keysHigh;
        while (high != 0)
        {
            count++;
            high &= high - 1;
        }
        
        return count;
    }

    public KeyboardKey[] GetPressedKeys()
    {
        var pressed = new List<KeyboardKey>();
        
        for (int i = 0; i < 64; i++)
        {
            if ((_keysLow & (1UL << i)) != 0)
                pressed.Add((KeyboardKey)i);
        }
        
        for (int i = 64; i < 101; i++)
        {
            int bitIndex = i - 64;
            if ((_keysHigh & (1UL << bitIndex)) != 0)
                pressed.Add((KeyboardKey)i);
        }
        
        return pressed.ToArray();
    }

    public void GetPressedKeys(KeyboardKey[] keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        int index = 0;
        
        for (int i = 0; i < 64 && index < keys.Length; i++)
        {
            if ((_keysLow & (1UL << i)) != 0)
                keys[index++] = (KeyboardKey)i;
        }
        
        for (int i = 64; i < 101 && index < keys.Length; i++)
        {
            int bitIndex = i - 64;
            if ((_keysHigh & (1UL << bitIndex)) != 0)
                keys[index++] = (KeyboardKey)i;
        }
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (!(obj is KeyboardState other))
            return false;

        return _keysLow == other._keysLow &&
               _keysHigh == other._keysHigh &&
               _capsLock == other._capsLock &&
               _numLock == other._numLock;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + _keysLow.GetHashCode();
        hash = hash * 31 + _keysHigh.GetHashCode();
        hash = hash * 31 + _capsLock.GetHashCode();
        hash = hash * 31 + _numLock.GetHashCode();
        return hash;
    }

    public static bool operator ==(in KeyboardState a, in KeyboardState b) => a.Equals(b);
    public static bool operator !=(in KeyboardState a, in KeyboardState b) => !a.Equals(b);
}