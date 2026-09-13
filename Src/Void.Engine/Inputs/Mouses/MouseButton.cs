// ============================================================================
//  MouseButton.cs
// ============================================================================
//  Defines the mouse buttons supported by the input system.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Defines the mouse buttons supported by VOID's input system.
/// </summary>
/// <remarks>
/// <para>
/// These values can be queried directly through <see cref="MouseState"/> or used
/// as bindings in the input-action system.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Mouse.GetState();
///
/// if (state.IsButtonPressed(MouseButton.Left))
///     Select();
///
/// if (state.IsButtonPressed(MouseButton.XButton1))
///     NavigateBack();
/// </code>
/// </para>
/// </remarks>
public enum MouseButton
{
    /// <summary>
    /// Represents no mouse button.
    /// </summary>
    None = -1,

    /// <summary>
    /// The primary left mouse button.
    /// </summary>
    Left = 0,

    /// <summary>
    /// The secondary right mouse button.
    /// </summary>
    Right = 1,

    /// <summary>
    /// The middle mouse button, commonly activated by pressing the scroll wheel.
    /// </summary>
    Middle = 2,

    /// <summary>
    /// The first extra mouse button.
    /// </summary>
    Extra1 = 3,

    /// <summary>
    /// The second extra mouse button.
    /// </summary>
    Extra2 = 4
}
