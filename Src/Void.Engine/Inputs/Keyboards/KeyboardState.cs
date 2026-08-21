// ============================================================================
//  KeyboardState.cs
// ============================================================================
//  Represents a snapshot of the keyboard state with bit-packed key storage
//  and query methods for key states, lock states, and pressed key lists.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Represents a snapshot of the keyboard state with query methods for
/// key states, lock states, and pressed key lists.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KeyboardState"/> structure provides a read-only snapshot of
/// the keyboard at a specific moment in time. It is returned by
/// <see cref="Keyboard.GetState"/> and should be used for all keyboard
/// queries within a frame.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Keyboard.GetState();
/// 
/// // Check individual keys
/// if (state.IsKeyDown(KeyboardKey.W))
///     MoveForward();
/// 
/// if (state.IsKeyUp(KeyboardKey.Escape))
///     // Key is not pressed
/// 
/// // Get all pressed keys
/// var pressedKeys = state.GetPressedKeys();
/// 
/// // Get pressed key count
/// int count = state.GetPressedKeyCount();
/// 
/// // Check lock states
/// if (state.CapsLock)
///     // Caps Lock is on
/// 
/// // Indexer access
/// if (state[KeyboardKey.Space] == KeyState.Down)
///     Jump();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe. All fields are read-only.
/// </para>
/// </remarks>
public struct KeyboardState
{
    // 101 keys fit in 2 ulongs (128 bits total)
    // Keys 0-63 in _keysLow, Keys 64-100 in _keysHigh
    private ulong _keysLow;
    private ulong _keysHigh;
    private readonly bool _capsLock;
    private readonly bool _numLock;

    /// <summary>
    /// Gets a value indicating whether Caps Lock is active.
    /// </summary>
    public readonly bool CapsLock => _capsLock;

    /// <summary>
    /// Gets a value indicating whether Num Lock is active.
    /// </summary>
    public readonly bool NumLock => _numLock;

    /// <summary>
    /// Gets the state of the specified key.
    /// </summary>
    /// <param name="key">The key to query.</param>
    /// <returns><see cref="KeyState.Down"/> if the key is pressed; otherwise, <see cref="KeyState.Up"/>.</returns>
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

    /// <summary>
    /// Determines whether the specified key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if the key is pressed; otherwise, <see langword="false"/>.</returns>
    public bool IsKeyDown(KeyboardKey key) => this[key] == KeyState.Down;

    /// <summary>
    /// Determines whether the specified key is currently released.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if the key is released; otherwise, <see langword="false"/>.</returns>
    public bool IsKeyUp(KeyboardKey key) => this[key] == KeyState.Up;

    /// <summary>
    /// Gets the number of pressed keys.
    /// </summary>
    /// <returns>The count of keys that are currently pressed.</returns>
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

    /// <summary>
    /// Gets an array of all pressed keys.
    /// </summary>
    /// <returns>An array of <see cref="KeyboardKey"/> values that are currently pressed.</returns>
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

    /// <summary>
    /// Fills the provided array with the currently pressed keys.
    /// </summary>
    /// <param name="keys">The array to fill with pressed keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keys"/> is null.</exception>
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

    /// <summary>
    /// Determines whether the current keyboard state is equal to the specified object.
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (!(obj is KeyboardState other))
            return false;

        return _keysLow == other._keysLow &&
               _keysHigh == other._keysHigh &&
               _capsLock == other._capsLock &&
               _numLock == other._numLock;
    }

    /// <summary>
    /// Returns the hash code for the current keyboard state.
    /// </summary>
    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + _keysLow.GetHashCode();
        hash = hash * 31 + _keysHigh.GetHashCode();
        hash = hash * 31 + _capsLock.GetHashCode();
        hash = hash * 31 + _numLock.GetHashCode();
        return hash;
    }

    /// <summary>
    /// Determines whether two keyboard states are equal.
    /// </summary>
    public static bool operator ==(in KeyboardState a, in KeyboardState b) => a.Equals(b);

    /// <summary>
    /// Determines whether two keyboard states are not equal.
    /// </summary>
    public static bool operator !=(in KeyboardState a, in KeyboardState b) => !a.Equals(b);
}