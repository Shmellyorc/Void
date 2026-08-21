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
/// Defines the state of a keyboard key.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KeyState"/> enumeration represents whether a keyboard key
/// is currently pressed or released. It is used by <see cref="KeyboardState"/>
/// to provide key state information.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Keyboard.GetState();
/// 
/// // Check key state using the indexer
/// if (state[KeyboardKey.Space] == KeyState.Down)
///     Jump();
/// 
/// // Or use the convenience methods
/// if (state.IsKeyDown(KeyboardKey.W))
///     MoveForward();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This enumeration is thread-safe by nature and can be used from any thread.
/// </para>
/// </remarks>
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