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
/// Represents an immutable snapshot of the keyboard state.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="KeyboardState"/> contains the key and keyboard lock states captured
/// by <see cref="Keyboard.GetState"/>. Because it is a snapshot, later keyboard
/// changes do not modify an existing value.
/// </para>
/// <para>
/// Supported keys can be queried through the indexer, <see cref="IsKeyDown"/>,
/// or <see cref="IsKeyUp"/>. Unknown, unbound, and out-of-range
/// <see cref="KeyboardKey"/> values are treated as <see cref="KeyState.Up"/>.
/// </para>
/// <para>
/// Pressed keys can be retrieved as a new array with <see cref="GetPressedKeys()"/>
/// or copied into an existing array with <see cref="GetPressedKeys(KeyboardKey[])"/>.
/// </para>
/// </remarks>
public readonly struct KeyboardState
{
    private readonly ulong _keysLow;
    private readonly ulong _keysHigh;
    private readonly bool _capsLock;
    private readonly bool _numLock;

    /// <summary>
    /// Gets whether Caps Lock was active when this snapshot was created.
    /// </summary>
    public bool CapsLock => _capsLock;

    /// <summary>
    /// Gets whether Num Lock was active when this snapshot was created.
    /// </summary>
    public bool NumLock => _numLock;

    /// <summary>
    /// Gets the state of a keyboard key in this snapshot.
    /// </summary>
    /// <param name="key">The keyboard key to query.</param>
    /// <returns>
    /// <see cref="KeyState.Down"/> when the key is pressed; otherwise,
    /// <see cref="KeyState.Up"/>. Unknown and out-of-range values return
    /// <see cref="KeyState.Up"/>.
    /// </returns>
    public KeyState this[KeyboardKey key]
    {
        get
        {
            int index = (int)key;
            if (key == KeyboardKey.Unknown || index < 0 || index >= (int)KeyboardKey.KeyCount)
                return KeyState.Up;

            bool isPressed;
            if (index < 64)
                isPressed = (_keysLow & (1UL << index)) != 0;
            else
                isPressed = (_keysHigh & (1UL << (index - 64))) != 0;

            return isPressed ? KeyState.Down : KeyState.Up;
        }
    }

    internal KeyboardState(ulong keysLow, ulong keysHigh, bool capsLock, bool numLock)
    {
        _keysLow = keysLow;
        _keysHigh = keysHigh;
        _capsLock = capsLock;
        _numLock = numLock;
    }

    /// <summary>
    /// Determines whether a keyboard key is currently pressed in this snapshot.
    /// </summary>
    /// <param name="key">The keyboard key to query.</param>
    /// <returns>
    /// <see langword="true"/> when the key is down; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsKeyDown(KeyboardKey key) => this[key] == KeyState.Down;

    /// <summary>
    /// Determines whether a keyboard key is currently up in this snapshot.
    /// </summary>
    /// <param name="key">The keyboard key to query.</param>
    /// <returns>
    /// <see langword="true"/> when the key is up, unknown, or out of range;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsKeyUp(KeyboardKey key) => this[key] == KeyState.Up;

    /// <summary>
    /// Gets the number of keys that are currently pressed in this snapshot.
    /// </summary>
    /// <returns>The number of pressed keys.</returns>
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
    /// Gets all keys that are currently pressed in this snapshot.
    /// </summary>
    /// <returns>
    /// A new array containing each pressed <see cref="KeyboardKey"/> in numeric key order.
    /// </returns>
    public KeyboardKey[] GetPressedKeys()
    {
        var pressed = new List<KeyboardKey>();
        int keyCount = (int)KeyboardKey.KeyCount;

        for (int i = 0; i < 64; i++)
        {
            if ((_keysLow & (1UL << i)) != 0)
                pressed.Add((KeyboardKey)i);
        }

        for (int i = 64; i < keyCount; i++)
        {
            int bitIndex = i - 64;
            if ((_keysHigh & (1UL << bitIndex)) != 0)
                pressed.Add((KeyboardKey)i);
        }

        return pressed.ToArray();
    }

    /// <summary>
    /// Copies the currently pressed keys into an existing array.
    /// </summary>
    /// <remarks>
    /// Keys are written in numeric key order until either every pressed key has
    /// been copied or <paramref name="keys"/> is full. Elements after the last
    /// written key are left unchanged.
    /// </remarks>
    /// <param name="keys">The destination array that receives the pressed keys.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="keys"/> is <see langword="null"/>.
    /// </exception>
    public void GetPressedKeys(KeyboardKey[] keys)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        int index = 0;
        int keyCount = (int)KeyboardKey.KeyCount;

        for (int i = 0; i < 64 && index < keys.Length; i++)
        {
            if ((_keysLow & (1UL << i)) != 0)
                keys[index++] = (KeyboardKey)i;
        }

        for (int i = 64; i < keyCount && index < keys.Length; i++)
        {
            int bitIndex = i - 64;
            if ((_keysHigh & (1UL << bitIndex)) != 0)
                keys[index++] = (KeyboardKey)i;
        }
    }

    /// <summary>
    /// Determines whether this snapshot contains the same key and lock states as another object.
    /// </summary>
    /// <param name="obj">The object to compare with this snapshot.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is an equivalent
    /// <see cref="KeyboardState"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is not KeyboardState other)
            return false;

        return _keysLow == other._keysLow &&
               _keysHigh == other._keysHigh &&
               _capsLock == other._capsLock &&
               _numLock == other._numLock;
    }

    /// <summary>
    /// Returns a hash code for this keyboard snapshot.
    /// </summary>
    /// <returns>A hash code derived from the key and lock states.</returns>
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
    /// Determines whether two keyboard snapshots contain the same key and lock states.
    /// </summary>
    /// <param name="a">The first keyboard snapshot.</param>
    /// <param name="b">The second keyboard snapshot.</param>
    /// <returns><see langword="true"/> when the snapshots are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(in KeyboardState a, in KeyboardState b) => a.Equals(b);

    /// <summary>
    /// Determines whether two keyboard snapshots contain different key or lock states.
    /// </summary>
    /// <param name="a">The first keyboard snapshot.</param>
    /// <param name="b">The second keyboard snapshot.</param>
    /// <returns><see langword="true"/> when the snapshots are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(in KeyboardState a, in KeyboardState b) => !a.Equals(b);
}
