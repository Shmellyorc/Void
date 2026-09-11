namespace Void.Engine.Systems;

/// <summary>
/// Selects VOID's preferred SDL window-system backend on Linux.
/// </summary>
/// <remarks>
/// This setting only affects Linux. Windows and macOS continue using their
/// normal native SDL video backends.
/// </remarks>
public enum LinuxWindowBackend
{
    /// <summary>
    /// VOID default. Prefer X11/XWayland first, then fall back to native Wayland
    /// if X11 cannot initialize.
    /// </summary>
    X11ThenWayland = 0,

    /// <summary>
    /// Let SDL use its normal video-backend selection order.
    /// </summary>
    Auto = 1,

    /// <summary>
    /// Force X11. On a Wayland desktop this normally means XWayland.
    /// Initialization fails if X11/XWayland is unavailable.
    /// </summary>
    X11 = 2,

    /// <summary>
    /// Force native Wayland.
    /// </summary>
    Wayland = 3
}
