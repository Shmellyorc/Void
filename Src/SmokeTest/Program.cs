using SDL3;
using Silk.NET.OpenGL;

if (!SDL.Init(SDL.InitFlags.Video))
    throw new InvalidOperationException($"SDL init failed: {SDL.GetError()}");

IntPtr window = IntPtr.Zero;
IntPtr context = IntPtr.Zero;
GL? gl = null;

try
{
    SDL.GLResetAttributes();
    SDL.GLSetAttribute(SDL.GLAttr.ContextMajorVersion, 3);
    SDL.GLSetAttribute(SDL.GLAttr.ContextMinorVersion, 3);
    SDL.GLSetAttribute(SDL.GLAttr.ContextProfileMask, (int)SDL.GLProfile.Core);
    SDL.GLSetAttribute(SDL.GLAttr.DoubleBuffer, 1);

    window = SDL.CreateWindow(
        "VOID SDL3-CS + Silk.NET OpenGL Smoke Test",
        1280,
        720,
        SDL.WindowFlags.OpenGL | SDL.WindowFlags.Resizable);

    if (window == IntPtr.Zero)
        throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.GetError()}");

    context = SDL.GLCreateContext(window);
    if (context == IntPtr.Zero)
        throw new InvalidOperationException($"SDL_GL_CreateContext failed: {SDL.GetError()}");

    if (!SDL.GLMakeCurrent(window, context))
        throw new InvalidOperationException($"SDL_GL_MakeCurrent failed: {SDL.GetError()}");

    if (!SDL.GLSetSwapInterval(1))
        Console.WriteLine($"VSync could not be enabled: {SDL.GetError()}");

    gl = GL.GetApi(name => SDL.GLGetProcAddress(name));
    gl.Viewport(0, 0, 1280, 720);

    bool running = true;
    while (running)
    {
        while (SDL.PollEvent(out var e))
        {
            SDL.EventType type = (SDL.EventType)e.Type;
            if (type is SDL.EventType.Quit or SDL.EventType.WindowCloseRequested)
                running = false;
        }

        gl.ClearColor(100f / 255f, 149f / 255f, 237f / 255f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit);

        if (!SDL.GLSwapWindow(window))
            throw new InvalidOperationException($"SDL_GL_SwapWindow failed: {SDL.GetError()}");
    }
}
finally
{
    gl?.Dispose();

    if (context != IntPtr.Zero)
        SDL.GLDestroyContext(context);

    if (window != IntPtr.Zero)
        SDL.DestroyWindow(window);

    SDL.Quit();
}
