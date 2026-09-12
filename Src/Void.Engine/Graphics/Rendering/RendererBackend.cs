// ============================================================================
//  RendererBackend.cs
// ============================================================================
//  Optional base class for third-party renderer backend implementations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics.Rendering;

/// <summary>
/// Provides the common initialization and disposal lifecycle for a renderer backend.
/// </summary>
/// <remarks>
/// <para>
/// Renderer authors may derive from this class or implement <see cref="IRendererBackend"/>
/// directly. Derived implementations create API-specific state in <see cref="OnInitialize"/>
/// and may release additional resources by overriding <see cref="OnDispose"/>.
/// </para>
/// <para>
/// A backend instance can be initialized only once. After disposal it cannot be reused.
/// The default disposal hook disposes <see cref="Device"/>.
/// </para>
/// </remarks>
public abstract class RendererBackend : IRendererBackend
{
    private bool _disposed;

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract GraphicsApi Api { get; }

    /// <inheritdoc/>
    public abstract GraphicsVersion Version { get; }

    /// <inheritdoc/>
    public abstract RendererWindowFlags RequiredWindowFlags { get; }

    /// <inheritdoc/>
    public abstract RendererCapabilities Capabilities { get; }

    /// <inheritdoc/>
    public abstract IGraphicsDevice Device { get; }

    /// <inheritdoc/>
    public abstract IGraphicsShaderProgram Default2DShader { get; }

    /// <inheritdoc/>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes this backend after VOID has created the native window.
    /// </summary>
    /// <param name="context">VOID-owned window and platform services.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this backend has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this backend is already initialized.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// <see cref="OnInitialize"/> runs first. When it completes, <see cref="IsInitialized"/>
    /// becomes true and <see cref="OnInitialized"/> runs. Derived implementations should
    /// have <see cref="Device"/>, <see cref="Capabilities"/>, and
    /// <see cref="Default2DShader"/> ready before <see cref="OnInitialize"/> returns.
    /// </remarks>
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

    /// <summary>
    /// Creates backend-specific device, presentation, shader, and capability state.
    /// </summary>
    /// <param name="context">VOID-owned window and platform services.</param>
    protected abstract void OnInitialize(IRendererContext context);

    /// <summary>
    /// Called after <see cref="OnInitialize"/> has completed and
    /// <see cref="IsInitialized"/> has become true.
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <inheritdoc/>
    public abstract void BeginFrame(Color clearColor);

    /// <inheritdoc/>
    public abstract void EndFrame();

    /// <inheritdoc/>
    public abstract void Resize(int width, int height);

    /// <summary>
    /// Releases this backend and its owned resources.
    /// </summary>
    /// <remarks>
    /// Repeated calls are ignored. <see cref="OnDispose"/> runs once and the backend
    /// cannot be initialized again afterward.
    /// </remarks>
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
    /// Releases backend-specific resources owned by this renderer.
    /// </summary>
    /// <remarks>
    /// The base implementation disposes <see cref="Device"/>. An override that owns
    /// additional resources should release them and either call the base implementation
    /// or dispose the device itself.
    /// </remarks>
    protected virtual void OnDispose()
    {
        Device?.Dispose();
    }
}
