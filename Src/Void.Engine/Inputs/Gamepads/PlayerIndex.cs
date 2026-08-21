// ============================================================================
//  PlayerIndex.cs
// ============================================================================
//  Defines the player indices for up to four gamepad controllers.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Gamepads;

/// <summary>
/// Defines the player indices for up to four gamepad controllers.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PlayerIndex"/> enumeration is used to identify which
/// gamepad to query when calling <see cref="Gamepad.GetState(PlayerIndex)"/>
/// and <see cref="Gamepad.Update(PlayerIndex)"/>.
/// </para>
/// <para>
/// Each value maps directly to the underlying joystick index (0-3).
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the state for player one
/// var state = Gamepad.GetState(PlayerIndex.One);
/// 
/// // Update player two's gamepad
/// Gamepad.Update(PlayerIndex.Two);
/// 
/// // Update all players
/// Gamepad.UpdateAll();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This enumeration is thread-safe by nature and can be used from any thread.
/// </para>
/// </remarks>
public enum PlayerIndex
{
    /// <summary>
    /// The first gamepad (index 0).
    /// </summary>
    One = 0,

    /// <summary>
    /// The second gamepad (index 1).
    /// </summary>
    Two = 1,

    /// <summary>
    /// The third gamepad (index 2).
    /// </summary>
    Three = 2,

    /// <summary>
    /// The fourth gamepad (index 3).
    /// </summary>
    Four = 3
}