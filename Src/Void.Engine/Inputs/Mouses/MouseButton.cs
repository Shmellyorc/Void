// ============================================================================
//  MouseButton.cs
// ============================================================================
//  Defines the available mouse buttons for input handling.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.Mouses;

/// <summary>
/// Defines the available mouse buttons for input handling.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MouseButton"/> enumeration provides a standardized set of
/// mouse button names used throughout the input system. It includes the
/// standard buttons as well as extended buttons found on gaming mice.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = Mouse.GetState();
/// 
/// // Check primary button
/// if (state.IsButtonPressed(MouseButton.Left))
///     SelectObject();
/// 
/// // Check secondary button
/// if (state.IsButtonPressed(MouseButton.Right))
///     OpenContextMenu();
/// 
/// // Check middle button
/// if (state.IsButtonPressed(MouseButton.Middle))
///     PanCamera();
/// 
/// // Check extended buttons
/// if (state.IsButtonPressed(MouseButton.XButton1))
///     Back();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This enumeration is thread-safe by nature and can be used from any thread.
/// </para>
/// </remarks>
public enum MouseButton
{
    /// <summary>
    /// Represents no mouse button. Used for unbound or invalid button references.
    /// </summary>
    None = -1,

    /// <summary>
    /// The left mouse button.
    /// </summary>
    Left = 0,

    /// <summary>
    /// The right mouse button.
    /// </summary>
    Right = 1,

    /// <summary>
    /// The middle mouse button (scroll wheel button).
    /// </summary>
    Middle = 2,

    /// <summary>
    /// The first extended mouse button (often used for forward/back navigation).
    /// </summary>
    XButton1 = 3,

    /// <summary>
    /// The second extended mouse button (often used for forward/back navigation).
    /// </summary>
    XButton2 = 4
}