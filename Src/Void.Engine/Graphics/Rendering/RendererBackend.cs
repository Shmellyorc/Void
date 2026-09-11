namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Optional base class for renderer authors who prefer VOID's normal extension style.
/// Custom renderers may derive from this class or implement IRendererBackend directly.
/// </summary>
public abstract class RendererBackend : IRendererBackend
{
    private bool _disposed;

    public abstract string Name { get; }
    public abstract GraphicsApi Api { get; }
    public abstract GraphicsVersion Version { get; }
    public abstract RendererWindowFlags RequiredWindowFlags { get; }
    public abstract RendererCapabilities Capabilities { get; }
    public abstract IGraphicsDevice Device { get; }
    public abstract IGraphicsShaderProgram Default2DShader { get; }

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes the backend once the SDL-backed window exists.
    /// Override OnInitialize to create API-specific devices, contexts, or swapchains.
    /// </summary>
    public void Initialize(IRendererContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsInitialized)
            throw new InvalidOperationException($"Renderer '{Name}' has already been initialized.");

        ArgumentNullException.ThrowIfNull(context);

        OnInitialize(context);
        IsInitialized = true;
        OnInitialized();
        RendererRuntime.Attach(this);
    }

    protected abstract void OnInitialize(IRendererContext context);

    /// <summary>
    /// Optional hook after successful initialization.
    /// </summary>
    protected virtual void OnInitialized() { }

    public abstract void BeginFrame(Color clearColor);
    public abstract void EndFrame();
    public abstract void Resize(int width, int height);

    public void Dispose()
    {
        if (_disposed)
            return;

        RendererRuntime.Detach(this);
        OnDispose();
        _disposed = true;
        IsInitialized = false;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override for backend-specific teardown. The base implementation disposes Device.
    /// </summary>
    protected virtual void OnDispose()
    {
        Device?.Dispose();
    }
}
