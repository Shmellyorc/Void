// ============================================================================
//  WindowCapabilities.cs
// ============================================================================
//  Native window-backend identification and capability reporting.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Identifies the native window-system backend currently used by VOID.
/// </summary>
/// <remarks>
/// This enum exposes renderer-neutral backend names and does not expose SDL types.
/// </remarks>
public enum NativeWindowBackend
{
    /// <summary>The backend could not be identified.</summary>
    Unknown,

    /// <summary>Microsoft Windows desktop backend.</summary>
    Windows,

    /// <summary>X11 backend.</summary>
    X11,

    /// <summary>Native Wayland backend.</summary>
    Wayland,

    /// <summary>macOS Cocoa backend.</summary>
    Cocoa,

    /// <summary>Android backend.</summary>
    Android,

    /// <summary>Apple UIKit backend.</summary>
    UIKit,

    /// <summary>Linux KMS/DRM backend.</summary>
    KmsDrm,

    /// <summary>A recognized backend not represented by another enum value.</summary>
    Other
}

/// <summary>
/// Describes window and display capabilities provided by the active native backend.
/// </summary>
/// <remarks>
/// Capabilities describe the backend actually initialized by the platform layer,
/// not only the operating system. For example, a Wayland desktop running VOID
/// through XWayland reports <see cref="NativeWindowBackend.X11"/>.
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

    /// <summary>
    /// Gets the native window-system backend currently active.
    /// </summary>
    public NativeWindowBackend Backend { get; }

    /// <summary>
    /// Gets the native backend name reported by the platform layer.
    /// </summary>
    /// <remarks>
    /// Typical values include <c>x11</c>, <c>wayland</c>, <c>windows</c>, and <c>cocoa</c>.
    /// </remarks>
    public string BackendName { get; }

    /// <summary>
    /// Gets whether VOID can request an arbitrary position for a normal top-level window.
    /// </summary>
    public bool CanPositionWindow { get; }

    /// <summary>
    /// Gets whether a windowed or borderless window can be moved to a specific display.
    /// </summary>
    public bool CanSelectWindowedDisplay { get; }

    /// <summary>
    /// Gets whether desktop fullscreen can be targeted at a specific display.
    /// </summary>
    public bool CanSelectDesktopFullscreenDisplay { get; }

    /// <summary>
    /// Gets whether exclusive fullscreen can target a specific display mode.
    /// </summary>
    public bool CanSelectExclusiveFullscreenDisplay { get; }

    /// <summary>
    /// Gets whether connected displays can be enumerated.
    /// </summary>
    public bool SupportsDisplayEnumeration { get; }

    /// <summary>
    /// Gets whether fullscreen display modes can be queried.
    /// </summary>
    public bool SupportsDisplayModes { get; }

    /// <summary>
    /// Gets whether display content or DPI scaling information is available.
    /// </summary>
    public bool SupportsContentScale { get; }

    /// <summary>
    /// Gets whether ordinary top-level window placement is controlled by the compositor.
    /// </summary>
    public bool UsesCompositorWindowPlacement => !CanPositionWindow;

    /// <summary>
    /// Returns the backend identifier and platform-reported backend name.
    /// </summary>
    /// <returns>A string in <c>Backend (BackendName)</c> form.</returns>
    public override string ToString()
        => $"{Backend} ({BackendName})";
}
