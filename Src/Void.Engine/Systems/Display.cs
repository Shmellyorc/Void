// ============================================================================
//  Display.cs
// ============================================================================
//  Renderer-neutral display information, display modes, and display change events.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Identifies a connected display without exposing the native platform handle.
/// </summary>
/// <remarks>
/// A display ID remains stable while the display stays connected, even if its
/// enumeration index changes.
/// </remarks>
public readonly struct DisplayId : IEquatable<DisplayId>
{
    private readonly uint _value;

    internal DisplayId(uint value) => _value = value;

    internal uint NativeValue => _value;

    /// <summary>
    /// Gets a value indicating whether this instance refers to a valid display.
    /// </summary>
    public bool IsValid => _value != 0;

    /// <summary>
    /// Determines whether this display ID is equal to another display ID.
    /// </summary>
    /// <param name="other">The display ID to compare.</param>
    /// <returns><see langword="true"/> if both IDs refer to the same display; otherwise, <see langword="false"/>.</returns>
    public bool Equals(DisplayId other) => _value == other._value;

    /// <summary>
    /// Determines whether this display ID is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is an equal <see cref="DisplayId"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object obj) => obj is DisplayId other && Equals(other);

    /// <summary>
    /// Returns the hash code for this display ID.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Determines whether two display IDs are equal.
    /// </summary>
    public static bool operator ==(DisplayId left, DisplayId right) => left.Equals(right);

    /// <summary>
    /// Determines whether two display IDs are not equal.
    /// </summary>
    public static bool operator !=(DisplayId left, DisplayId right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of this display ID.
    /// </summary>
    /// <returns>A string identifying the display, or <c>Display(Invalid)</c> when invalid.</returns>
    public override string ToString() => IsValid ? $"Display({_value})" : "Display(Invalid)";
}

/// <summary>
/// Determines how VOID enters fullscreen mode.
/// </summary>
public enum FullscreenStyle
{
    /// <summary>
    /// Uses desktop or borderless fullscreen without requesting a display mode change.
    /// </summary>
    Desktop,

    /// <summary>
    /// Requests an exclusive fullscreen display mode.
    /// </summary>
    Exclusive
}

/// <summary>
/// Describes a resolution and refresh-rate mode supported by a display.
/// </summary>
/// <remarks>
/// Equality compares the resolution exactly and compares refresh rate and pixel
/// density using the same epsilon-based quantization used by <see cref="Vect2"/>.
/// </remarks>
public readonly struct DisplayMode : IEquatable<DisplayMode>
{
    /// <summary>
    /// Gets the mode width in logical display coordinates.
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// Gets the mode height in logical display coordinates.
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// Gets the bits-per-pixel value when reported by the platform.
    /// </summary>
    /// <remarks>
    /// A value of zero indicates that the platform did not provide this information.
    /// </remarks>
    public uint BitsPerPixel { get; }

    /// <summary>
    /// Gets the nominal refresh rate in hertz.
    /// </summary>
    /// <remarks>
    /// A value of zero indicates that no nominal refresh rate was reported.
    /// </remarks>
    public float RefreshRate { get; }

    /// <summary>
    /// Gets the logical-to-pixel scale associated with this mode.
    /// </summary>
    public float PixelDensity { get; }

    /// <summary>
    /// Gets the exact refresh-rate numerator when supplied by the platform.
    /// </summary>
    public int RefreshRateNumerator { get; }

    /// <summary>
    /// Gets the exact refresh-rate denominator when supplied by the platform.
    /// </summary>
    public int RefreshRateDenominator { get; }

    /// <summary>
    /// Gets the most precise refresh rate available in hertz.
    /// </summary>
    /// <remarks>
    /// Uses the exact numerator and denominator when both are positive; otherwise,
    /// falls back to <see cref="RefreshRate"/>.
    /// </remarks>
    public double ExactRefreshRate
        => RefreshRateNumerator > 0 && RefreshRateDenominator > 0
            ? (double)RefreshRateNumerator / RefreshRateDenominator
            : RefreshRate;

    internal DisplayMode(
        int width,
        int height,
        float refreshRate,
        float pixelDensity,
        int refreshRateNumerator,
        int refreshRateDenominator,
        uint bitsPerPixel = 0)
    {
        Width = width > 0 ? (uint)width : 0u;
        Height = height > 0 ? (uint)height : 0u;
        BitsPerPixel = bitsPerPixel;
        RefreshRate = refreshRate;
        PixelDensity = pixelDensity;
        RefreshRateNumerator = refreshRateNumerator;
        RefreshRateDenominator = refreshRateDenominator;
    }

    /// <summary>
    /// Determines whether this display mode is equal to another display mode.
    /// </summary>
    /// <param name="other">The display mode to compare.</param>
    /// <returns><see langword="true"/> if the modes are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(DisplayMode other)
        => Width == other.Width &&
           Height == other.Height &&
           new Vect2(RefreshRate, PixelDensity)
               .Equals(new Vect2(other.RefreshRate, other.PixelDensity));

    /// <summary>
    /// Determines whether this display mode is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is an equal <see cref="DisplayMode"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object obj) => obj is DisplayMode other && Equals(other);

    /// <summary>
    /// Returns the hash code for this display mode.
    /// </summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
        => HashCode.Combine(
            Width,
            Height,
            new Vect2(RefreshRate, PixelDensity).GetHashCode());

    /// <summary>
    /// Determines whether two display modes are equal.
    /// </summary>
    public static bool operator ==(DisplayMode left, DisplayMode right) => left.Equals(right);

    /// <summary>
    /// Determines whether two display modes are not equal.
    /// </summary>
    public static bool operator !=(DisplayMode left, DisplayMode right) => !left.Equals(right);

    /// <summary>
    /// Returns a human-readable representation of this display mode.
    /// </summary>
    /// <returns>A string containing the resolution and optional refresh rate and pixel density.</returns>
    public override string ToString()
    {
        string refresh = RefreshRate > 0f ? $" @ {RefreshRate:0.###} Hz" : string.Empty;
        string density = PixelDensity > 0f && MathF.Abs(PixelDensity - 1f) > 0.001f
            ? $" ({PixelDensity:0.##}x)"
            : string.Empty;
        return $"{Width}x{Height}{refresh}{density}";
    }
}

/// <summary>
/// Provides an immutable snapshot of one connected display.
/// </summary>
public sealed class DisplayInfo
{
    private readonly DisplayMode[] _supportedModes;

    internal DisplayInfo(
        DisplayId id,
        int index,
        string name,
        bool isPrimary,
        Rect2 bounds,
        Rect2 workArea,
        float contentScale,
        DisplayMode desktopMode,
        DisplayMode currentMode,
        DisplayMode[] supportedModes)
    {
        Id = id;
        Index = index;
        Name = name ?? string.Empty;
        IsPrimary = isPrimary;
        Bounds = bounds;
        WorkArea = workArea;
        ContentScale = contentScale > 0f ? contentScale : 1f;
        DesktopMode = desktopMode;
        CurrentMode = currentMode;
        _supportedModes = supportedModes ?? Array.Empty<DisplayMode>();
    }

    /// <summary>
    /// Gets the stable VOID identifier for this display.
    /// </summary>
    public DisplayId Id { get; }

    /// <summary>
    /// Gets the display's current zero-based enumeration index.
    /// </summary>
    /// <remarks>
    /// Enumeration indices may change when displays are connected or disconnected.
    /// Use <see cref="Id"/> for long-lived references.
    /// </remarks>
    public int Index { get; }

    /// <summary>
    /// Gets the human-readable display name supplied by the operating system.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this is the operating system's primary display.
    /// </summary>
    public bool IsPrimary { get; }

    /// <summary>
    /// Gets the full desktop bounds in global screen coordinates.
    /// </summary>
    public Rect2 Bounds { get; }

    /// <summary>
    /// Gets the desktop work area after system-reserved regions are excluded.
    /// </summary>
    public Rect2 WorkArea { get; }

    /// <summary>
    /// Gets the display content scale.
    /// </summary>
    /// <remarks>
    /// A value of 1.0 represents 100 percent scaling, 1.5 represents 150 percent,
    /// and 2.0 represents 200 percent.
    /// </remarks>
    public float ContentScale { get; }

    /// <summary>
    /// Gets the approximate logical DPI using a 96-DPI baseline.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="ContentScale"/> for layout decisions.
    /// </remarks>
    public float EstimatedDpi => 96f * ContentScale;

    /// <summary>
    /// Gets the display's desktop mode.
    /// </summary>
    public DisplayMode DesktopMode { get; }

    /// <summary>
    /// Gets the display's currently active mode.
    /// </summary>
    public DisplayMode CurrentMode { get; }

    /// <summary>
    /// Gets the fullscreen modes reported for this display.
    /// </summary>
    public IReadOnlyList<DisplayMode> SupportedModes => _supportedModes;
}

/// <summary>
/// Identifies the type of display configuration change reported by VOID.
/// </summary>
public enum DisplayChangeKind
{
    /// <summary>The display orientation changed.</summary>
    OrientationChanged,

    /// <summary>A display was connected.</summary>
    Added,

    /// <summary>A display was disconnected.</summary>
    Removed,

    /// <summary>The display moved within the desktop layout.</summary>
    Moved,

    /// <summary>The display's desktop mode changed.</summary>
    DesktopModeChanged,

    /// <summary>The display's active mode changed.</summary>
    CurrentModeChanged,

    /// <summary>The display content scale changed.</summary>
    ContentScaleChanged,

    /// <summary>The display work area changed.</summary>
    WorkAreaChanged
}

/// <summary>
/// Contains information about a display configuration change.
/// </summary>
public readonly struct DisplayChangedEvent
{
    internal DisplayChangedEvent(DisplayId display, DisplayChangeKind kind)
    {
        Display = display;
        Kind = kind;
    }

    /// <summary>
    /// Gets the display associated with the change.
    /// </summary>
    public DisplayId Display { get; }

    /// <summary>
    /// Gets the type of display change.
    /// </summary>
    public DisplayChangeKind Kind { get; }
}

/// <summary>
/// Provides access to connected displays and display configuration information.
/// </summary>
/// <remarks>
/// Native platform details remain internal to VOID. Returned display information
/// is exposed through renderer-neutral engine types.
/// </remarks>
public static class DisplayManager
{
    /// <summary>
    /// Occurs when a display is connected, disconnected, moved, or otherwise reconfigured.
    /// </summary>
    /// <remarks>
    /// Notifications are raised while the game loop pumps platform events.
    /// </remarks>
    public static event Action<DisplayChangedEvent> Changed;

    /// <summary>
    /// Gets a fresh snapshot of all currently connected displays.
    /// </summary>
    /// <returns>A read-only list containing the current displays.</returns>
    public static IReadOnlyList<DisplayInfo> GetDisplays()
        => Platform.SDL.SdlPlatform.GetDisplays();

    /// <summary>
    /// Gets the number of currently connected displays.
    /// </summary>
    public static int Count => GetDisplays().Count;

    /// <summary>
    /// Gets the current primary display.
    /// </summary>
    public static DisplayInfo PrimaryDisplay
        => Platform.SDL.SdlPlatform.GetPrimaryDisplay();

    /// <summary>
    /// Gets a display by its current enumeration index.
    /// </summary>
    /// <param name="index">The zero-based display index.</param>
    /// <returns>The display at the specified index.</returns>
    public static DisplayInfo GetDisplay(int index)
        => Platform.SDL.SdlPlatform.GetDisplay(index);

    /// <summary>
    /// Gets a display by its stable VOID display ID.
    /// </summary>
    /// <param name="id">The display ID to resolve.</param>
    /// <returns>The matching display.</returns>
    public static DisplayInfo GetDisplay(DisplayId id)
        => Platform.SDL.SdlPlatform.GetDisplay(id);

    /// <summary>
    /// Attempts to get a display by its stable VOID display ID.
    /// </summary>
    /// <param name="id">The display ID to resolve.</param>
    /// <param name="display">When this method returns, contains the matching display when found.</param>
    /// <returns><see langword="true"/> if the display was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDisplay(DisplayId id, out DisplayInfo display)
        => Platform.SDL.SdlPlatform.TryGetDisplay(id, out display);

    /// <summary>
    /// Gets the current enumeration index for a display.
    /// </summary>
    /// <param name="id">The display ID to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> if the display is no longer connected.</returns>
    public static int GetIndex(DisplayId id)
        => Platform.SDL.SdlPlatform.GetDisplayIndex(id);

    internal static void NotifyChanged(DisplayId display, DisplayChangeKind kind)
        => Changed?.Invoke(new DisplayChangedEvent(display, kind));
}
