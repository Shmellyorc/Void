namespace Void.Engine.Systems;

/// <summary>
/// Identifies the native window-system backend currently used by VOID.
/// This is intentionally independent of SDL types.
/// </summary>
public enum NativeWindowBackend
{
    Unknown,
    Windows,
    X11,
    Wayland,
    Cocoa,
    Android,
    UIKit,
    KmsDrm,
    Other
}

/// <summary>
/// Describes window/display operations that the active native backend can
/// reliably provide to VOID.
/// </summary>
/// <remarks>
/// These values describe the backend SDL actually initialized, not merely the
/// operating system. For example, a Linux Wayland desktop running VOID through
/// XWayland reports <see cref="NativeWindowBackend.X11"/>.
/// </remarks>
public readonly struct WindowCapabilities
{
    internal WindowCapabilities(
        NativeWindowBackend backend,
        string backendName,
        bool canPositionWindow,
        bool canSelectWindowedDisplay,
        bool canSelectDesktopFullscreenDisplay,
        bool canSelectExclusiveFullscreenDisplay,
        bool supportsDisplayEnumeration,
        bool supportsDisplayModes,
        bool supportsContentScale)
    {
        Backend = backend;
        BackendName = backendName ?? string.Empty;
        CanPositionWindow = canPositionWindow;
        CanSelectWindowedDisplay = canSelectWindowedDisplay;
        CanSelectDesktopFullscreenDisplay = canSelectDesktopFullscreenDisplay;
        CanSelectExclusiveFullscreenDisplay = canSelectExclusiveFullscreenDisplay;
        SupportsDisplayEnumeration = supportsDisplayEnumeration;
        SupportsDisplayModes = supportsDisplayModes;
        SupportsContentScale = supportsContentScale;
    }

    /// <summary>The native window-system backend currently active.</summary>
    public NativeWindowBackend Backend { get; }

    /// <summary>
    /// Native backend name reported by the platform layer, such as
    /// "x11", "wayland", "windows", or "cocoa".
    /// </summary>
    public string BackendName { get; }

    /// <summary>
    /// Whether VOID can request an arbitrary X/Y position for a normal
    /// top-level window.
    /// </summary>
    public bool CanPositionWindow { get; }

    /// <summary>
    /// Whether a windowed/borderless window can be moved to a specific display.
    /// </summary>
    public bool CanSelectWindowedDisplay { get; }

    /// <summary>
    /// Whether desktop/borderless fullscreen can be deterministically targeted
    /// at a specific display by VOID.
    /// </summary>
    public bool CanSelectDesktopFullscreenDisplay { get; }

    /// <summary>
    /// Whether exclusive fullscreen can target a specific display through a
    /// display-specific fullscreen mode.
    /// </summary>
    public bool CanSelectExclusiveFullscreenDisplay { get; }

    /// <summary>Whether connected displays can be enumerated.</summary>
    public bool SupportsDisplayEnumeration { get; }

    /// <summary>Whether fullscreen display modes can be queried.</summary>
    public bool SupportsDisplayModes { get; }

    /// <summary>Whether display content/DPI scaling information is available.</summary>
    public bool SupportsContentScale { get; }

    /// <summary>
    /// True when placement of ordinary top-level windows is controlled by the
    /// compositor instead of by the application.
    /// </summary>
    public bool UsesCompositorWindowPlacement => !CanPositionWindow;

    public override string ToString()
        => $"{Backend} ({BackendName})";
}
