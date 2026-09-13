// ============================================================================
//  LinuxWindowBackend.cs
// ============================================================================
//  Linux native window-backend preference used during platform initialization.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Selects VOID's preferred window-system backend on Linux.
/// </summary>
/// <remarks>
/// This setting applies only to Linux. Other supported platforms continue using
/// their normal native window backend selection.
/// </remarks>
public enum LinuxWindowBackend
{
    /// <summary>
    /// Prefers X11 or XWayland and falls back to native Wayland if X11 cannot initialize.
    /// </summary>
    X11ThenWayland = 0,

    /// <summary>
    /// Uses the platform layer's normal backend selection order.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Requires X11. On a Wayland desktop this normally uses XWayland.
    /// </summary>
    /// <remarks>
    /// Initialization fails when X11 or XWayland is unavailable.
    /// </remarks>
    X11 = 2,

    /// <summary>
    /// Requires the native Wayland backend.
    /// </summary>
    Wayland = 3
}
