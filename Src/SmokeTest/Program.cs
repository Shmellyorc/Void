// Renderer API regression test.
// Verifies that external renderer backends can retrieve platform-native handles
// through IRendererContext without depending on VOID's internal SDL implementation.
//
// This intentionally exercises the same public path available to third-party
// renderer plugins through GameSettings.SetRenderer().
//
// Added during VOID's migration from SFML to SDL3 + Silk.NET.

using Void.Engine;
using Void.Engine.Graphics.Rendering;
using Void.Engine.Systems;

Console.WriteLine("VOID Native Handle Bridge Smoke Test");
Console.WriteLine();

var settings = GameSettings.Instance
    .SetRenderer(() => new NativeHandleProbeRenderer());

// On Linux, force the exact backend we want to verify. On a Wayland desktop
// this intentionally exercises VOID through XWayland/X11.
if (OperatingSystem.IsLinux())
    settings.SetLinuxWindowBackend(LinuxWindowBackend.X11);

try
{
    // The probe renderer throws ProbeSucceededException from Initialize()
    // after validating the native handles. Window catches it, cleans up the
    // SDL host, and rethrows it to us here.
    using var window = new Window(
        640,
        360,
        "VOID Native Handle Bridge Smoke Test",
        WindowMode.Windowed,
        vsync: false);

    Console.Error.WriteLine("FAIL: probe renderer initialized without completing the probe.");
    return 1;
}
catch (ProbeSucceededException)
{
    Console.WriteLine();
    Console.WriteLine("PASS: native handle bridge returned the expected non-zero handles.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"FAIL: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

sealed class NativeHandleProbeRenderer : IRendererBackend
{
    public string Name => "VOID Native Handle Probe";
    public GraphicsApi Api => GraphicsApi.Custom;
    public GraphicsVersion Version => default;
    public RendererWindowFlags RequiredWindowFlags => RendererWindowFlags.None;
    public RendererCapabilities Capabilities => default;

    // These are intentionally never reached. The probe completes during
    // Initialize(), before Window asks for rendering resources.
    public IGraphicsDevice Device
        => throw new NotSupportedException("The native-handle probe does not create a graphics device.");

    public IGraphicsShaderProgram Default2DShader
        => throw new NotSupportedException("The native-handle probe does not create shaders.");

    public bool IsInitialized { get; private set; }

    public void Initialize(IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IsInitialized = true;

        Console.WriteLine($"Platform backend: {context.PlatformBackend}");
        Console.WriteLine($"VOID window-system handle: {Format(context.WindowSystemHandle)}");
        Console.WriteLine();

        switch (context.PlatformBackend)
        {
            case NativeWindowBackend.X11:
                ProbeX11(context);
                break;

            case NativeWindowBackend.Windows:
                ProbeWindows(context);
                break;

            case NativeWindowBackend.Wayland:
                ProbeWayland(context);
                break;

            case NativeWindowBackend.Cocoa:
                ProbeCocoa(context);
                break;

            default:
                throw new PlatformNotSupportedException(
                    $"Native-handle smoke test does not have assertions for " +
                    $"{context.PlatformBackend}.");
        }

        throw new ProbeSucceededException();
    }

    private static void ProbeX11(IRendererContext context)
    {
        nint window = RequireHandle(context, NativeWindowHandleKind.Window);
        nint display = RequireHandle(context, NativeWindowHandleKind.Display);

        Console.WriteLine("X11 bridge:");
        Console.WriteLine($"  Window/XID : {Format(window)}");
        Console.WriteLine($"  Display*   : {Format(display)}");
    }

    private static void ProbeWindows(IRendererContext context)
    {
        nint hwnd = RequireHandle(context, NativeWindowHandleKind.Window);
        nint monitor = RequireHandle(context, NativeWindowHandleKind.Display);
        nint instance = RequireHandle(context, NativeWindowHandleKind.Instance);
        nint hdc = RequireHandle(context, NativeWindowHandleKind.Surface);

        Console.WriteLine("Win32 bridge:");
        Console.WriteLine($"  HWND       : {Format(hwnd)}");
        Console.WriteLine($"  HMONITOR   : {Format(monitor)}");
        Console.WriteLine($"  HINSTANCE  : {Format(instance)}");
        Console.WriteLine($"  HDC        : {Format(hdc)}");
    }

    private static void ProbeWayland(IRendererContext context)
    {
        nint surface = RequireHandle(context, NativeWindowHandleKind.Window);
        nint display = RequireHandle(context, NativeWindowHandleKind.Display);
        nint renderSurface = RequireHandle(context, NativeWindowHandleKind.Surface);

        if (surface != renderSurface)
        {
            throw new InvalidOperationException(
                "Wayland Window and Surface handles were expected to resolve to the same wl_surface*.");
        }

        Console.WriteLine("Wayland bridge:");
        Console.WriteLine($"  wl_surface*: {Format(surface)}");
        Console.WriteLine($"  wl_display*: {Format(display)}");
    }

    private static void ProbeCocoa(IRendererContext context)
    {
        nint window = RequireHandle(context, NativeWindowHandleKind.Window);

        Console.WriteLine("Cocoa bridge:");
        Console.WriteLine($"  NSWindow*  : {Format(window)}");
        Console.WriteLine("  Metal View/Surface are tested only by a renderer requesting RendererWindowFlags.Metal.");
    }

    private static nint RequireHandle(
        IRendererContext context,
        NativeWindowHandleKind kind)
    {
        bool success = context.TryGetNativeHandle(kind, out nint handle);

        Console.WriteLine(
            $"TryGetNativeHandle({kind}) -> {success}, {Format(handle)}");

        if (!success)
            throw new InvalidOperationException(
                $"TryGetNativeHandle({kind}) returned false.");

        if (handle == 0)
            throw new InvalidOperationException(
                $"TryGetNativeHandle({kind}) returned a zero handle.");

        return handle;
    }

    private static string Format(nint handle)
        => $"0x{unchecked((nuint)handle):X}";

    public void BeginFrame(Color clearColor)
        => throw new NotSupportedException();

    public void EndFrame()
        => throw new NotSupportedException();

    public void Resize(int width, int height)
        => throw new NotSupportedException();

    public void Dispose()
    {
    }
}

sealed class ProbeSucceededException : Exception;
