// ============================================================================
//  SdlPlatform.cs
// ============================================================================
//  SDL platform initialization, backend selection, and display management.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Platform.SDL;

/// <summary>
/// Owns SDL initialization and display queries for VOID. SDL remains an internal platform detail.
/// </summary>
internal static class SdlPlatform
{
    private static int _refCount;

    public static void Acquire()
    {
        if (_refCount++ > 0)
            return;

        ConfigureVideoBackend();

        if (!SDL3.SDL.Init(SDL3.SDL.InitFlags.Video | SDL3.SDL.InitFlags.Gamepad))
        {
            _refCount = 0;
            throw new InvalidOperationException($"SDL video/gamepad initialization failed: {SDL3.SDL.GetError()}");
        }

        WindowCapabilities capabilities = GetWindowCapabilities();

        Logger.Instance.InfoWithCategory("Platform", "SDL video backend:");
        Logger.Instance.InfoWithCategory("Platform", "Name: {0}", capabilities.BackendName);
        Logger.Instance.InfoWithCategory("Platform", "Backend: {0}", capabilities.Backend);

        Logger.Instance.InfoWithCategory("Platform", "Window capabilities:");
        Logger.Instance.InfoWithCategory("Platform", "CanPositionWindow: {0}", capabilities.CanPositionWindow);
        Logger.Instance.InfoWithCategory("Platform", "CanSelectWindowedDisplay: {0}", capabilities.CanSelectWindowedDisplay);
        Logger.Instance.InfoWithCategory("Platform", "CanSelectDesktopFullscreenDisplay: {0}", capabilities.CanSelectDesktopFullscreenDisplay);
        Logger.Instance.InfoWithCategory("Platform", "CanSelectExclusiveFullscreenDisplay: {0}", capabilities.CanSelectExclusiveFullscreenDisplay);
        Logger.Instance.InfoWithCategory("Platform", "SupportsDisplayEnumeration: {0}", capabilities.SupportsDisplayEnumeration);
        Logger.Instance.InfoWithCategory("Platform", "SupportsDisplayModes: {0}", capabilities.SupportsDisplayModes);
        Logger.Instance.InfoWithCategory("Platform", "SupportsContentScale: {0}", capabilities.SupportsContentScale);
        Logger.Instance.InfoWithCategory("Platform", "UsesCompositorWindowPlacement: {0}", capabilities.UsesCompositorWindowPlacement);
    }

    private static void ConfigureVideoBackend()
    {
        // Windows, macOS, and other platforms keep SDL's normal native backend.
        if (!OperatingSystem.IsLinux())
            return;

        string driverList = GameSettings.Instance.LinuxWindowBackend switch
        {
            LinuxWindowBackend.X11ThenWayland => "x11,wayland",
            LinuxWindowBackend.X11 => "x11",
            LinuxWindowBackend.Wayland => "wayland",
            LinuxWindowBackend.Auto => null,
            _ => "x11,wayland"
        };

        // Auto deliberately leaves SDL_VIDEO_DRIVER untouched.
        if (driverList == null)
            return;

        // SDL requires this before SDL_Init(). Override priority ensures the
        // GameSettings choice is the one VOID actually requests.
        if (!SDL3.SDL.SetHintWithPriority(
                SDL3.SDL.Hints.VideoDriver,
                driverList,
                SDL3.SDL.HintPriority.Override))
        {
            throw new InvalidOperationException(
                $"SDL could not set Linux video backend preference '{driverList}': {SDL3.SDL.GetError()}");
        }
    }

    internal static NativeWindowBackend GetWindowBackend()
    {
        string driver = SDL3.SDL.GetCurrentVideoDriver();

        if (string.IsNullOrWhiteSpace(driver))
            return NativeWindowBackend.Unknown;

        return driver.ToLowerInvariant() switch
        {
            "windows" => NativeWindowBackend.Windows,
            "x11" => NativeWindowBackend.X11,
            "wayland" => NativeWindowBackend.Wayland,
            "cocoa" => NativeWindowBackend.Cocoa,
            "android" => NativeWindowBackend.Android,
            "uikit" => NativeWindowBackend.UIKit,
            "kmsdrm" => NativeWindowBackend.KmsDrm,
            _ => NativeWindowBackend.Other
        };
    }

    internal static WindowCapabilities GetWindowCapabilities()
    {
        string driver = SDL3.SDL.GetCurrentVideoDriver() ?? "unknown";
        NativeWindowBackend backend = GetWindowBackend();

        bool canPositionWindow = backend switch
        {
            NativeWindowBackend.Windows => true,
            NativeWindowBackend.X11 => true,
            NativeWindowBackend.Cocoa => true,
            NativeWindowBackend.KmsDrm => true,
            _ => false
        };

        bool canSelectWindowedDisplay = canPositionWindow;
        bool canSelectDesktopFullscreenDisplay = canPositionWindow;
        bool canSelectExclusiveFullscreenDisplay = backend switch
        {
            NativeWindowBackend.Windows => true,
            NativeWindowBackend.X11 => true,
            NativeWindowBackend.Wayland => true,
            NativeWindowBackend.Cocoa => true,
            NativeWindowBackend.KmsDrm => true,
            _ => false
        };

        // These queries are implemented by the SDL3 display layer and are
        // available to the desktop backends supported by VOID.
        bool desktopDisplayFeatures = backend switch
        {
            NativeWindowBackend.Windows => true,
            NativeWindowBackend.X11 => true,
            NativeWindowBackend.Wayland => true,
            NativeWindowBackend.Cocoa => true,
            NativeWindowBackend.KmsDrm => true,
            _ => false
        };

        return new WindowCapabilities(
            backend,
            driver,
            canPositionWindow,
            canSelectWindowedDisplay,
            canSelectDesktopFullscreenDisplay,
            canSelectExclusiveFullscreenDisplay,
            supportsDisplayEnumeration: desktopDisplayFeatures,
            supportsDisplayModes: desktopDisplayFeatures,
            supportsContentScale: desktopDisplayFeatures);
    }

    public static void Release()
    {
        if (_refCount <= 0)
            return;

        if (--_refCount == 0)
            SDL3.SDL.Quit();
    }

    internal static DisplayInfo[] GetDisplays()
    {
        bool temporaryAcquire = EnsureAvailable();
        try
        {
            uint[] ids = SDL3.SDL.GetDisplays(out int count) ?? Array.Empty<uint>();
            int actualCount = Math.Min(Math.Max(count, 0), ids.Length);
            if (actualCount == 0)
                return Array.Empty<DisplayInfo>();

            uint primary = SDL3.SDL.GetPrimaryDisplay();
            var displays = new DisplayInfo[actualCount];

            for (int i = 0; i < actualCount; i++)
                displays[i] = BuildDisplayInfo(ids[i], i, ids[i] == primary);

            return displays;
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static DisplayInfo GetPrimaryDisplay()
    {
        bool temporaryAcquire = EnsureAvailable();
        try
        {
            uint primary = SDL3.SDL.GetPrimaryDisplay();
            if (primary == 0)
                throw new InvalidOperationException($"SDL could not determine the primary display: {SDL3.SDL.GetError()}");

            int index = GetDisplayIndexCore(primary);
            return BuildDisplayInfo(primary, index < 0 ? 0 : index, true);
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static DisplayInfo GetDisplay(int index)
    {
        bool temporaryAcquire = EnsureAvailable();
        try
        {
            uint[] ids = SDL3.SDL.GetDisplays(out int count) ?? Array.Empty<uint>();
            int actualCount = Math.Min(Math.Max(count, 0), ids.Length);

            if ((uint)index >= (uint)actualCount)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Display index {index} is outside the connected display range 0..{Math.Max(actualCount - 1, 0)}.");

            uint id = ids[index];
            return BuildDisplayInfo(id, index, id == SDL3.SDL.GetPrimaryDisplay());
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static DisplayInfo GetDisplay(DisplayId id)
    {
        if (!id.IsValid)
            throw new ArgumentException("Display ID is invalid.", nameof(id));

        bool temporaryAcquire = EnsureAvailable();
        try
        {
            int index = GetDisplayIndexCore(id.NativeValue);
            if (index < 0)
                throw new InvalidOperationException("The requested display is no longer connected.");

            return BuildDisplayInfo(id.NativeValue, index, id.NativeValue == SDL3.SDL.GetPrimaryDisplay());
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static bool TryGetDisplay(DisplayId id, out DisplayInfo display)
    {
        display = null;
        if (!id.IsValid)
            return false;

        bool temporaryAcquire = EnsureAvailable();
        try
        {
            int index = GetDisplayIndexCore(id.NativeValue);
            if (index < 0)
                return false;

            display = BuildDisplayInfo(id.NativeValue, index, id.NativeValue == SDL3.SDL.GetPrimaryDisplay());
            return true;
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static int GetDisplayIndex(DisplayId id)
    {
        if (!id.IsValid)
            return -1;

        bool temporaryAcquire = EnsureAvailable();
        try
        {
            return GetDisplayIndexCore(id.NativeValue);
        }
        finally
        {
            if (temporaryAcquire)
                Release();
        }
    }

    internal static DisplayId GetDisplayId(int index)
        => GetDisplay(index).Id;

    internal static DisplayId GetPrimaryDisplayId()
        => GetPrimaryDisplay().Id;

    internal static DisplayId GetDisplayForWindow(IntPtr window)
    {
        if (window == IntPtr.Zero)
            return default;

        uint id = SDL3.SDL.GetDisplayForWindow(window);
        return new DisplayId(id);
    }

    internal static Rect2 GetDisplayBounds(DisplayId display, bool usable)
    {
        if (!display.IsValid)
            throw new ArgumentException("Display ID is invalid.", nameof(display));

        SDL3.SDL.Rect rect;
        bool success = usable
            ? SDL3.SDL.GetDisplayUsableBounds(display.NativeValue, out rect)
            : SDL3.SDL.GetDisplayBounds(display.NativeValue, out rect);

        if (!success)
            throw new InvalidOperationException($"SDL could not query display bounds: {SDL3.SDL.GetError()}");

        return new Rect2(rect.X, rect.Y, rect.W, rect.H);
    }

    internal static Vect2 GetPrimaryDesktopResolution()
    {
        DisplayInfo display = GetPrimaryDisplay();
        return new Vect2(display.DesktopMode.Width, display.DesktopMode.Height);
    }

    internal static List<Vect2> GetPrimarySupportedResolutions()
    {
        DisplayInfo display = GetPrimaryDisplay();
        var result = new List<Vect2>();

        foreach (DisplayMode mode in display.SupportedModes)
        {
            var size = new Vect2(mode.Width, mode.Height);
            if (!result.Contains(size))
                result.Add(size);
        }

        return result;
    }

    internal static bool IsPrimaryResolutionSupported(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;

        foreach (var size in GetPrimarySupportedResolutions())
        {
            if ((int)size.X == width && (int)size.Y == height)
                return true;
        }

        return false;
    }

    private static DisplayInfo BuildDisplayInfo(uint displayId, int index, bool isPrimary)
    {
        string name = SDL3.SDL.GetDisplayName(displayId) ?? $"Display {index + 1}";

        Rect2 bounds = QueryRect(displayId, usable: false);
        Rect2 workArea = QueryRect(displayId, usable: true);
        if (workArea.IsEmpty)
            workArea = bounds;

        float contentScale = SDL3.SDL.GetDisplayContentScale(displayId);
        if (contentScale <= 0f)
            contentScale = 1f;

        SDL3.SDL.DisplayMode? desktop = SDL3.SDL.GetDesktopDisplayMode(displayId);
        SDL3.SDL.DisplayMode? current = SDL3.SDL.GetCurrentDisplayMode(displayId);

        DisplayMode desktopMode = desktop.HasValue ? ConvertMode(desktop.Value) : default;
        DisplayMode currentMode = current.HasValue ? ConvertMode(current.Value) : desktopMode;

        SDL3.SDL.DisplayMode[] modes = SDL3.SDL.GetFullscreenDisplayModes(displayId, out int modeCount)
            ?? Array.Empty<SDL3.SDL.DisplayMode>();

        int actualCount = Math.Min(Math.Max(modeCount, 0), modes.Length);
        var supported = new List<DisplayMode>(actualCount);

        for (int i = 0; i < actualCount; i++)
        {
            DisplayMode mode = ConvertMode(modes[i]);
            if (mode.Width == 0 || mode.Height == 0)
                continue;

            if (!supported.Contains(mode))
                supported.Add(mode);
        }

        supported.Sort(static (a, b) =>
        {
            int area = ((long)a.Width * a.Height).CompareTo((long)b.Width * b.Height);
            if (area != 0) return area;
            int refresh = a.ExactRefreshRate.CompareTo(b.ExactRefreshRate);
            if (refresh != 0) return refresh;
            return a.PixelDensity.CompareTo(b.PixelDensity);
        });

        return new DisplayInfo(
            new DisplayId(displayId),
            index,
            name,
            isPrimary,
            bounds,
            workArea,
            contentScale,
            desktopMode,
            currentMode,
            supported.ToArray());
    }

    private static Rect2 QueryRect(uint displayId, bool usable)
    {
        SDL3.SDL.Rect rect;
        bool success = usable
            ? SDL3.SDL.GetDisplayUsableBounds(displayId, out rect)
            : SDL3.SDL.GetDisplayBounds(displayId, out rect);

        return success
            ? new Rect2(rect.X, rect.Y, rect.W, rect.H)
            : Rect2.Empty;
    }

    private static DisplayMode ConvertMode(SDL3.SDL.DisplayMode mode)
        => new(
            mode.W,
            mode.H,
            mode.RefreshRate,
            mode.PixelDensity,
            mode.RefreshRateNumerator,
            mode.RefreshRateDenominator);

    private static int GetDisplayIndexCore(uint displayId)
    {
        uint[] ids = SDL3.SDL.GetDisplays(out int count) ?? Array.Empty<uint>();
        int actualCount = Math.Min(Math.Max(count, 0), ids.Length);

        for (int i = 0; i < actualCount; i++)
        {
            if (ids[i] == displayId)
                return i;
        }

        return -1;
    }

    private static bool EnsureAvailable()
    {
        if (_refCount > 0)
            return false;

        Acquire();
        return true;
    }
}
