// ============================================================================
//  Display.cs
// ============================================================================
//  Renderer/platform-neutral display and monitor information for VOID.
//  SDL is used internally by the platform layer but is never exposed here.
// ============================================================================

namespace Void.Engine.Systems;

/// <summary>
/// Stable VOID handle for a connected display.
/// </summary>
/// <remarks>
/// The native platform identifier is intentionally hidden so SDL remains an
/// implementation detail. A display ID remains useful across display-list
/// reordering while that display stays connected.
/// </remarks>
public readonly struct DisplayId : IEquatable<DisplayId>
{
    private readonly uint _value;

    internal DisplayId(uint value) => _value = value;

    internal uint NativeValue => _value;

    /// <summary>Gets whether this handle refers to a valid display.</summary>
    public bool IsValid => _value != 0;

    public bool Equals(DisplayId other) => _value == other._value;
    public override bool Equals(object obj) => obj is DisplayId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(DisplayId left, DisplayId right) => left.Equals(right);
    public static bool operator !=(DisplayId left, DisplayId right) => !left.Equals(right);

    public override string ToString() => IsValid ? $"Display({_value})" : "Display(Invalid)";
}

/// <summary>
/// Describes one resolution/refresh-rate mode supported by a display.
/// </summary>
/// <summary>
/// Determines how VOID enters fullscreen.
/// </summary>
public enum FullscreenStyle
{
    /// <summary>
    /// Desktop/borderless fullscreen. The operating system keeps the display's
    /// current desktop mode; VOID does not request a resolution switch.
    /// </summary>
    Desktop,

    /// <summary>
    /// Exclusive fullscreen. VOID requests a concrete resolution and optional
    /// refresh rate from the selected display.
    /// </summary>
    Exclusive
}

public readonly struct DisplayMode : IEquatable<DisplayMode>
{
    /// <summary>Mode width in logical display coordinates.</summary>
    public uint Width { get; }

    /// <summary>Mode height in logical display coordinates.</summary>
    public uint Height { get; }

    /// <summary>
    /// Retained for compatibility with the old VOID display-mode shape.
    /// SDL3 does not expose a simple bits-per-pixel field for every mode, so
    /// this is zero when unavailable.
    /// </summary>
    public uint BitsPerPixel { get; }

    /// <summary>Nominal refresh rate in Hz, or zero when unspecified.</summary>
    public float RefreshRate { get; }

    /// <summary>Logical-to-pixel scale associated with this mode.</summary>
    public float PixelDensity { get; }

    /// <summary>Exact refresh-rate numerator when supplied by the platform.</summary>
    public int RefreshRateNumerator { get; }

    /// <summary>Exact refresh-rate denominator when supplied by the platform.</summary>
    public int RefreshRateDenominator { get; }

    /// <summary>
    /// Gets the most precise refresh rate available.
    /// </summary>
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

    public bool Equals(DisplayMode other)
        => Width == other.Width &&
           Height == other.Height &&
           MathF.Abs(RefreshRate - other.RefreshRate) <= 0.001f &&
           MathF.Abs(PixelDensity - other.PixelDensity) <= 0.001f;

    public override bool Equals(object obj) => obj is DisplayMode other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height, RefreshRate, PixelDensity);

    public static bool operator ==(DisplayMode left, DisplayMode right) => left.Equals(right);
    public static bool operator !=(DisplayMode left, DisplayMode right) => !left.Equals(right);

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
/// Immutable snapshot of one connected display.
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

    /// <summary>Stable VOID handle for this connected display.</summary>
    public DisplayId Id { get; }

    /// <summary>
    /// Current zero-based enumeration index. Indices can change when displays
    /// are connected or disconnected; keep <see cref="Id"/> for long-lived references.
    /// </summary>
    public int Index { get; }

    /// <summary>Human-readable display name supplied by the operating system.</summary>
    public string Name { get; }

    /// <summary>Gets whether this is the operating system's primary display.</summary>
    public bool IsPrimary { get; }

    /// <summary>Full desktop bounds in global screen coordinates.</summary>
    public Rect2 Bounds { get; }

    /// <summary>
    /// Desktop work area after system-reserved regions such as panels, docks,
    /// or taskbars have been removed.
    /// </summary>
    public Rect2 WorkArea { get; }

    /// <summary>
    /// Display content scale. 1.0 is 100%, 1.5 is 150%, 2.0 is 200%, etc.
    /// </summary>
    public float ContentScale { get; }

    /// <summary>
    /// Approximate logical DPI based on the conventional 96-DPI desktop baseline.
    /// Prefer <see cref="ContentScale"/> for layout decisions.
    /// </summary>
    public float EstimatedDpi => 96f * ContentScale;

    /// <summary>Desktop display mode.</summary>
    public DisplayMode DesktopMode { get; }

    /// <summary>Currently active display mode.</summary>
    public DisplayMode CurrentMode { get; }

    /// <summary>Fullscreen modes reported for this display.</summary>
    public IReadOnlyList<DisplayMode> SupportedModes => _supportedModes;
}

/// <summary>Types of display topology/configuration changes reported by VOID.</summary>
public enum DisplayChangeKind
{
    OrientationChanged,
    Added,
    Removed,
    Moved,
    DesktopModeChanged,
    CurrentModeChanged,
    ContentScaleChanged,
    WorkAreaChanged
}

/// <summary>Display change notification raised while SDL events are pumped.</summary>
public readonly struct DisplayChangedEvent
{
    internal DisplayChangedEvent(DisplayId display, DisplayChangeKind kind)
    {
        Display = display;
        Kind = kind;
    }

    public DisplayId Display { get; }
    public DisplayChangeKind Kind { get; }
}

/// <summary>
/// Public monitor/display API. No SDL types are exposed.
/// </summary>
public static class DisplayManager
{
    /// <summary>
    /// Raised for display hot-plug and configuration changes while the game
    /// event loop is pumping platform events.
    /// </summary>
    public static event Action<DisplayChangedEvent> Changed;

    /// <summary>Gets a fresh snapshot of all currently connected displays.</summary>
    public static IReadOnlyList<DisplayInfo> GetDisplays()
        => Platform.SDL.SdlPlatform.GetDisplays();

    /// <summary>Gets the number of currently connected displays.</summary>
    public static int Count => GetDisplays().Count;

    /// <summary>Gets the current primary display.</summary>
    public static DisplayInfo PrimaryDisplay
        => Platform.SDL.SdlPlatform.GetPrimaryDisplay();

    /// <summary>Gets a display by its current enumeration index.</summary>
    public static DisplayInfo GetDisplay(int index)
        => Platform.SDL.SdlPlatform.GetDisplay(index);

    /// <summary>Gets a display by its stable VOID display handle.</summary>
    public static DisplayInfo GetDisplay(DisplayId id)
        => Platform.SDL.SdlPlatform.GetDisplay(id);

    /// <summary>Attempts to get a display by handle.</summary>
    public static bool TryGetDisplay(DisplayId id, out DisplayInfo display)
        => Platform.SDL.SdlPlatform.TryGetDisplay(id, out display);

    /// <summary>Gets the current enumeration index for a display, or -1 if disconnected.</summary>
    public static int GetIndex(DisplayId id)
        => Platform.SDL.SdlPlatform.GetDisplayIndex(id);

    internal static void NotifyChanged(DisplayId display, DisplayChangeKind kind)
        => Changed?.Invoke(new DisplayChangedEvent(display, kind));
}
