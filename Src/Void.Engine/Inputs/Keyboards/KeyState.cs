// ============================================================================
//  KeyState.cs
// ============================================================================
//  Defines the state of a keyboard key.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Keyboards;

/// <summary>
/// Specifies whether a keyboard key is currently up or down.
/// </summary>
public enum KeyState
{
    /// <summary>
    /// The key is not currently pressed.
    /// </summary>
    Up,

    /// <summary>
    /// The key is currently pressed.
    /// </summary>
    Down
}
